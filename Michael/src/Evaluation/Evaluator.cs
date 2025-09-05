using System.Numerics;

namespace Michael.src.Evaluation
{
    public class Evaluator
    {

        //Variables
        private Board board;

        readonly int[] PiecesValues =
        {
            100, //Pawn
            320, //Knight
            350, //Bishop
            500, //Rook
            900 //Queen
        };

        public int Evaluate()
        {
            board = MatchManager.board;

            int evaluation = 0;

            evaluation += CountMaterial();

            int colorBias = board.IsWhiteToMove ? 1 : -1;

            return evaluation * colorBias;
        }

        private int CountMaterial()
        {
            int material = 0;

            ulong Bitboard;
            int pieceCount;
            int pieceValue;
            int colorBias;

            for (int i = 0; i < 11; i++)
            {
                //Skip kings
                if (i % 6 == 5)
                    continue;

                Bitboard = board.PiecesBitboards[i];
                pieceCount = BitOperations.PopCount(Bitboard);
                pieceValue = PiecesValues[i % 6];
                colorBias = i < 6 ? 1 : -1;

                material += pieceCount * pieceValue * colorBias;
            }

            return material;
        }
    }
}