using System.Numerics;

namespace Michael.src.Helpers
{
    /// <summary>
    /// Provides helper methods for working with bitboards,
    /// including calculating bitboard indexes and manipulating individual bits.
    /// </summary>
    public static class BitboardHelper
    {
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
            for (int i = 0; i < 64; i++)
            {
                if (IsBitSet(bitboard, i))
                {
                    Console.Write("1 ");
                }
                else
                {
                    Console.Write(". ");
                }
                if ((i + 1) % 8 == 0)
                {
                    Console.WriteLine();
                }
            }
        }
    }
}
