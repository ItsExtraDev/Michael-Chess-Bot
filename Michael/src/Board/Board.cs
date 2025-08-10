using Michael.src.Helpers;
using Michael.src.MoveGen;

namespace Michael.src
{
    /// <summary>
    /// Represents the main board class in the chess engine.
    /// 
    /// Manages all board-related state, including:
    /// - Piece bitboards
    /// - Turn tracking
    /// - Check status
    /// - Castling rights, en passant, etc.
    ///
    /// Provides core methods such as:
    /// - <c>MakeMove</c>: Executes a given move.
    /// - <c>GetLegalMoves</c>: Returns all legal moves for the current position.
    /// 
    /// This class serves as the central interface for querying and updating board state.
    /// </summary>
    public class Board
    {
        /// <summary>
        /// This project uses bitboards for move generation and board evaluation.
        /// Bitboards are an efficient way to represent the board and pieces, 
        /// with bitboards each piece type has its own bitboard. This allows us to
        /// avoid looping through the entire board — instead, we analyze binary numbers 
        /// to determine occupied squares and possible attacks.
        ///
        /// Each index in the array corresponds to a different piece type and color:
        /// index 0 = white pawn, index 1 = white knight, etc.
        ///
        /// More information can be found at: https://www.chessprogramming.org/Bitboards
        /// </summary>
        public ulong[] PiecesBitboards = new ulong[12];
        public ulong[] ColoredBitboards = new ulong[3];

        /// <summary>
        /// Each index in the array correlates to the corresponding square in the board.
        /// Unlike bitboards, which are very fast to show which square is occupied and by
        /// which color, this array is mostly used to determaine which piece is occuping the square,
        /// which is not possible with regular bitboards.
        /// </summary>
        public int[] Squares = new int[64];

        //Current turn of the game.
        public int ColorToMove;

        //The game state used to track the current state of the game.
        public int CurrentGameState;

        //Contains the history of game states for undo functionality.
        public List<int> GameStateHistory = new List<int>();

        //Create the array before the game starts to avoid allocating memory every time we need to generate legal moves.
        Move[] legalMoves;

        /// <summary>
        /// Instantiates a board and automatically loads the starting position.
        /// The position can be changed by passing a custom FEN string.
        /// </summary>
        /// <param name="fenString">The FEN string representing the position (defaults to starting position).</param>
        public Board(string fenString = FEN.StartingFEN)
        {
            LoadFen(fenString);
        }

        /// <summary>
        /// Sets up the board state based on a given FEN string.
        /// </summary>
        /// <param name="fenString">The FEN string representing the position.</param>
        private void LoadFen(string fenString)
        {
            FEN.LoadFEN(this, fenString);
            CurrentGameState = GameState.MakeGameState(Piece.None, Piece.None); // Initialize the game state
        }

        /// <summary>
        /// Gets all the legal moves in the current position and returns them as an array of moves.
        /// </summary>
        /// <returns>An array of all the legal moves in the position</returns>
        public Move[] GetLegalMoves()
        {
            legalMoves = MoveGenerator.GenerateLegalMoves(this);

            return legalMoves;
        }

