namespace Michael.src.MoveGen
{
    /// <summary>
    /// Provides methods for generating legal moves for a given board position.
    /// Handles all legal move generation, like move generation, legal validation, capture logic etc.
    /// generateLegalMoves is the main method and should be called only from the board.GetLegalMoves() Function.
    /// </summary>
    public static class MoveGenerator
    {
        //The board we are generating moves for.
        //Being passed from the board.GetLegalMoves() function.
        private static Board board;

        //Maximum number of legal moves in a position, used for move generation array.
        //This is possible in the position: R6R/3Q4/1Q4Q1/4Q3/2Q4Q/Q4Q2/pp1Q4/kBNN1KB1 w - - 0 1, composed by Nenad Petrovic.
        public const int MaxLegalMoves = 218;

        //Current index in the legal moves array, used to track the position in the array.
        private static int CurrentMoveIndex = 0;

        public static Move[] generateLegalMoves(Board boardInstance)
        {
            Board board = boardInstance; //Set the board to the current board instance.
            Init(); //Initialize all the necessary variables for move generation.

            Move[] legalMoves = new Move[MaxLegalMoves]; // Create an array to hold the legal moves.

            //Convert the array to a span and slice to the amount of legal moves in the position and return as an array.
            //This is done to no return 218 moves when there are less than that in the position.
            return legalMoves.AsSpan().Slice(0, CurrentMoveIndex).ToArray(); 
        }

        //Sets up all the necessary variables for move generation.
        //only called from the start of generateLegalMoves. NOT from main.
        private static void Init()
        {
            CurrentMoveIndex = 0; // Reset the current move index to 0.
        }
    }
}