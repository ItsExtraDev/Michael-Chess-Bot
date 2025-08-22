using System;

namespace Michael.src.Helpers
{
    public static class Zobrist
    {
        private static readonly Random rng = new Random(1337); // deterministic for reproducibility

        // Random numbers for each piece on each square [pieceIndex, square]
        private static readonly ulong[,] PieceSquareRandoms = new ulong[12, 64];

        // Random numbers for castling rights [0-15]
        private static readonly ulong[] CastlingRandoms = new ulong[16];

        // Random numbers for en passant file [0-7]
        private static readonly ulong[] EnPassantRandoms = new ulong[8];

        // Random number for side to move
        private static readonly ulong SideToMoveRandom;

        static Zobrist()
        {
            // Fill piece-square randoms
            for (int piece = 0; piece < 12; piece++)
            {
                for (int square = 0; square < 64; square++)
                {
                    PieceSquareRandoms[piece, square] = RandomUlong();
                }
            }

            // Fill castling rights randoms
            for (int i = 0; i < 16; i++)
                CastlingRandoms[i] = RandomUlong();

            // Fill en passant randoms
            for (int i = 0; i < 8; i++)
                EnPassantRandoms[i] = RandomUlong();

            // Side to move
            SideToMoveRandom = RandomUlong();
        }

        private static ulong RandomUlong()
        {
            byte[] buf = new byte[8];
            rng.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        /// <summary>
        /// Computes the Zobrist hash of the current board.
        /// </summary>
        public static ulong ComputeHash(Board board)
        {
            ulong hash = 0;

            // Piece positions
            for (int square = 0; square < 64; square++)
            {
                int piece = board.Squares[square];
                if (piece != Piece.None)
                {
                    int pieceIndex = BitboardHelper.GetBitboardIndex(Piece.PieceType(piece), Piece.Color(piece));
                    hash ^= PieceSquareRandoms[pieceIndex, square];
                }
            }

            // Castling rights
            hash ^= CastlingRandoms[board.CasltingRight & 0b1111];

            // En passant
            if (board.EnPassantSquare != 0)
            {
                int file = board.EnPassantSquare % 8;
                hash ^= EnPassantRandoms[file];
            }

            // Side to move
            if (board.ColorToMove == Piece.White)
                hash ^= SideToMoveRandom;

            return hash;
        }
    }
}
