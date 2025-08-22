using System.Numerics;

namespace Michael.src.Bot.Eval
{
    public static class Evaluator
    {
        private static readonly int[] PieceValues = new int[]
        {
            100,  // Pawn
            320,  // Knight
            350,  // Bishop
            500,  // Rook
            900,  // Queen
        };

        private static Board board;

        public static int Evaluate(Board boardInstance)
        {
            board = boardInstance;
            int eval = 0;

            eval += CountMaterial();

            int colorBias = board.ColorToMove == Piece.White ? 1 : -1;
            return eval * colorBias;
        }

        public static int CountMaterial()
        {
            int score = 0;
            for (int i = 0; i < 12; i++)
            {
                ulong Bitboard = board.PiecesBitboards[i];

                if (i % 6 == 5) // Skip kings
                    continue;
                int numPieces = BitOperations.PopCount(Bitboard);
                int pieceValues = PieceValues[i % 6];

                score += (i < 6 ? 1 : -1) * numPieces * pieceValues;
            }
            return score;
        }
    }
}
