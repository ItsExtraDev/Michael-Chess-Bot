using Michael.src.MoveGen;
using System.Numerics;

namespace Michael.src.Helpers
{
    /// <summary>
    /// Provides helper methods for working with bitboards,
    /// including calculating bitboard indexes and manipulating individual bits.
    /// </summary>
    public static class BitboardHelper
    {
        //Rank masks
        public const ulong Rank1 = 0x00000000000000FF;
        public const ulong Rank2 = 0x000000000000FF00;
        public const ulong Rank3 = 0x0000000000FF0000;
        public const ulong Rank4 = 0x00000000FF000000;
        public const ulong Rank5 = 0x000000FF00000000;
        public const ulong Rank6 = 0x0000FF0000000000;
        public const ulong Rank7 = 0x00FF000000000000;
        public const ulong Rank8 = 0xFF00000000000000;

        //File Masks 
        public const ulong FileA = 0x0101010101010101;
        public const ulong FileB = 0x0202020202020202;
        public const ulong FileC = 0x0404040404040404;
        public const ulong FileD = 0x0808080808080808;
        public const ulong FileE = 0x1010101010101010;
        public const ulong FileF = 0x2020202020202020;
        public const ulong FileG = 0x4040404040404040;
        public const ulong FileH = 0x8080808080808080;



        /// <summary>
        /// Calculates the index of the bitboard for a given piece type and color.
        /// </summary>
        /// <param name="pieceType">The type of the piece (Pawn, Knight, etc.).</param>
        /// <param name="color">The color of the piece (White or Black).</param>
        /// <returns>The index corresponding to the bitboard for the specified piece type and color.</returns>
        public static int GetBitboardIndex(int pieceType, int color)
            => (color * 6) + pieceType - 1;

        /// <summary>
        /// Calculates the index of the bitboard for a given piece type and color, using a boolean for color.
        /// </summary>
        /// <param name="pieceType">The type of the piece.</param>
        /// <param name="isWhite">True if the piece is white; false if black.</param>
        /// <returns>The index corresponding to the bitboard for the specified piece type and color.</returns>
        public static int GetBitboardIndex(int pieceType, bool isWhite)
            => GetBitboardIndex(pieceType, isWhite ? Piece.White : Piece.Black);

        /// <summary>
        /// Checks if the bit at the specified index is set (1) in the given bitboard.
        /// </summary>
        /// <param name="Bitboard">The bitboard to check.</param>
        /// <param name="bitIndex">The zero-based index of the bit to check (0-63).</param>
        /// <returns>True if the bit is set; otherwise, false.</returns>
        public static bool IsBitSet(ulong Bitboard, int bitIndex)
            => (Bitboard & 1UL << bitIndex) != 0;

        /// <summary>
        /// Returns and toggles off the least significant bit (LSB) of the bitboard.
        /// </summary>
        /// <param name="Bitboard">The bitboard</param>
        /// <returns>the LSB</returns>
        public static int PopLSB(ref ulong Bitboard)
        {
            int LSB = BitOperations.TrailingZeroCount(Bitboard);
            ToggleBit(ref Bitboard, LSB);

            return LSB;
        }

        /// <summary>
        /// Toggles (flips) the bit at the specified index in the given bitboard.
        /// </summary>
        /// <param name="Bitboard">Reference to the bitboard to modify.</param>
        /// <param name="bitIndex">The zero-based index of the bit to toggle (0-63).</param>
        public static void ToggleBit(ref ulong Bitboard, int bitIndex)
        {
            Bitboard ^= 1UL << bitIndex;
        }

        /// <summary>
        /// Shifts the bits in the bitboard by the specified number of positions.
        /// </summary>
        /// <param name="Bitboard">The bitboard to shift</param>
        /// <param name="shift">How many places should we shift?</param>
        /// <returns>The shifted bitboard</returns>
        public static ulong ShiftBitboard(ulong Bitboard, int shift)
        {
            if (shift > 0)
            {
                return Bitboard << shift;
            }
            else if (shift < 0)
            {
                return Bitboard >> -shift;
            }
            return Bitboard; // No shift
        }

        /// <summary>
        /// Move the piece in the bitboard from squareA to squareB.
        /// </summary>
        /// <param name="Bitboard"></param>
        /// <param name="square1"></param>
        /// <param name="square2"></param>
        public static void MovePiece(ref ulong Bitboard, int squareA, int squareB)
        {
            ToggleBit(ref Bitboard, squareA);
            ToggleBit(ref Bitboard, squareB);
        }

        /// <summary>
        /// Prints a bitboard to the console in a human-readable format.
        /// used for debugging purposes to visualize the bitboard.
        /// </summary>
        /// <param name="bitboard">The bitboard to print</param>
        public static void PrintBitboard(ulong bitboard)
        {
            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    char symbol = IsBitSet(bitboard, square) ? '1' : '.';
                    Console.Write(symbol + " ");
                }
                Console.WriteLine();
            }
        }

        public static ulong GetFileMask(int file) 
            => ShiftBitboard(FileA, file);

        public static ulong GetAdjecentFilesBitboard(int file)
        {
            ulong mask = 0;
            if (file > 0) mask |= GetFileMask(file - 1);
            if (file < 7) mask |= GetFileMask(file + 1);
            return mask;
        }


        public static ulong GetPassedPawnMask(int square, int color)
        {
            int file = BoardHelper.File(square);
            int rank = BoardHelper.Rank(square);

            ulong filesMask = GetFileMask(file) | GetAdjecentFilesBitboard(file);
            ulong forwardMask;

            if (color == 0) // White
            {
                forwardMask = ~((1UL << ((rank + 1) * 8)) - 1); // Clear all ranks ≤ current

                if (rank == 7)
                    forwardMask = ulong.MinValue;
            }
            else // Black
            {
                forwardMask = (1UL << (rank * 8)) - 1; // Keep ranks below current

                if (rank == 0)
                    forwardMask = ulong.MinValue;
            }

            return filesMask & forwardMask;
        }

        /// <summary>
        /// Returns a bitboard mask of all squares behind the given square (on the same file),
        /// relative to the pawn's color.
        /// For white: squares below the current rank.
        /// For black: squares above the current rank.
        /// </summary>
        /// <param name="square">The square index (0-63).</param>
        /// <param name="color">0 for white, 1 for black.</param>
        /// <returns>A bitboard with backward squares set to 1.</returns>
        public static ulong GetBackwardMask(int square, int color)
        {
            int file = BoardHelper.File(square);
            int rank = BoardHelper.Rank(square);

            ulong fileMask = GetFileMask(file);

            if (color == 0) // White
            {
                // All ranks below current rank
                ulong lowerRanksMask = (1UL << (rank * 8)) - 1;
                return fileMask & lowerRanksMask;
            }
            else // Black
            {
                // All ranks above current rank
                ulong upperRanksMask = ~((1UL << ((rank + 1) * 8)) - 1);
                return fileMask & upperRanksMask;
            }
        }


    }
}
