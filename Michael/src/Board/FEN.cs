using Michael.src.Helpers;
using Michael.src.MoveGen;
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

            //Color to move
            board.ColorToMove = fenParts[1] == "w" ? Piece.White : Piece.Black; // Set color to move

            //Castling rights
            string castlingRightsString = fenParts[2];
            board.CasltingRight = 0; // Initialize castling rights]
            if (castlingRightsString != "-")
            {
                if (castlingRightsString.Contains('K')) board.CasltingRight |= CastlingRights.WhiteShort;
                if (castlingRightsString.Contains('Q')) board.CasltingRight |= CastlingRights.WhiteLong;
                if (castlingRightsString.Contains('k')) board.CasltingRight |= CastlingRights.BlackShort;
                if (castlingRightsString.Contains('q')) board.CasltingRight |= CastlingRights.BlackLong;
            }

            //En passant
            if (fenParts[3] != "-")
                board.EnPassantSquare = Notation.SquareToIndex(fenParts[3]);

            // Halfmove clock (for 50-move rule)
            if (fenParts.Length > 4)
                board.HalfmoveClock = int.Parse(fenParts[4]);
            else
                board.HalfmoveClock = 0;

            if (fenParts.Length > 5)
                board.plyCount = int.Parse(fenParts[5]);
        }
    }
}