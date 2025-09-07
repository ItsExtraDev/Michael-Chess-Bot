using Michael.src.Helpers;
using Michael.src.Search;
using System.Collections.Generic;

namespace Michael
{
    public class Bot
    {
        //Settings
        public bool UseMaxTimePerMove = true;
        public int MaxTimePerMoveInMS = 100;

        //Variables 
        public bool IsThinking;
        public event Action<string>? OnMoveChosen;
        //Refrances
        private readonly Searcher searcher;

        //Cancellation source used to stop searching when we get the command to stop, or we have thought longer
        //then the maximum allocated time.
        CancellationTokenSource? cancelSearchTimer;

        public Bot()
        {
            IsThinking = false;
            searcher = new Searcher();
            searcher.OnSearchComplete += OnSearchComplete;
        }

        public void StartThinkingTimed(int timeToThinkMS)
        {
            IsThinking = true;

            cancelSearchTimer?.Cancel();
            StartNewSearch(timeToThinkMS);
        }

        private void StartNewSearch(int timeToThinkMS)
        {
            cancelSearchTimer = new CancellationTokenSource();
            var localCancel = cancelSearchTimer;

            Task.Delay(timeToThinkMS, localCancel.Token).ContinueWith((t) =>
            {
                // Only end if we’re still using this token (not a newer search)
                if (!t.IsCanceled && localCancel == cancelSearchTimer && IsThinking)
                {
                    EndSearch();
                }
            });

            searcher.StartNewSearch();
        }


        void OnSearchComplete(Move move)
        {
            IsThinking = false;

            string moveName = Notation.MoveToAlgebraic(move);

            OnMoveChosen?.Invoke(moveName);
        }


        //Stop the search by force, either we spent too much time, or the GUI tells us to stop
        public void EndSearch()
        {
            cancelSearchTimer?.Cancel();
            if (IsThinking)
            {
                searcher.EndSearch();
            }
            //Cancel the search token
            IsThinking = false;
        }
    }
}