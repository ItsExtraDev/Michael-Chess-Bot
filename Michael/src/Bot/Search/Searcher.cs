using Michael.src.Bot.Eval;
using Michael.src.Helpers;
using Michael.src.MoveGen;
using System.Diagnostics;

namespace Michael.src.Bot.Search
{
    /*
     * TODO:
     * 
     * Search:
     * negaminx
     * alpha beta pruning
     * qsearch
     * illetrive deepening
     * time managment
     * move ordering
     * aspiration windows
     * null move pruning
     * late move reductions
     * transposition table
     * killer moves
     * 
     * Evaluation:
     * mg piece table
     * eg piece table
     * mop up
     * piece values
     * opening book
     * piece mobility
     * king safety
     * pawn structure evaluation
     */

    public static class Searcher
    {
        private static Board board;
        private static Move BestMove;
        private static int MaxDepth = 5;
        private static int BigNumber = 10000;
        private static string[] pvMoves = new string[MaxDepth];
        private static int TotalNodes;


        public static Move GetBestMove(Board boardInstance, Clock MatchClock)
        {
            board = boardInstance;

            BestMove = Move.NullMove();
            TotalNodes = 0;

            Stopwatch stopwatch = Stopwatch.StartNew();

            int eval = Search(MaxDepth, -BigNumber, BigNumber);
            stopwatch.Stop();

            SendInfoMessage(eval, (int)stopwatch.ElapsedMilliseconds);

            return BestMove;
        }

        /// <summary>
        /// Performs a search on the current position based on the negamax algorithm.
        /// in a simplified version, it plays every legal move in the board to some depth, and evaluats the board
        /// and chooses the move that leades to the best evaluation.
        /// more information can be found at: https://www.chessprogramming.org/Negamax
        /// </summary>
        /// <param name="depth">how many moves ahead should we look?</param>
        /// <returns>the evaluation of the board state when looked to the depth of {depth}</returns>
        public static int Search(int depth, int alpha, int beta)
        {
            if (board.IsDraw())
                return 0;

            if (board.IsCheckmate())
                return -BigNumber + board.plyCount;

            if (depth == 0)
                return Evaluator.Evaluate(board);

            Move[] legalMoves = board.GetLegalMoves();
            MoveOrderer.OrderMoves(board, ref legalMoves);

            int bestEvaluation = -BigNumber;
            foreach (Move move in legalMoves)
            {
                TotalNodes++;
                board.MakeMove(move);
                int eval = -Search(depth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (eval > bestEvaluation)
                {
                    bestEvaluation = eval;
                    pvMoves[MaxDepth - depth] = Notation.MoveToAlgebraic(move);
                    if (depth == MaxDepth)
                    {
                        BestMove = move;
                    }
                    alpha = Math.Max(eval, alpha);
                    if (alpha >= beta)
                        break; //Cut off
                }
            }

            return bestEvaluation;
        }

        /// <summary>
        /// Sends a message with useful message to the GUI, such as what depth we are searching,
        /// how many nodes we looked at, what is the best line, etc.
        /// </summary>
        public static void SendInfoMessage(int eval, int searchTimeInMS)
        {
            Console.WriteLine($"info depth {MaxDepth} score cp {eval} nodes {TotalNodes} nps {TotalNodes * (Math.Max(0.001, searchTimeInMS) * 1000)} pv {string.Join(' ', pvMoves)}");
        }
    }
}