        /// <summary>
        /// Makes a move on the board. updates bitboards and square array.
        /// </summary>
        /// <param name="move"></param>
        public void MakeMove(Move move)
        {
            //Console.WriteLine($"Making move: {move.StartingSquare} {move.TargetSquare} {Squares[move.StartingSquare]} {ColorToMove}");
            //BitboardHelper.PrintBitboard(PiecesBitboards[1]);
            //BoardHelper.PrintBoard(this); // Print the board before making the move for debugging purposes
            //BitboardHelper.PrintBitboard(PiecesBitboards[2]);
            int movingPiece = Squares[move.StartingSquare];
            int movingPieceType = Piece.PieceType(movingPiece);
            //Console.WriteLine(movingPiece);
            //Console.WriteLine(BitboardHelper.GetBitboardIndex(movingPieceType, ColorToMove));
            //Console.WriteLine(movingPieceType);
            int movingBitboardIndex = BitboardHelper.GetBitboardIndex(movingPieceType, ColorToMove);
            ref ulong movingBitboard = ref PiecesBitboards[movingBitboardIndex];
            int CapturedPiece = Squares[move.TargetSquare];
            //Console.WriteLine("B");
            Squares[move.StartingSquare] = Piece.None; // Clear the starting square
            Squares[move.TargetSquare] = movingPiece; // Place the piece on the target square
            BitboardHelper.MovePiece(ref movingBitboard, move.StartingSquare, move.TargetSquare); // Update the bitboard
            BitboardHelper.MovePiece(ref ColoredBitboards[ColorToMove], move.StartingSquare, move.TargetSquare); // Update the colored bitboard
            BitboardHelper.MovePiece(ref ColoredBitboards[2], move.StartingSquare, move.TargetSquare); // Remove the starting square from the empty squares bitboard
            // Console.WriteLine("C");

            GameStateHistory.Add(CurrentGameState); // Add the current game state to history
            CurrentGameState = GameState.MakeGameState(CapturedPiece, movingPiece); // Update the game state with the captured piece and moving piece
            ColorToMove ^= 1; // Switch the turn to the other player (0 for white, 1 for black)

            if (CapturedPiece != Piece.None)
            {
                //Console.WriteLine("D");
                // If a piece was captured, remove it from the board and its bitboard
                int capturedPieceType = Piece.PieceType(CapturedPiece);                      //The color of the captured piece is always the opposite color of the moving player.
                int capturedBitboardIndex = BitboardHelper.GetBitboardIndex(capturedPieceType, ColorToMove);
                ref ulong capturedBitboard = ref PiecesBitboards[capturedBitboardIndex];
                BitboardHelper.ToggleBit(ref capturedBitboard, move.TargetSquare); // Remove the captured piece from its bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[ColorToMove], move.TargetSquare); // Remove from the colored bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], move.TargetSquare);
                // Console.WriteLine("D2");
            }
                //TODO promotion logic, en passant logic, and caslting logic
           // Console.WriteLine("E");
        }

        public void UndoMove(Move move)
        {
            int movingPiece = Squares[move.TargetSquare];
            int movingPieceType = Piece.PieceType(movingPiece);
            int movingBitboardIndex = BitboardHelper.GetBitboardIndex(movingPieceType, ColorToMove ^ 1);
            ref ulong movingBitboard = ref PiecesBitboards[movingBitboardIndex];
            int capturedPiece = GameState.CapturedPiece(CurrentGameState);

            Squares[move.TargetSquare] = capturedPiece; // Clear the target square
            Squares[move.StartingSquare] = movingPiece; // Place the piece back on the starting square
            BitboardHelper.MovePiece(ref movingBitboard, move.TargetSquare, move.StartingSquare); // Update the bitboard
            BitboardHelper.MovePiece(ref ColoredBitboards[ColorToMove ^ 1], move.TargetSquare, move.StartingSquare); // Update the colored bitboard
            BitboardHelper.MovePiece(ref ColoredBitboards[2], move.StartingSquare, move.TargetSquare); // Add the target square back to the empty squares bitboard

            if (capturedPiece != Piece.None)
            {
                // If a piece was captured, restore it to the board and its bitboard
                int capturedPieceType = Piece.PieceType(capturedPiece);
                int capturedBitboardIndex = BitboardHelper.GetBitboardIndex(capturedPieceType, ColorToMove);
                ref ulong capturedBitboard = ref PiecesBitboards[capturedBitboardIndex];
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], move.StartingSquare);
                BitboardHelper.ToggleBit(ref capturedBitboard, move.TargetSquare); // Restore the captured piece to its bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[ColorToMove], move.TargetSquare); // Restore to the colored bitboard
            }

            CurrentGameState = GameStateHistory.ElementAt(GameStateHistory.Count - 1); // Restore the previous game state from history
            GameStateHistory.RemoveAt(GameStateHistory.Count - 1); // Remove the last game state from history
            ColorToMove ^= 1; // Switch the turn back to the previous player (0 for white, 1 for black)
        }
    }
}