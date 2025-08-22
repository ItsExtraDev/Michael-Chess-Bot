using Michael.src.Bot.Eval;
using Michael.src.Helpers;
using Michael.src.MoveGen;
using System.Diagnostics;
using static System.Formats.Asn1.AsnWriter;

namespace Michael.src.Bot.Search
{

    /*
     * TODO:
     * qsearch
     * time management
     * aspiration windows
     * null move pruning
     * late move reductions
     * transposition table
     * killer moves
     * 
     * opening book
     * add random bit to draw detections to avoid 3-fold repetition draws in even but not drawn positions
     * early game piece square tables
     * end game piece square tables
     * piece mobility
     * mop up score in endgame
     * king safety
     * pawn structure evaluation
     */

    public static class Searcher
    {
        private static Board board;
        private static Move BestMove = Move.NullMove();
        private static Move BestMoveThisIteration;
        private static int MaxInt = 100000;
        private static int MaxDepth = 7;
        private static int Depth;
        private static int NodesSearched;

        public static Move GetBestMove(Board boardInstance)
        {
            board = boardInstance;
            BestMoveThisIteration = Move.NullMove();

            for (Depth = 1; Depth <= MaxDepth; Depth++)
            {
                BestMoveThisIteration = BestMove;
                NodesSearched = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                int eval = Search(Depth, -MaxInt, MaxInt);
                stopwatch.Stop();

                SendInfoMessage(Depth, eval, NodesSearched, NodesSearched / Math.Max(1, (int)stopwatch.Elapsed.Seconds));

            }
            return BestMove;
        }

        public static int Search(int depth,int alpha, int beta)
        {
            if (depth == 0)
            {
                return Quiesce(alpha, beta);
            }

            if (board.IsDraw())
            {
                return 0;
            }

            if (board.IsCheckmate())
            {
                return -MaxInt + board.plyCount; // avoid overflow
            }


            var moves = board.GetLegalMoves();
            MoveOrderer.OrderMoves(board, /*(depth == Depth) ? BestMoveThisIteration : TODO use it!*/ Move.NullMove(), ref moves);

            int bestEval = -MaxInt;
            foreach (var move in moves)
            {
                board.MakeMove(move);
                NodesSearched++;
                int eval = -Search(depth - 1, -beta, -alpha);
                board.UndoMove(move);
                if (eval > bestEval)
                {
                    bestEval = eval;
                    if (depth == Depth)
                    {
                        BestMove = move;
                    }
                }
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                {
                    break; // beta cutoff
                }
            }
            return bestEval;
        }

        private static int Quiesce(int alpha, int beta)
        {
            int static_eval = Evaluator.Evaluate(board);

            // Stand Pat
            int best_value = static_eval;
            if (best_value >= beta)
                return best_value;
            if (best_value > alpha)
                alpha = best_value;

            Move[] legalMoves = board.GetLegalMoves(true);
            MoveOrderer.OrderMoves(board, Move.NullMove(), ref legalMoves);
            foreach (Move move in legalMoves.Reverse())  {
                board.MakeMove(move);
                int score = -Quiesce(-beta, -alpha);
                board.UndoMove(move);

                if (score >= beta)
                    return score;
                if (score > best_value)
                    best_value = score;
                if (score > alpha)
                    alpha = score;
            }

            return best_value;
        }


        public static void SendInfoMessage(int depth, int eval, int nodes, int nps)
        {
            Console.WriteLine($"info depth {depth} score cp {eval} nodes {nodes} nps {nps} pv {Notation.MoveToAlgebraic(BestMove)}");
        }
    }
}