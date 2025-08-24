using Michael.src.Bot.Eval;
using Michael.src.Helpers;
using Michael.src.MoveGen;
using System.Diagnostics;

namespace Michael.src.Bot.Search
{
    /* * TODO:
     * 
     * Search:
     * aspiration windows 
     * null move pruning 
     * late move reductions 
     * killer moves 
     * 
     * Evaluation: 
     * opening book 
     * piece mobility 
     * king safety 
     * pawn structure evaluation
     * 
     * More:
     * improve time managment
     * */

    public static class Searcher
    {
        private static Board board;
        private static Move BestMove;
        private static int MaxDepth = 3;
        private static int BigNumber = 100000;
        private static int Depth;
        private static int TotalNodes;

        private static string[][] pvTable;
        private static string[] pvMoves;

        public static Move GetBestMove(Board boardInstance, Clock matchClock)
        {
            board = boardInstance;
            // Initialize PV Table
            pvTable = new string[MaxDepth + 1][];
            for (int i = 0; i <= MaxDepth; i++)
                pvTable[i] = new string[MaxDepth + 1];

            // Dynamic time allocation
            int buffer = 50; // ms reserved as safety
            int softTime = Math.Max(matchClock.TimeLeftInMS / Math.Max(30, matchClock.MovesToGo), 50); // base time
            softTime += matchClock.Incrament / 2; // use part of increment
            int hardTime = softTime * 3; // absolute emergency stop
            if (softTime > matchClock.TimeLeftInMS - buffer) softTime = matchClock.TimeLeftInMS - buffer;

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Iterative Deepening
            for (Depth = 1; Depth <= MaxDepth; Depth++)
            {
                TotalNodes = 0;

                int eval = Search(Depth, -BigNumber, BigNumber);

                SendInfoMessage(eval, (int)stopwatch.ElapsedMilliseconds);

                // Soft limit check
                if (stopwatch.ElapsedMilliseconds >= softTime)
                    break;

                // Hard limit emergency stop
                if (stopwatch.ElapsedMilliseconds >= hardTime)
                    break;
            }

            stopwatch.Stop();

            return BestMove;
        }

        /// <summary>
        /// Negamax with Alpha-Beta and PV Table
        /// </summary>
        private static int Search(int depth, int alpha, int beta)
        {
            ulong hash = Zobrist.ComputeHash(board);

            // Transposition Table Lookup
            if (TranspositionTable.TryGet(hash, out TTEntry ttEntry))
            {
                if (ttEntry.Depth >= depth)
                {
                    if (ttEntry.Type == NodeType.Exact) return ttEntry.Score;
                    if (ttEntry.Type == NodeType.Alpha && ttEntry.Score <= alpha) return alpha;
                    if (ttEntry.Type == NodeType.Beta && ttEntry.Score >= beta) return beta;
                }
            }

            if (board.IsDraw())
                return 0;

            if (board.IsCheckmate()) 
                return -BigNumber + board.plyCount;

            if (depth == 0)
                return QSearch(alpha, beta);

            Move[] legalMoves = board.GetLegalMoves();
            MoveOrderer.OrderMoves(board, ref legalMoves, pvMove: BestMove);


            int bestEval = -BigNumber;
            Move bestMove = Move.NullMove();

            for (int i = 0; i < legalMoves.Length; i++)
            {
                Move move = legalMoves[i];

                TotalNodes++;
                board.MakeMove(move);
                int eval = -Search(depth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (eval > bestEval)
                {
                    bestEval = eval;
                    bestMove = move;

                    // Update PV
                    pvTable[depth][0] = Notation.MoveToAlgebraic(move);
                    if (depth > 1)
                        Array.Copy(pvTable[depth - 1], 0, pvTable[depth], 1, MaxDepth - 1);

                    if (depth == Depth)
                    {
                        BestMove = move;
                        pvMoves = pvTable[depth].Where(m => m != null).ToArray();
                    }

                    alpha = Math.Max(alpha, eval);
                    if (alpha >= beta) break; // Cut-off
                }
            }

            // Store in TT
            NodeType nodeType = NodeType.Exact;
            if (bestEval <= alpha) nodeType = NodeType.Alpha;
            else if (bestEval >= beta) nodeType = NodeType.Beta;

            TranspositionTable.Store(hash, new TTEntry
            {
                ZobristKey = hash,
                Depth = depth,
                Score = bestEval,
                Type = nodeType,
                BestMove = bestMove
            });

            return bestEval;
        }

        /// <summary>
        /// Quiescence Search for better leaf evaluation
        /// </summary>
        private static int QSearch(int alpha, int beta)
        {
            int standPat = Evaluator.Evaluate(board);
            if (standPat >= beta) return beta;
            if (alpha < standPat) alpha = standPat;

            Move[] captures = board.GetLegalMoves(true);
            MoveOrderer.OrderMoves(board, ref captures);

            foreach (Move move in captures)
            {
                TotalNodes++;
                board.MakeMove(move);
                int score = -QSearch(-beta, -alpha);
                board.UndoMove(move);

                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }
            return alpha;
        }

        /// <summary>
        /// UCI Info Output
        /// </summary>
        public static void SendInfoMessage(int eval, int searchTimeInMS)
        {
            double nps = (TotalNodes / Math.Max(1, searchTimeInMS)) * 1000;
            Console.WriteLine($"info depth {Depth} score cp {eval} nodes {TotalNodes} nps {nps} pv {string.Join(' ', pvMoves ?? Array.Empty<string>())}");
        }

        /// <summary>
        /// If a move is a capture, promotion or a check, returns false.
        /// otherwise true
        /// </summary>
        /// <returns>If a move is quiet</returns>
        public static bool IsMoveQuiet(Move move)
        {
            if (move.IsPromotion())
                return false;

            board.MakeMove(move);
            if (GameState.IsCapture(board.CurrentGameState) || board.IsInCheck())
                return false;
            board.UndoMove(move);
            return true;
        }
    }
}
