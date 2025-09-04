namespace Michael.src.Search
{
    public class Searcher
    {
        public event Action<Move>? OnSearchComplete;
        private bool searchCancelled;
        private Board board;
        private Move bestMove;

        public Searcher()
        {
            board = MatchManager.board;
            bestMove = Move.NullMove();
        }

        public void EndSearch()
        {
            searchCancelled = true;
        }


        public void StartNewSearch()
        {
            searchCancelled = false;


            StartIlliterateDeepeningSearch();

            OnSearchComplete?.Invoke(bestMove);
        }

        private void StartIlliterateDeepeningSearch()
        {
        }

        public int Search(int depth)
        {
            return 0;
        }

    }
}
