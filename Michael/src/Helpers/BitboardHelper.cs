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
        /// Toggles (flips) the bit at the specified index in the given bitboard.
        /// </summary>
        /// <param name="Bitboard">Reference to the bitboard to modify.</param>
        /// <param name="bitIndex">The zero-based index of the bit to toggle (0-63).</param>
        public static void ToggleBit(ref ulong Bitboard, int bitIndex)
        {
            Bitboard ^= 1UL << bitIndex;
        }
    }
}
