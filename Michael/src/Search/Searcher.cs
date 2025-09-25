using Michael.src.Evaluation;
using System.Diagnostics;
using System.Text;

namespace Michael.src.Search
{

    public class Searcher
    {
        // Variable Declarations
        private Board board;
        private int Depth;
        private const byte MaxDepth = 255;
        private Move bestMove;
        private Move bestMoveThisIteration;
        private int bestEval;
        private int bestEvalThisIteration;
        private bool searchCancelled;
        private ulong nodes;
        private const int PositiveInfinity = 999_999;
        private const int NegativeInfinity = -999_999;
        private Stopwatch searchTimer = new Stopwatch();

        //Refrances
        public event Action<Move> OnSearchComplete;
        readonly Evaluator evaluator;
        readonly MoveOrderer moveOrderer;

        // PV storage
        private Move[,] pvMoves = new Move[256, 256]; // [ply,depth]
        private int[] pvLength = new int[256];

        // Transposition table
        private const int TTBits = 20; // 2^20 entries ≈ 1M entries ≈ 32 MB
        public const int TTSize = 1 << TTBits;
        private TTEntry[] TranspositionTable = new TTEntry[TTSize];

        public Searcher()
        {
            board = MatchManager.board;
            evaluator = new Evaluator();
            moveOrderer = new MoveOrderer();
        }

        public void EndSearch()
        {
            searchCancelled = true;
        }

        public void StartNewSearch()
        {
            moveOrderer.ResetKillers();
            searchCancelled = false;
            bestMove = Move.NullMove;

            board = MatchManager.board;
            StartIlliterateDeepeningSearch();

            OnSearchComplete?.Invoke(bestMove);
        }

        private void StartIlliterateDeepeningSearch()
        {
            for (Depth = 1; Depth <= MaxDepth; Depth++)
            {
                nodes = 0;
                searchTimer.Restart();
                bestMoveThisIteration = Move.NullMove;
                bestEvalThisIteration = 0;


                Search(Depth, NegativeInfinity, PositiveInfinity, 0);

                if (!bestMoveThisIteration.IsNull())
                {
                    bestEval = bestEvalThisIteration;
                    bestMove = bestMoveThisIteration;
                    PrintInfoMessage();
                }

                if (searchCancelled && Depth > 1)
                {
                    break;
                }
            }
        }

        private int Search(int depth, int alpha, int beta, int plyFromRoot, int numExt = 0)
        {
            if (searchCancelled && Depth > 1)
            {
                return 0;
            }

            if (board.IsDraw())
                return 0;

            Move[] legalMoves = board.GetLegalMoves();

            if (legalMoves.Length == 0)
            {
                if (board.IsInCheck())
                    return NegativeInfinity + plyFromRoot;
                return 0;
            }

            if (depth == 0)
            {
                return Quiesce(alpha, beta);
            }

            Move ttMove = Move.NullMove;

            if (TT.TryGetEntry(TranspositionTable, board.CurrentHash, out var entry)) {
                //even if we did a search but the result if from a lesser depth, it is irrelevent to us. don't use the result.
                if (entry.Depth >= depth)
                {
                    if (entry.Type == NodeType.Exact)
                        return entry.Eval;
                    if (entry.Type == NodeType.Alpha && entry.Eval <= alpha)
                        return alpha;
                    if (entry.Type == NodeType.Beta && entry.Eval >= beta)
                        return beta;

                    ttMove = entry.BestMove;
                }
            }

            legalMoves = moveOrderer.OrderMoves(legalMoves, Depth == depth ? bestMove : ttMove, plyFromRoot);

            Move bestMoveAtNode = Move.NullMove;
            int bestScore = NegativeInfinity;

            for (int moveIndex = 0; moveIndex < legalMoves.Length; moveIndex++)
            {
                int extensions = 0;

                Move move = legalMoves[moveIndex];

                nodes++;
                if (board.IsInCheck() && numExt < 16)
                {
                    extensions = 1;
                }
                board.MakeMove(move);
                int score = -Search(depth - 1 + extensions, -beta, -alpha, plyFromRoot + 1, numExt + extensions);
                board.UndoMove(move);

                if (searchCancelled && Depth > 1)
                {
                    return 0;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoveAtNode = move;

                    if (numExt == 0)
                    {
                        pvMoves[depth, 0] = move;
                        for (int i = 0; i < pvLength[depth - 1]; i++)
                            pvMoves[depth, i + 1] = pvMoves[depth - 1, i];
                        pvLength[depth] = pvLength[depth - 1] + 1;

                        if (Depth == depth)
                        {
                            bestMoveThisIteration = move;
                            bestEvalThisIteration = score;
                        }
                    }
                }
                if (bestScore > alpha)
                {
                    alpha = bestScore;
                }
                if (alpha >= beta)
                {
                    moveOrderer.AddKiller(move, plyFromRoot);
                   // moveOrderer.AddHistory(move, Piece.PieceType(board.Squares[move.StartingSquare]), depth);
                    break;
                }
            }

            TT.StoreEntry(ref TranspositionTable, board.CurrentHash, depth, bestScore,
                    bestScore >= beta
                    ? NodeType.Beta
                    : (bestScore > alpha ? NodeType.Exact : NodeType.Alpha), 
                    bestMoveAtNode);

            return bestScore;
        }

