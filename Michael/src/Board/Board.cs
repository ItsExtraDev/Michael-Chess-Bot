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
        }

        /// <summary>
        /// Gets all the legal moves in the current position and returns them as an array of moves.
        /// </summary>
        /// <returns>An array of all the legal moves in the position</returns>
        public Move[] GetLegalMoves()
        {
            Move[] legalMoves = MoveGenerator.generateLegalMoves(this);

            return legalMoves;
        }

        /// <summary>
        /// Makes a move on the board. updates bitboards and square array.
        /// </summary>
        /// <param name="move"></param>
        public void MakeMove(Move move)
        {
            int movingPiece = Squares[move.StartingSquare];
            int movingPieceType = Piece.PieceType(movingPiece);
            int movingBitboardIndex = BitboardHelper.GetBitboardIndex(movingPieceType, ColorToMove);
            ref ulong movingBitboard = ref PiecesBitboards[movingBitboardIndex];
            int CapturedPiece = Squares[move.TargetSquare];

            Squares[move.StartingSquare] = Piece.None; // Clear the starting square
            Squares[move.TargetSquare] = movingPiece; // Place the piece on the target square
            BitboardHelper.MovePiece(ref movingBitboard, move.StartingSquare, move.TargetSquare); // Update the bitboard
            BitboardHelper.MovePiece(ref ColoredBitboards[ColorToMove], move.StartingSquare, move.TargetSquare); // Update the colored bitboard

            if (CapturedPiece != Piece.None)
            {
                // If a piece was captured, remove it from the board and its bitboard
                int capturedPieceType = Piece.PieceType(CapturedPiece);                      //The color of the captured piece is always the opposite color of the moving player.
                int capturedBitboardIndex = BitboardHelper.GetBitboardIndex(capturedPieceType, ColorToMove ^ 1 );
                ref ulong capturedBitboard = ref PiecesBitboards[capturedBitboardIndex];
                BitboardHelper.ToggleBit(ref capturedBitboard, move.TargetSquare); // Remove the captured piece from its bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[ColorToMove ^ 1], move.TargetSquare); // Remove from the colored bitboard
            }
            //TODO promotion logic, en passant logic, and caslting logic
        }
    }
}