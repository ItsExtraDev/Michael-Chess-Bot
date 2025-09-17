using Michael.src.MoveGen;

namespace Michael.src.Helpers
{
    /// <summary>
    /// Provides helper methods for working with the board class,
    /// including converting squares to ranks, printing the board etc.
    /// </summary>
    public static class BoardHelper
    {
        /// <summary>
        /// Calculates the rank of the given square
        /// </summary>
        /// <param name="square"></param>
        /// <returns>The rank of the given square</returns>
        public static int Rank(int square)
            => square >> 3;

        /// <summary>
        /// Calculates the file of the given square
        /// </summary>
        /// <param name="square"></param>
        /// <returns>The file of the given square</returns>
        public static int File(int square)
            => square & 7;

        /// <summary>
        /// Prints a copy of the board to command promt.
        /// mainly used to help visualize and for debugging
        /// </summary>
        /// <param name="board"></param>
        public static void PrintBoard(Board board)
        {
            Console.WriteLine(" +---+---+---+---+---+---+---+---+");
            for (int rank = 7; rank >= 0; rank--)
            {
                Console.Write(" | ");
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    int piece = board.Squares[square];
                    int pieceType = Piece.PieceType(piece);
                    char Symbol = Piece.PieceTypeToSymbol(pieceType);
                    Symbol = Piece.IsWhite(piece) ? char.ToUpper(Symbol) : char.ToLower(Symbol);

                    Console.Write(Symbol + " | ");
                }
                Console.WriteLine((rank + 1));
                Console.WriteLine(" +---+---+---+---+---+---+---+---+");
            }
            Console.WriteLine("   a   b   c   d   e   f   g   h");
        }

        /// <summary>
        /// Returns the bitboard representing the "attack tunnel" between two squares.
        /// This is used mainly for x-ray attacks for rooks and bishops.
        /// </summary>
        /// <param name="square1">The attacking piece's square.</param>
        /// <param name="square2">The target square.</param>
        /// <param name="IsRook">True if the piece is a rook, false for bishop.</param>
        /// <returns>A bitboard of squares that lie between the two squares in the line of attack, or 0 if not aligned.</returns>
        public static ulong GetAttackTunnel(int square1, int square2, bool IsRook)
        {
            if (IsRook)
            {
                // Check if the target is on the rook's attack line
                if ((Magic.GetRookAttacks(square1, 0) & (1ul << square2)) != 0)
                    return Magic.GetRookAttackMask(square1, 1ul << square2)
                         & Magic.GetRookAttackMask(square2, 1ul << square1);
                return 0;
            }

            // Bishop case
            if ((Magic.GetBishopAttacks(square1, 0) & (1ul << square2)) != 0)
                return Magic.GetBishopAttackMask(square1, 1ul << square2)
                     & Magic.GetBishopAttackMask(square2, 1ul << square1);

            return 0; // Not aligned
        }
    }
}