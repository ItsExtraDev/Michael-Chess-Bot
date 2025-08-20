using Michael.src.Helpers;
using System.Diagnostics.Metrics;

namespace Michael.src
{
    /// <summary>
    /// FEN (Forsyth-Edwards Notation) is the most popular and convenient way to load a position using a single string.
    /// Each character in the string represents a different piece type (e.g., 'p' = pawn, 'b' = bishop, etc.).
    /// 
    /// FEN also includes additional game state information, such as:
    /// - Whose turn it is
    /// - Castling rights
    /// - En passant target square
    /// - Halfmove clock (for the fifty-move rule)
    /// - Fullmove number
    /// 
    /// More information can be found at: https://www.chessprogramming.org/Forsyth-Edwards_Notation
    /// </summary>
    public static class FEN
    {
        //The FEN string for the standard, opening position.
        public const string StartingFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static void ResetArrays(Board board)
        {
            Array.Clear(board.PiecesBitboards);
            Array.Clear(board.ColoredBitboards);
            Array.Clear(board.Squares);
            board.ColoredBitboards[2] = ulong.MaxValue; // Initialize the empty squares bitboard to all squares being empty
        }

        /// <summary>
        /// Loads a position onto the board from a FEN string.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="fenString"></param>
        public static void LoadFEN(Board board, string fenString)
        {
            ResetArrays(board);

            string[] fenParts = fenString.Split(' ');

            int rank = 7;
            int file = 0;

            foreach (char letter in fenParts[0])
            {
                if (letter == '/')
                {
                    rank--;
                    file = 0;
                }
                else if (char.IsNumber(letter))
                {
                    file += (int)char.GetNumericValue(letter);
                }
                else
                {
                    int square = rank * 8 + file; // Calculate 0-based square index from rank and file
                    bool isWhite = char.IsUpper(letter); // Uppercase letter = white piece
                    int pieceType = Piece.SymbolToPieceType(letter); // Convert symbol to piece type
                    int piece = Piece.CreatePiece(pieceType, isWhite); // Create piece code combining type and color
                    int bitboardIndex = BitboardHelper.GetBitboardIndex(pieceType, isWhite); // Find bitboard index for piece
                    ref ulong bitboard = ref board.PiecesBitboards[bitboardIndex];
                    board.Squares[square] = piece; // Place piece on board square
                    BitboardHelper.ToggleBit(ref bitboard, square); // Set corresponding bit in bitboard
                    BitboardHelper.ToggleBit(ref board.ColoredBitboards[isWhite ? Piece.White : Piece.Black], square); // Set colored bitboard
                    BitboardHelper.ToggleBit(ref board.ColoredBitboards[2], square); // Set empty squares bitboard
                    file++; // Move to next file (column)

                }
            }

            board.ColorToMove = fenParts[1] == "w" ? Piece.White : Piece.Black; // Set color to move
            if (fenParts[3] != "-")
            board.EnPassantSquare = Notation.SquareToIndex(fenParts[3]);
        }
    }
}