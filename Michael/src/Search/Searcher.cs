using Michael.src.Evaluation;
using Michael.src.Helpers;
using System.Diagnostics;
using System.Formats.Tar;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Michael.src.Search
{
    public class Searcher
    {
        // Variable Declarations
        private bool searchCancelled;
        private Move bestMove;
        private Move bestMoveThisIteration;
        private int bestEval;
        private int bestEvalThisIteration;
        private const int PositiveInfinity = 1000000;
        private const int NegativeInfinity = -1000000;
        private byte Depth;
        private Board board;
        private int totalNodes;
        private Stopwatch stopwatch;

        private const int TTMaxSize = 10_000_000;
        private static Dictionary<ulong, TTEntry> TranspositionTable = new Dictionary<ulong, TTEntry>(TTMaxSize);

        //references
        readonly Evaluator evaluator;
        readonly MoveOrderer moveOrderer;

        // public stuff
        public event Action<Move> OnSearchComplete;

        public Searcher()
        {
            evaluator = new Evaluator();
            moveOrderer = new MoveOrderer();
            bestMove = bestMoveThisIteration = Move.NullMove();
            board = MatchManager.board;
            stopwatch = new Stopwatch();
        }

        public void EndSearch()
        {
            searchCancelled = true;
        }

        public void StartNewSearch()
        {
            board = MatchManager.board;
            searchCancelled = false;
            bestMove = bestMoveThisIteration = Move.NullMove();
            StartIlliterateDeepeningSearch();

            OnSearchComplete?.Invoke(bestMove);
        }

        private void StartIlliterateDeepeningSearch()
        {
            // clear TT once at the start of the whole search
            TranspositionTable.Clear();

            for (Depth = 1; Depth < 255; Depth++)
            {
                totalNodes = 0;

                stopwatch.Restart();
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

            // If we never found a root move, try to recover from TT (optional)
            if (bestMove == Move.NullMove())
            {
                ulong key = Zobrist.ComputeHash(board);
                if (TranspositionTable.TryGetValue(key, out TTEntry entry))
                {
                    bestMove = entry.bestMove;
                }
            }
        }

        private int Search(int depth, int alpha, int beta)
        {
            if (searchCancelled && Depth > 1)
                return 0;

            int originalAlpha = alpha;

            // Transposition Table Lookup
            ulong key = Zobrist.ComputeHash(board);
            if (TranspositionTable.TryGetValue(key, out TTEntry entry) && entry.Depth >= depth)
            {
                if (entry.NodeType == NodeType.Exact)
                    return entry.Evaluation;
                else if (entry.NodeType == NodeType.Lowerbound && entry.Evaluation >= beta)
                    return entry.Evaluation;
                else if (entry.NodeType == NodeType.Upperbound && entry.Evaluation <= alpha)
                    return entry.Evaluation;
            }

            if (board.IsDraw())
                return 0;

            if (board.IsCheckmate())
            {
                int plyFromRoot = Depth - depth;
                return NegativeInfinity + plyFromRoot;
            }

            if (depth == 0)
                return Quiesce(alpha, beta);

            int bestScore = NegativeInfinity;
            Move bestMoveAtNode = Move.NullMove(); // local best move for this node
            Move[] legalMoves = board.GetLegalMoves();
            moveOrderer.OrderMoves(ref legalMoves, Depth == depth ? bestMove : Move.NullMove(), board, Depth - depth);

            NodeType nodeType = NodeType.Upperbound;

            for (int i = 0; i < legalMoves.Length; i++)
            {
                if (searchCancelled && Depth > 1)
                    break;

                Move move = legalMoves[i];
                totalNodes++;

                board.MakeMove(move);
                bool isCapture = GameState.IsCapture(board.CurrentGameState);
                int score = -Search(depth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (searchCancelled && Depth > 1)
                    break;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoveAtNode = move;

                    if (depth == Depth)
                    {
                        bestEvalThisIteration = score;
                        bestMoveThisIteration = move;
                    }
                }

                if (bestScore > alpha)
                {
                    alpha = bestScore;
                    // don't set nodeType to Exact yet — decide after loop by comparing to originalAlpha/beta
                }

                if (alpha >= beta)
                {
                    //Stop killer move
                    if (!isCapture)
                    {
                        moveOrderer.AddKillerMove(move, Depth - depth);
                    }

                    // fail-high
                    nodeType = NodeType.Lowerbound;
                    break;
                }
            }

            // determine node type correctly using originalAlpha/beta
            if (bestScore <= originalAlpha)
            {
                nodeType = NodeType.Upperbound;
            }
            else if (bestScore >= beta)
            {
                nodeType = NodeType.Lowerbound;
            }
            else
            {
                nodeType = NodeType.Exact;
            }

            // Store in TT: use the local bestMoveAtNode
            TranspositionTable[key] = new TTEntry
            {
                ZobristKey = key,
                Depth = depth,
                Evaluation = bestScore,
                NodeType = nodeType,
                bestMove = bestMoveAtNode
            };

            return bestScore;
        }

        private int Quiesce(int alpha, int beta)
        {
            if (searchCancelled && Depth > 1)
            {
                return 0;
            }

            ulong key = Zobrist.ComputeHash(board);
            if (TranspositionTable.TryGetValue(key, out TTEntry entry) && entry.Depth >= 0)
            {
                if (entry.NodeType == NodeType.Exact)
                    return entry.Evaluation;
                if (entry.NodeType == NodeType.Upperbound && entry.Evaluation <= alpha)
                    return alpha;
                if (entry.NodeType == NodeType.Lowerbound && entry.Evaluation >= beta)
                    return beta;
            }

            int staticEval = evaluator.Evaluate();
            int bestValue = staticEval;

            if (bestValue >= beta)
                return bestValue;
            if (bestValue > alpha)
                alpha = bestValue;

            Move[] captures = board.GetLegalMoves(true);
            moveOrderer.OrderMoves(ref captures, Move.NullMove(), board, 0);

            Move bestMove = Move.NullMove();
            NodeType nodeType = NodeType.Upperbound;

            for (int i = 0; i < captures.Length; i++)
            {
                if (searchCancelled && Depth > 1)
                {
                    break;
                }

                Move move = captures[i];
                board.MakeMove(move);
                totalNodes++;
                int score = -Quiesce(-beta, -alpha);
                board.UndoMove(move);

                if (searchCancelled && Depth > 1)
                {
                    break;
                }

                if (score >= beta)
                {
                    nodeType = NodeType.Lowerbound;
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

            TranspositionTable[key] = new TTEntry
            {
                ZobristKey = key,
                Depth = 0,
                Evaluation = bestValue,
                NodeType = nodeType,
                bestMove = bestMove
            };

            return bestValue;
        }

        private void PrintInfoMessage()
        {
            long elapsedMs = Math.Max(1, stopwatch.ElapsedMilliseconds);
            long nps = (long)totalNodes * 1000 / elapsedMs;
            Console.WriteLine($"info depth {Depth} score cp {bestEval} nodes {totalNodes} nps {nps} time {elapsedMs} pv {Notation.MoveToAlgebraic(bestMove)}");
        }

    }
}

