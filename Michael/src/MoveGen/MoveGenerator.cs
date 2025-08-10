using Michael.src.Helpers;

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

        private static ulong enemyBitboardAndEmptySquares; //Stores the enemy bitboard and empty squares for legal move generation.

        //Stores all the legal moves the knight can make from a given square.
        //Precomputed before the game starts in the PrecomputeMoveData class.
        public static ulong[] KnightMoves = new ulong[64];

        public static Move[] GenerateLegalMoves(Board boardInstance)
        {
            board = boardInstance; //Set the board to the current board instance.
            Init(); //Initialize all the necessary variables for move generation.

            Move[] legalMoves = new Move[MaxLegalMoves]; // Create an array to hold the legal moves.

            GenerateLegalPawnMoves(ref legalMoves); // Generate all the legal pawn moves and add them to the legal moves array.
            GenerateLegalKnightMoves(ref legalMoves); // Generate all the legal knight moves and add them to the legal moves array.


            //Convert the array to a span and slice to the amount of legal moves in the position and return as an array.
            //This is done to no return 218 moves when there are less than that in the position.
            return legalMoves.AsSpan().Slice(0, CurrentMoveIndex).ToArray(); 
        }

        /// <summary>
        /// Generates all the legal moves for a pawn piece and return to the given legalMoves array.
        /// </summary>
        /// <param name="legalMoves">the array to return the legal pawn moves</param>
        public static void GenerateLegalPawnMoves(ref Move[] legalMoves)
        {
            ulong pawnBitboard = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Pawn, board.ColorToMove)];

            int moveDirection = board.ColorToMove == Piece.White ? 1 : -1; // Determine the move direction based on the color to move.
            ulong oneRankPush = BitboardHelper.ShiftBitboard(pawnBitboard, 8 * moveDirection) & board.ColoredBitboards[2];
            BitboardHelper.PrintBitboard(board.ColoredBitboards[2]);
            while (oneRankPush != 0)
            {
                int targetSquare = BitboardHelper.PopLSB(ref oneRankPush); // Get the square of the pawn piece.
                int startingSquare = targetSquare - (8 * moveDirection); // Calculate the starting square of the pawn.
                legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
            }
        }

        /// <summary>
        /// Generates all the legal moves for a knight piece and return to the given legalMoves array.
        /// </summary>
        /// <param name="legalMoves">the array to return the legal knight moves</param>
        public static void GenerateLegalKnightMoves(ref Move[] legalMoves)
        {
            ulong knightBitboard = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Knight, board.ColorToMove)];
            while (knightBitboard != 0)
            {
                int knightSquare = BitboardHelper.PopLSB(ref knightBitboard); // Get the square of the knight piece.
                ulong attacks = KnightMoves[knightSquare] & enemyBitboardAndEmptySquares; // Get the precomputed moves for the knight from that square.
                // Iterate through all possible moves and add them to the legal moves array.
                while (attacks != 0)
                {
                    int targetSquare = BitboardHelper.PopLSB(ref attacks);
                    legalMoves[CurrentMoveIndex++] = new Move(knightSquare, targetSquare);
                }
            }
        }

        //Sets up all the necessary variables for move generation.
        //only called from the start of generateLegalMoves. NOT from main.
        private static void Init()
        {
            CurrentMoveIndex = 0; // Reset the current move index to 0.
            enemyBitboardAndEmptySquares = ~board.ColoredBitboards[board.ColorToMove]; // Get the enemy bitboard and empty squares.
        }
    }
}