using System.Numerics;

namespace Michael.src.Helpers
{
    /// <summary>
    /// Zobrist hashing for the chess engine.
    /// Generates a unique 64-bit hash for each board position using piece positions,
    /// castling rights, en passant squares, and side to move.
    /// </summary>
    public static class Zobrist
    {
        private static readonly Random rng = new Random(1337); // fixed seed for reproducibility

        // Random numbers for each piece type on each square
        private static readonly ulong[,] PieceSquareRandoms = new ulong[12, 64];

        // Random numbers for each castling rights combination (16 possible)
        private static readonly ulong[] CastlingRandoms = new ulong[16];

        // Random numbers for each file for en passant (8 possible)
        private static readonly ulong[] EnPassantRandoms = new ulong[8];

        // Random number for side to move
        private static readonly ulong SideToMoveRandom;

        static Zobrist()
        {
            // Fill piece-square table
            for (int p = 0; p < 12; p++)
                for (int sq = 0; sq < 64; sq++)
                    PieceSquareRandoms[p, sq] = RandomUlong();

            // Fill castling rights table
            for (int i = 0; i < 16; i++)
                CastlingRandoms[i] = RandomUlong();

            // Fill en passant table
            for (int i = 0; i < 8; i++)
                EnPassantRandoms[i] = RandomUlong();

            // Side to move random
            SideToMoveRandom = RandomUlong();
        }

        /// <summary>
        /// Generate a random 64-bit number
        /// </summary>
        private static ulong RandomUlong() =>
            ((ulong)rng.Next() << 32) | (uint)rng.Next();

        /// <summary>
        /// Computes the Zobrist hash for the given board.
        /// </summary>
        public static ulong ComputeHash(Board board)
        {
            ulong hash = 0UL;

            // Iterate over all piece bitboards
            for (int p = 0; p < 12; p++)
            {
                ulong bb = board.PiecesBitboards[p];
                while (bb != 0UL)
                {
                    int sq = BitOperations.TrailingZeroCount(bb); // index of LSB
                    hash ^= PieceSquareRandoms[p, sq];
                    bb &= bb - 1; // pop LSB
                }
            }

            // Castling rights: mask to 4 bits
            hash ^= CastlingRandoms[board.CasltingRight & 0b1111];

            // En passant: XOR random number for file if there is an en passant square
            int epSquare = board.EnPassantSquare;
            if (epSquare >= 0)
            {
                hash ^= EnPassantRandoms[epSquare & 7]; // get file 0–7
            }

            // Side to move: XOR if black to move
            if (board.ColorToMove == Piece.Black)
                hash ^= SideToMoveRandom;

            return hash;
        }
    }
}
