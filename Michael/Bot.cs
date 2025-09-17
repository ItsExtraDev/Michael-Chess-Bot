using Michael.src.Helpers;
using Michael.src.Search;

namespace Michael
{
    /// <summary>
    /// The Bot class represents the chess engine's "thinking" component.
    /// It manages search timing, search start/stop, and communicates chosen moves to the UCI interface.
    /// </summary>
    public class Bot
    {
        // --- Settings ---

        /// <summary>
        /// If true, the bot will always use the maximum allowed time per move.
        /// </summary>
        public bool UseMaxTimePerMove = true;

        /// <summary>
        /// Maximum time to think per move in milliseconds.
        /// </summary>
        public int MaxTimePerMoveInMS = 10000;

        // --- State variables ---

        /// <summary>
        /// True if the bot is currently thinking/searching for a move.
        /// </summary>
        public bool IsThinking;

        /// <summary>
        /// Event invoked when the bot has chosen a move.
        /// Subscribed to by the UCI interface to send moves to the GUI.
        /// </summary>
        public event Action<string>? OnMoveChosen;

        // --- References ---

        /// <summary>
        /// Reference to the Searcher, which performs the actual move search.
        /// </summary>
        private readonly Searcher searcher;

        /// <summary>
        /// Cancellation token source used to stop the search when:
        /// 1. Maximum allocated thinking time has elapsed.
        /// 2. The GUI sends a "stop" command.
        /// </summary>
        CancellationTokenSource? cancelSearchTimer;

        /// <summary>
        /// Initializes a new instance of the Bot class.
        /// Subscribes to the Searcher's OnSearchComplete event.
        /// </summary>
        public Bot()
        {
            IsThinking = false;
            searcher = new Searcher();
            searcher.OnSearchComplete += OnSearchComplete;
        }

        /// <summary>
        /// Starts thinking for a limited time (timed search).
        /// Cancels any previous ongoing search.
        /// </summary>
        /// <param name="timeToThinkMS">Time to think in milliseconds.</param>
        public void StartThinkingTimed(int timeToThinkMS)
        {
            IsThinking = true;

            // Cancel any previous search
            cancelSearchTimer?.Cancel();

            // Start a new search with the specified time limit
            StartNewSearch(timeToThinkMS);
        }

        /// <summary>
        /// Starts a new search task and sets up the cancellation timer.
        /// </summary>
        /// <param name="timeToThinkMS">Time to think in milliseconds.</param>
        private void StartNewSearch(int timeToThinkMS)
        {
            cancelSearchTimer = new CancellationTokenSource();
            var localCancel = cancelSearchTimer;

            // Schedule a task to end the search after the specified time
            Task.Delay(timeToThinkMS, localCancel.Token).ContinueWith((t) =>
            {
                // Only stop if the task was not canceled and this is still the active search
                if (!t.IsCanceled && localCancel == cancelSearchTimer && IsThinking)
                {
                    EndSearch();
                }
            });

            // Begin the actual search asynchronously
            searcher.StartNewSearch();
        }

        /// <summary>
        /// Event handler called by the Searcher when a move search completes.
        /// Sends the move to subscribers (e.g., UCI interface).
        /// </summary>
        /// <param name="move">The best move found by the search.</param>
        void OnSearchComplete(Move move)
        {
            IsThinking = false;

            // Convert internal Move object to UCI algebraic notation
            string moveName = Notation.MoveToAlgebraic(move);

            // Notify subscribers (UCI) about the chosen move
            OnMoveChosen?.Invoke(moveName);
        }

        /// <summary>
        /// Stops the current search by force.
        /// Called when either the GUI sends a stop command or maximum time has elapsed.
        /// </summary>
        public void EndSearch()
        {
            // Cancel the timer task
            cancelSearchTimer?.Cancel();

            // Stop the searcher if it is still thinking
            if (IsThinking)
            {
                searcher.EndSearch();
            }

            // Update state
            IsThinking = false;
        }
    }
}
