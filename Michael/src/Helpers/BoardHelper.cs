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
            => square / 8;

        /// <summary>
        /// Calculates the file of the given square
        /// </summary>
        /// <param name="square"></param>
        /// <returns>The file of the given square</returns>
        public static int File(int square)
            => square % 8;

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

        public static ulong GetAttackTunnel(int square1, int square2, bool IsRook)
        {

            if (IsRook)
            {
                if ((Magic.GetRookAttacks(square1, 0) & 1ul << square2) != 0)
                    return Magic.GetRookAttackMask(square1, 1ul<<square2) & Magic.GetRookAttackMask(square2, 1ul<<square1);
                return 0;
            }
            if ((Magic.GetBishopAttacks(square1, 0) & 1ul << square2) != 0)
                return Magic.GetBishopAttackMask(square1, 1ul << square2) & Magic.GetBishopAttackMask(square2, 1ul<<square1);
            return 0;
        }

        public static ulong GetPinAttackTunnel(int square1, int square2, bool IsRook)
        {

            if (IsRook)
            {
                if ((Magic.GetRookAttacks(square1, 0) & 1ul << square2) != 0)
                    return Magic.GetRookAttacks(square1, 0) & Magic.GetRookAttacks(square2, 0);
                return 0;
            }
            if ((Magic.GetBishopAttacks(square1, 0) & 1ul << square2) != 0)
                return Magic.GetBishopAttacks(square1, 0) & Magic.GetBishopAttacks(square2, 0);
            return 0;
        }
    }
}