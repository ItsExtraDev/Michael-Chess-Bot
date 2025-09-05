using Michael.src.Evaluation;
using Michael.src.Helpers;
using System.Diagnostics;

namespace Michael.src.Search
{
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

        //Refrances
        readonly LogWriter writer;
        readonly Evaluator evaluator;


        public Searcher()
        {
            board = MatchManager.board;
            bestMove = bestMoveThisIteration = Move.NullMove();

            writer = new LogWriter(FileType.Search, true);
            evaluator = new Evaluator();
        }

        public void EndSearch()
        {
            searchCancelled = true;
        }

        /// <summary>
        /// Start a new search on the board, to find the best move
        /// </summary>
        public void StartNewSearch()
        {
            searchCancelled = false;
            board = MatchManager.board;
            bestMove = Move.NullMove();
            bestEvalThisIteration = NegativeInfinity;

            StartIlliterateDeepeningSearch();

            OnSearchComplete?.Invoke(bestMove);
        }

        /// <summary>
        /// Perform an illiterative deepening search. which means we first perform a search to
        /// a depth of 1, then we perfrom a search at a depth of 2 and so on, until we run out of time for the current search.
        /// it may look extremly inefficent and slow, but thanks to move ordering and alpha beta pruning, it can
        /// often lead to faster results then a regular search
        /// </summary>
        private void StartIlliterateDeepeningSearch()
        {
            for (Depth = 1; Depth <= MaxDepth; Depth++)
            {
                searchTime.Restart();

                Search(Depth);

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

        public int Search(int depth)
        {
            //We have spent too long in the search.
            //time to perform an hard stop, and return the best move so far.
            if (searchCancelled)
            {
                return 0;
            }

            if (board.IsDraw())
                return 0;

            if (board.IsCheckmate())
            {
                int plyFromRoot = Depth - depth;
                return NegativeInfinity + board.plyCount;
            }

            if (depth == 0)
            {
                return evaluator.Evaluate();
            }

            int bestScore = NegativeInfinity;

            Move[] legalMoves = board.GetLegalMoves();

            for (int moveIndex = 0; moveIndex < legalMoves.Length; moveIndex++)
            {
                TotalNodes++;
                Move move = legalMoves[moveIndex];

                board.MakeMove(move);
                int score = -Search(depth - 1);
                board.UndoMove(move);

                if (score > bestScore)
                {
                    bestScore = score;

                    if (depth == Depth)
                    {
                        bestEvalThisIteration = score;
                        bestMoveThisIteration = move;
                    }
                }
            }

            return bestScore;
        }

        void PrintInfoMessage()
        {
            long nps = (TotalNodes / (Math.Max(1, searchTime.ElapsedMilliseconds) * 1000));
            Console.WriteLine($"info depth {Depth} score cp {bestEval} nodes {TotalNodes} nps {nps} time {searchTime.ElapsedMilliseconds} pv {Notation.MoveToAlgebraic(bestMove)}");
        }

    }
}
