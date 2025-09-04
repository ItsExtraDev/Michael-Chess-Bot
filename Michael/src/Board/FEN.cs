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

        public static string BoardToFen(Board board, bool alwaysIncludeEPSquare = true)
        {
            string fen = "";
            for (int rank = 7; rank >= 0; rank--)
            {
                int numEmptyFiles = 0;
                for (int file = 0; file < 8; file++)
                {
                    int i = rank * 8 + file;
                    int piece = board.Squares[i];
                    if (piece != 0)
                    {
                        if (numEmptyFiles != 0)
                        {
                            fen += numEmptyFiles;
                            numEmptyFiles = 0;
                        }
                        bool isBlack = Piece.Color(piece) == Piece.Black;
                        int pieceType = Piece.PieceType(piece);
                        char pieceChar = ' ';
                        switch (pieceType)
                        {
                            case Piece.Rook:
                                pieceChar = 'R';
                                break;
                            case Piece.Knight:
                                pieceChar = 'N';
                                break;
                            case Piece.Bishop:
                                pieceChar = 'B';
                                break;
                            case Piece.Queen:
                                pieceChar = 'Q';
                                break;
                            case Piece.King:
                                pieceChar = 'K';
                                break;
                            case Piece.Pawn:
                                pieceChar = 'P';
                                break;
                        }
                        fen += (isBlack) ? pieceChar.ToString().ToLower() : pieceChar.ToString();
                    }
                    else
                    {
                        numEmptyFiles++;
                    }

                }
                if (numEmptyFiles != 0)
                {
                    fen += numEmptyFiles;
                }
                if (rank != 0)
                {
                    fen += '/';
                }
            }

            // Side to move
            fen += ' ';
            fen += (board.ColorToMove == Piece.White) ? 'w' : 'b';

            // Castling
            bool whiteKingside = (board.CasltingRight & 1) == 1;
            bool whiteQueenside = (board.CasltingRight >> 1 & 1) == 1;
            bool blackKingside = (board.CasltingRight >> 2 & 1) == 1;
            bool blackQueenside = (board.CasltingRight >> 3 & 1) == 1;
            fen += ' ';
            fen += (whiteKingside) ? "K" : "";
            fen += (whiteQueenside) ? "Q" : "";
            fen += (blackKingside) ? "k" : "";
            fen += (blackQueenside) ? "q" : "";
            fen += ((board.CasltingRight) == 0) ? "-" : "";

            // En-passant
            fen += ' ';
            int epSquare = board.EnPassantSquare;

            bool isEnPassant = epSquare != 0;
            if (isEnPassant && alwaysIncludeEPSquare)
            {
                fen += Notation.IndexToSquare(epSquare);
            }
            else
            {
                fen += '-';
            }

            // 50 move counter
            fen += ' ';
            fen += board.HalfmoveClock;

            // Full-move count (should be one at start, and increase after each move by black)
            fen += ' ';
            fen += (board.plyCount / 2) + 1;

            return fen;
        }
    }
}