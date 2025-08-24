using System.Numerics;

namespace Michael.src.Bot.Eval
{
    public static class Evaluator
    {
        private static Board board;

        private static int[] PieceValues =
        {
            100, //Pawn
            330, //Knight
            350, //Bishop
            500, //Rook
            900, //Queen
        };

        public static int Evaluate(Board boardInstance)
        {
            board = boardInstance;

            if (board.IsDraw())
                return 0;

            if (board.IsCheckmate())
                return -100000 + board.plyCount;

            int eval = 0;

            eval += CountMaterial();
            eval += Activity.EvaluatePieceSquares(board);

            int colorBias = board.ColorToMove == Piece.White ? 1 : -1;

            return eval * colorBias;
        }


        public static int CountMaterial()
        {
            int material = 0;

            for (int i = 0; i < 11; i++)
            {
                if (i % 6 == 5) // Skip kings
                    continue;

                ulong bitboard = board.PiecesBitboards[i];
                int numPieces = BitOperations.PopCount(bitboard);
                int pieceValue = PieceValues[i % 6];

                int colorFactor = (i < 6) ? 1 : -1;

                material += numPieces * pieceValue * colorFactor;
            }
            return material;
        }
    }
}