        private int Quiesce(int alpha, int beta)
        {
            if (searchCancelled && Depth > 1)
            {
                return 0;
            }

            ulong key = board.CurrentHash;
            Move ttMove = Move.NullMove;

            if (TT.TryGetEntry(TranspositionTable, board.CurrentHash, out var entry))
            {
                if (entry.Depth >= 0)
                {
                    if (entry.Type == NodeType.Exact)
                        return entry.Eval;
                    if (entry.Type == NodeType.Alpha && entry.Eval <= alpha)
                        return alpha;
                    if (entry.Type == NodeType.Beta && entry.Eval >= beta)
                        return beta;

                    ttMove = entry.BestMove;
                }
            }

            int staticEval = evaluator.Evaluate();
            int bestValue = staticEval;

            if (bestValue >= beta)
                return bestValue;

            if (bestValue > alpha)
                alpha = bestValue;

            Move[] captures = board.GetLegalMoves(true);
            captures = moveOrderer.OrderMoves(captures, Move.NullMove, 255);

            Move bestMove = Move.NullMove;
            NodeType nodeType = NodeType.Alpha;

            for (int i = 0; i < captures.Length; i++)
            {
                if (searchCancelled && Depth > 1)
                    break;

                Move move = captures[i];
                nodes++;

                board.MakeMove(move);
                int score = -Quiesce(-beta, -alpha);
                board.UndoMove(move);

                if (score >= beta)
                {
                    nodeType = NodeType.Beta;
                    bestValue = score;
                    bestMove = move;
                    break;
                }

                if (score > bestValue)
                {
                    bestValue = score;
                    bestMove = move;
                }

                if (score > alpha)
                {
                    alpha = score;
                    nodeType = NodeType.Exact;
                }
            }

            TT.StoreEntry(ref TranspositionTable, board.CurrentHash, 0, bestValue, nodeType, bestMove);

            return bestValue;
        }

        private void PrintInfoMessage()
        {
            ulong elapsedMs = (ulong) searchTimer.ElapsedMilliseconds;
            if (elapsedMs == 0) elapsedMs = 1;
            ulong nps = nodes * 1000 / elapsedMs;

            Console.WriteLine(
                $"info depth {Depth} score cp {bestEval} nodes {nodes} nps {nps} time {elapsedMs} pv {GetPVLine(Depth)}"
            );
        }

        private string GetPVLine(int depth)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < pvLength[depth]; i++)
                sb.Append(pvMoves[depth, i] + " ");

            return sb.ToString().Trim();
        }
    }
}
