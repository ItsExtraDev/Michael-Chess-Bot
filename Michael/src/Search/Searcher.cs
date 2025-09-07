using Michael.src.Evaluation;
using Michael.src.Helpers;
using System.Diagnostics;

namespace Michael.src.Search
{
    /*
     * position fen r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -
        go

    info depth 1 score cp 350 nodes 48 nps 0 time 5 pv e2a6
    info depth 2 score cp 30 nodes 2135 nps 0 time 28 pv e2a6
    info depth 3 score cp 350 nodes 102084 nps 0 time 324 pv e2a6
    info depth 4 score cp -270 nodes 4_287_636 nps 0 time 7284 pv e2a6

    info depth 1 score cp 350 nodes 48 nps 0 time 6 pv e2a6
    info depth 2 score cp 30 nodes 1089 nps 0 time 16 pv e2a6
    info depth 3 score cp 350 nodes 25391 nps 0 time 218 pv e2a6
    info depth 4 score cp -270 nodes 325_044 nps 0 time 531 pv e2a6

    info depth 1 score cp 350 nodes 48 nps 0 time 8 pv e2a6
    info depth 2 score cp 30 nodes 179 nps 0 time 26 pv e2a6
    info depth 3 score cp 350 nodes 2439 nps 0 time 92 pv e2a6
    info depth 4 score cp -170 nodes 8_687 nps 0 time 320 pv e2a6
    info depth 5 score cp -50 nodes 87913 nps 0 time 471 pv e2a6
    info depth 6 score cp -670 nodes 317532 nps 0 time 4847 pv e2a6
    
    info depth 1 score cp 350 nodes 48 nps 8000 time 6 pv e2a6
    info depth 2 score cp 30 nodes 131 nps 5458 time 24 pv e2a6
    info depth 3 score cp 350 nodes 2262 nps 20378 time 111 pv e2a6
    info depth 4 score cp -170 nodes 5_916 nps 18147 time 327 pv e2a6
    info depth 5 score cp -50 nodes 58976 nps 145980 time 404 pv e2a6
    info depth 6 score cp -670 nodes 150982 nps 55754 time 2708 pv e2a6
     */
    public class Searcher
    {
        //Variables
        private bool searchCancelled;
        private Board board;
        private Move bestMove;
        private Move bestMoveThisIteration;
        private int bestEval;
        private int bestEvalThisIteration;
        private int MaxDepth = 256;
        private int Depth;
        private const int PositiveInfinity = 1000000;
        private const int NegativeInfinity = -1000000;
        private int TotalNodes = 0;
        Stopwatch searchTime = new Stopwatch();

        public event Action<Move>? OnSearchComplete;

        //References
        readonly LogWriter writer;
        readonly Evaluator evaluator;
        readonly MoveOrderer orderer;

        // --- Transposition Table ---
        public enum NodeType { Exact, Alpha, Beta }

        public class TTEntry
        {
            public ulong ZobristKey;
            public int Depth;
            public int Eval;
            public NodeType Type;
            public Move BestMove;
        }

        private const int TTMaxSize = 10_000_000;
        private static Dictionary<ulong, TTEntry> TranspositionTable = new Dictionary<ulong, TTEntry>(TTMaxSize);

        public Searcher()
        {
            board = MatchManager.board;
            bestMove = bestMoveThisIteration = Move.NullMove();

            writer = new LogWriter(FileType.Search, true);
            evaluator = new Evaluator();
            orderer = new MoveOrderer();
        }

        public void EndSearch()
        {
            searchCancelled = true;
        }

        public void StartNewSearch()
        {
            searchCancelled = false;
            board = MatchManager.board;
            bestMove = Move.NullMove();
            bestEvalThisIteration = NegativeInfinity;
            TranspositionTable.Clear();

            StartIlliterateDeepeningSearch();

            OnSearchComplete?.Invoke(bestMove);
        }

        private void StartIlliterateDeepeningSearch()
        {
            for (Depth = 1; Depth <= MaxDepth; Depth++)
            {
                searchTime.Restart();
                TotalNodes = 0;

                Search(Depth, NegativeInfinity, PositiveInfinity);

                if (searchCancelled)
                {
                    break;
                }
                else
                {
                    bestMove = bestMoveThisIteration;
                    bestEval = bestEvalThisIteration;
                }

                PrintInfoMessage();
            }
        }

