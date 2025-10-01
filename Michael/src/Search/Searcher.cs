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
        private Move bestMove;
        private Move bestMoveThisIteration;
        private int bestEval;
        private int bestEvalThisIteration;
        private bool searchCancelled;
        private ulong nodes;
        private const int PositiveInfinity = 1_000_000;
        private const int NegativeInfinity = -1_000_000;
        private Stopwatch searchTimer = new Stopwatch();

        //Refrances
        readonly Evaluator evaluator;
        readonly MoveOrderer moveOrderer;
        readonly StaticExchangeEvaluator staticExchangeEvaluator;

        // PV storage
        private Move[,] pvMoves = new Move[256, 256]; // [ply,depth]
        private int[] pvLength = new int[256];

        // Transposition table
        private const int TTBits = 20; // 2^20 entries ≈ 1M entries ≈ 32 MB
        public const int TTSize = 1 << TTBits;
        private TTEntry[] TranspositionTable = new TTEntry[TTSize];

        // Futility margins by depth (in centipawns)
        private const int FutilityMargin = 200;

        // Events
        public event Action<Move> OnSearchComplete;


        public Searcher()
        {
            board = MatchManager.board;
            evaluator = new Evaluator();
            moveOrderer = new MoveOrderer();
            staticExchangeEvaluator = new StaticExchangeEvaluator();
        }

        public void EndSearch()
        {
            searchCancelled = true;
        }

        /// <summary>
        /// Starts a new search in the current position.
        /// resets the necessary variables and begins an iterative deepening.
        /// </summary>
        public void StartNewSearch()
        {
            moveOrderer.ResetKillers();
            searchCancelled = false;
            bestMove = Move.NullMove;

            board = MatchManager.board;
            StartIlliterateDeepeningSearch();

            OnSearchComplete?.Invoke(bestMove);
        }

        /// <summary>
        /// Perform an iterative deepening search with aspiration windows.
        /// ilerative deepening means we search depth 1, then depth 2, then depth 3, etc. until we reach the max depth or the search is cancelled.
        /// this means that it doesn't matter when the search is cancelled, we will always have a best move from the last completed iteration.
        /// it may seem slow and inefficent, but it is actually faster in practice due to better move ordering and the ability to use aspiration windows.
        /// </summary>
        private void StartIlliterateDeepeningSearch()
        {
            //EXAPLIN ASPIRATION WINDOW
            const int InitialAspiration = 50;   // initial +/- window
            const int MaxAspiration = 5000;     // cap to avoid infinite widening loops

            // iterative deepening
            for (Depth = 1; Depth <= 255; Depth++)
            {
                //reset the variable for this iteration. ready up for a new search
                nodes = 0;
                searchTimer.Restart();
                bestMoveThisIteration = Move.NullMove;
                bestEvalThisIteration = 0;

                // If this is the first iteration we don't have a prior eval -> full window
                bool havePrevEval = (Depth > 1) && bestEval != 0;
                int asp = InitialAspiration;
                int score;

                if (!havePrevEval)
                {
                    // no previous info: full-window search
                    score = Search(Depth, NegativeInfinity, PositiveInfinity, 0);
                }
                else
                {
                    // aspiration loop: try a narrow window around previous iteration's eval,
                    // widen on fail-low / fail-high until the result fits or we fall back to full width.
                    int alpha = bestEval - asp;
                    int beta = bestEval + asp;

                    // clamp to infinities
                    if (alpha <= NegativeInfinity) alpha = NegativeInfinity + 1;
                    if (beta >= PositiveInfinity) beta = PositiveInfinity - 1;

                    while (true)
                    {
                        bestMoveThisIteration = Move.NullMove;
                        score = Search(Depth, alpha, beta, 0);

                        // searchCancelled handling: Search returns 0 if cancelled in your code.
                        if (searchCancelled && Depth > 1)
                            break;

                        if (score <= alpha)
                        {
                            // fail-low: result <= alpha -> widen downward
                            asp = Math.Min(asp * 2, MaxAspiration);
                            alpha = bestEval - asp;

                            // if we've widened to or beyond full window, do a full-width search
                            if (asp >= MaxAspiration || alpha <= NegativeInfinity)
                            {
                                score = Search(Depth, NegativeInfinity, PositiveInfinity, 0);
                                break;
                            }

                            // keep beta as previous alpha+1 to search exact failing side (optional),
                            // but to be safe we keep a symmetric window around bestEval:
                            beta = bestEval + asp;
                            continue;
                        }
                        else if (score >= beta)
                        {
                            // fail-high: result >= beta -> widen upward
                            asp = Math.Min(asp * 2, MaxAspiration);
                            beta = bestEval + asp;

                            if (asp >= MaxAspiration || beta >= PositiveInfinity)
                            {
                                score = Search(Depth, NegativeInfinity - 1, PositiveInfinity + 1, 0);
                                break;
                            }

                            alpha = bestEval - asp;
                            continue;
                        }
                        else
                        {
                            // inside window: success
                            break;
                        }
                    }
                }

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

            if (depth <= 0)
            {
                return Quiesce(alpha, beta);
            }

            Move ttMove = Move.NullMove;

            if (TT.TryGetEntry(TranspositionTable, board.CurrentHash, out var entry))
            {
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

            // Null-move pruning
            // Preconditions: not in check, depth sufficiently large, and not in extension context
            if (!board.IsInCheck() && depth >= 3 && numExt == 0)
            {
                // Reduction R: choose based on depth (consistent with other reductions using depth/6)
                int R = 2 + (depth / 6);
                // Make a null move on the board.
                board.MakeNullMove();
                int score = -Search(depth - 1 - R, -beta, -beta + 1, plyFromRoot + 1, numExt + 1);
                board.UndoNullMove();


                if (score >= beta)
                {
                    // Fail-hard beta cutoff from null move search. Store as beta node in TT and return.
                    TT.StoreEntry(ref TranspositionTable, board.CurrentHash, depth, score, NodeType.Beta, Move.NullMove);
                    return score;
                }
            }

            legalMoves = moveOrderer.OrderMoves(legalMoves, depth == Depth ? bestMove : ttMove, plyFromRoot);

            Move bestMoveAtNode = Move.NullMove;
            int bestScore = NegativeInfinity;

            for (int moveIndex = 0; moveIndex < legalMoves.Length; moveIndex++)
            {
                if (searchCancelled && Depth > 1)
                {
                    return 0;
                }

                int extensions = 0;

                Move move = legalMoves[moveIndex];

                if (depth == 1                  // only at depth=1 (or small depth)
        && Depth > 1                     // only if we already finished at least one search
        && !board.IsInCheck()        // not in check
        && board.Squares[move.TargetSquare] == Piece.None         // quiet move
        && !move.IsPromotion())
                {
                    int staticEval = evaluator.Evaluate();
                    if (staticEval + FutilityMargin <= alpha)
                    {
                        // skip this move entirely
                        continue;
                    }
                }

                nodes++;
                if (board.IsInCheck() && numExt < 16)
                {
                    extensions = 1;
                }
                board.MakeMove(move);

                int score;
                if (depth >= 3 &&
                moveIndex >= 3 &&                  // not first few moves
                !board.IsInCheck() &&              // don’t reduce when in check
                board.Squares[move.TargetSquare] == Piece.None &&         // don’t reduce captures/promotions
                !move.IsPromotion())
                {
                    int reduction = 1 + (moveIndex / 6) + (depth / 6);
                    score = -Search(depth - 1 - reduction, -beta, -alpha, plyFromRoot + 1, numExt + extensions);

                    //Move looks promising - do a full re-search
                    if (score > alpha)
                    {
                        score = -Search(depth - 1 + extensions, -beta, -alpha, plyFromRoot + 1, numExt + extensions);
                    }
                }
                //Perfrom a full search
                else
                {
                    score = -Search(depth - 1 + extensions, -beta, -alpha, plyFromRoot + 1, numExt + extensions);
                }
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
                    moveOrderer.AddHistory(move, Piece.PieceType(board.Squares[move.StartingSquare]), depth);
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
            captures = moveOrderer.OrderMoves(captures, ttMove, 255);

            Move bestMoveThisNode = Move.NullMove;
            NodeType nodeType = NodeType.Alpha;

            for (int i = 0; i < captures.Length; i++)
            {
                if (searchCancelled && Depth > 1)
                    return 0;

                Move move = captures[i];

                nodes++;

                board.MakeMove(move);
                int score = -Quiesce(-beta, -alpha);
                board.UndoMove(move);

                if (searchCancelled && Depth > 1)
                {
                    return 0;
                }

                if (score >= beta)
                {
                    nodeType = NodeType.Beta;
                    bestValue = score;
                    bestMoveThisNode = move;
                    break;
                }

                if (score > bestValue)
                {
                    bestValue = score;
                    bestMoveThisNode = move;
                }

                if (score > alpha)
                {
                    alpha = score;
                    nodeType = NodeType.Exact;
                }
            }

            TT.StoreEntry(ref TranspositionTable, board.CurrentHash, 0, bestValue, nodeType, bestMoveThisNode);

            return bestValue;
        }

        private void PrintInfoMessage()
        {
            ulong elapsedMs = (ulong)searchTimer.ElapsedMilliseconds;
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
