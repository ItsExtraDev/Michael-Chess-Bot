using Michael.src.Helpers;
using Michael.src.MoveGen;
using System;
using System.Diagnostics.Metrics;

namespace Michael.src
{
    /// <summary>
    /// Handles Forsyth-Edwards Notation (FEN) parsing and generation for the chess engine.
    /// FEN is a standard way to represent a chess position using a single string.
    /// 
    /// A FEN string encodes:
    /// - Piece placement (letters 'p', 'r', 'n', 'b', 'q', 'k')
    /// - Active color (w/b)
    /// - Castling rights (KQkq)
    /// - En passant target square
    /// - Halfmove clock (for 50-move rule)
    /// - Fullmove number
    /// 
    /// Reference: https://www.chessprogramming.org/Forsyth-Edwards_Notation
    /// </summary>
    public static class FEN
    {
        /// <summary>
        /// FEN string representing the standard chess starting position.
        /// </summary>
        public const string StartingFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        /// <summary>
        /// Resets all board arrays (bitboards and squares) to empty.
        /// Also initializes the empty squares bitboard to all ones.
        /// </summary>
        /// <param name="board">Board to reset.</param>
        public static void ResetArrays(Board board)
        {
            Array.Clear(board.PiecesBitboards);
            Array.Clear(board.ColoredBitboards);
            Array.Clear(board.Squares);
            board.ColoredBitboards[2] = ulong.MaxValue; // all squares empty
        }

        /// <summary>
        /// Loads a position onto the board from a FEN string.
        /// </summary>
        /// <param name="board">Board to populate.</param>
        /// <param name="fenString">FEN string representing the position.</param>
        public static void LoadFEN(Board board, string fenString)
        {
            ResetArrays(board);

            string[] fenParts = fenString.Split(' ');

            int rank = 7;
            int file = 0;

            // Piece placement
            foreach (char letter in fenParts[0])
            {
                if (letter == '/')
                {
                    rank--;
                    file = 0;
                }
                else if (char.IsNumber(letter))
                {
                    file += (int)char.GetNumericValue(letter); // skip empty squares
                }
                else
                {
                    int square = rank * 8 + file;
                    bool isWhite = char.IsUpper(letter);
                    int pieceType = Piece.SymbolToPieceType(letter);
                    int piece = Piece.CreatePiece(pieceType, isWhite);
                    int bitboardIndex = BitboardHelper.GetBitboardIndex(pieceType, isWhite);
                    ref ulong bitboard = ref board.PiecesBitboards[bitboardIndex];

                    board.Squares[square] = piece;
                    BitboardHelper.ToggleBit(ref bitboard, square);
                    BitboardHelper.ToggleBit(ref board.ColoredBitboards[isWhite ? Piece.White : Piece.Black], square);
                    BitboardHelper.ToggleBit(ref board.ColoredBitboards[2], square); // update empty squares

                    file++;
                }
            }

            // Active color
            board.ColorToMove = fenParts[1] == "w" ? Piece.White : Piece.Black;

            // Castling rights
            string castlingRightsString = fenParts[2];
            board.CasltingRight = 0;
            if (castlingRightsString != "-")
            {
                if (castlingRightsString.Contains('K')) board.CasltingRight |= CastlingRights.WhiteShort;
                if (castlingRightsString.Contains('Q')) board.CasltingRight |= CastlingRights.WhiteLong;
                if (castlingRightsString.Contains('k')) board.CasltingRight |= CastlingRights.BlackShort;
                if (castlingRightsString.Contains('q')) board.CasltingRight |= CastlingRights.BlackLong;
            }

            // En passant target square
            if (fenParts[3] != "-")
                board.EnPassantSquare = Notation.SquareToIndex(fenParts[3]);

            // Halfmove clock (50-move rule)
            if (fenParts.Length > 4)
                board.HalfmoveClock = int.Parse(fenParts[4]);
            else
                board.HalfmoveClock = 0;

            // Ply count / fullmove number
            if (fenParts.Length > 5)
                board.plyCount = int.Parse(fenParts[5]);
        }
    }
}