        public int Search(int depth, int alpha, int beta)
        {
            if (searchCancelled)
                return 0;

            if (board.IsDraw())
                return 0;

            if (board.IsCheckmate())
                return -1000000 + board.plyCount;

            Move[] legalMoves = board.GetLegalMoves();

            if (depth == 0 || legalMoves.Length == 0)
                return Quiesce(alpha, beta);

            ulong key = Zobrist.ComputeHash(board);

            // TT Lookup
            if (TranspositionTable.TryGetValue(key, out TTEntry entry) && entry.Depth >= depth)
            {
                if (entry.Type == NodeType.Exact)
                    return entry.Eval;
                if (entry.Type == NodeType.Alpha && entry.Eval <= alpha)
                    return alpha;
                if (entry.Type == NodeType.Beta && entry.Eval >= beta)
                    return beta;
            }

            int bestScore = NegativeInfinity;

            // Move ordering (try TT best move first if exists)
            if (entry != null && entry.BestMove != Move.NullMove())
            {
                int idx = Array.IndexOf(legalMoves, entry.BestMove);
                if (idx > 0)
                {
                    var tmp = legalMoves[0];
                    legalMoves[0] = legalMoves[idx];
                    legalMoves[idx] = tmp;
                }
            }

            orderer.OrderMoves(ref legalMoves, depth == Depth ? bestMoveThisIteration : Move.NullMove());

            Move bestLocalMove = Move.NullMove();
            NodeType nodeType = NodeType.Alpha;

            for (int moveIndex = 0; moveIndex < legalMoves.Length; moveIndex++)
            {
                TotalNodes++;
                Move move = legalMoves[moveIndex];

                board.MakeMove(move);
                int score = -Search(depth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestLocalMove = move;

                    if (depth == Depth)
                    {
                        bestEvalThisIteration = score;
                        bestMoveThisIteration = move;
                    }
                }

                alpha = Math.Max(alpha, score);
                if (alpha >= beta)
                {
                    nodeType = NodeType.Beta;
                    break;
                }
            }

            // Save in TT
            TranspositionTable[key] = new TTEntry
            {
                ZobristKey = key,
                Depth = depth,
                Eval = bestScore,
                Type = bestScore >= beta ? NodeType.Beta : (bestScore > alpha ? NodeType.Exact : NodeType.Alpha),
                BestMove = bestLocalMove
            };

            return bestScore;
        }

        int Quiesce(int alpha, int beta)
        {
            ulong key = Zobrist.ComputeHash(board);

            // TT Lookup
            if (TranspositionTable.TryGetValue(key, out TTEntry entry) && entry.Depth >= 0)
            {
                if (entry.Type == NodeType.Exact)
                    return entry.Eval;
                if (entry.Type == NodeType.Alpha && entry.Eval <= alpha)
                    return alpha;
                if (entry.Type == NodeType.Beta && entry.Eval >= beta)
                    return beta;
            }

            int static_eval = evaluator.Evaluate();

            int best_value = static_eval;
            if (best_value >= beta)
                return best_value;
            if (best_value > alpha)
                alpha = best_value;

            Move[] legalCaptures = board.GetLegalMoves(true);
            orderer.OrderMoves(ref legalCaptures, Move.NullMove());

            Move bestLocalMove = Move.NullMove();
            NodeType nodeType = NodeType.Alpha;

            for (int i = 0; i < legalCaptures.Length; i++)
            {
                TotalNodes++;
                Move move = legalCaptures[i];
                board.MakeMove(move);
                int score = -Quiesce(-beta, -alpha);
                board.UndoMove(move);

                if (score >= beta)
                {
                    nodeType = NodeType.Beta;
                    best_value = score;
                    bestLocalMove = move;
                    break;
                }
                if (score > best_value)
                {
                    best_value = score;
                    bestLocalMove = move;
                }
                if (score > alpha)
                {
                    alpha = score;
                    nodeType = NodeType.Exact;
                }
            }

            // Save in TT
            TranspositionTable[key] = new TTEntry
            {
                ZobristKey = key,
                Depth = 0,
                Eval = best_value,
                Type = nodeType,
                BestMove = bestLocalMove
            };

            return best_value;
        }

        void PrintInfoMessage()
        {
            long elapsedMs = Math.Max(1, searchTime.ElapsedMilliseconds);
            long nps = (TotalNodes * 1000L) / elapsedMs;

            Console.WriteLine($"info depth {Depth} score cp {bestEval} nodes {TotalNodes} nps {nps} time {searchTime.ElapsedMilliseconds} pv {Notation.MoveToAlgebraic(bestMove)}");
        }
    }
}
