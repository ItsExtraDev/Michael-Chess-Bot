using System.Numerics;

namespace Michael.src.Evaluation
{
    public class Evaluator
    {
        //Variable declarations
        private Board board;
        private int materialCount;
        private int evaluation;
        private int colorBias;
        private ulong Bitboard;
        private int pieceValue;
        private int pieceCount;

        private int[] piecesValues =
        {
            100, //Pawn
            320, //Knight
            350, //Bishop
            500, //Rook
            900 //Queen
        };

        //Refrances
        readonly Activity activity;

        public Evaluator()
        {
            board = MatchManager.board;
            activity = new Activity();
        }

        public int Evaluate()
        {
            board = MatchManager.board;

            evaluation = 0;

            evaluation += CountMaterial(Piece.White);
            evaluation -= CountMaterial(Piece.Black);

            evaluation += activity.EvaluatePieceSquares();

            colorBias = board.IsWhiteToMove ? 1 : -1;
            return evaluation * colorBias;
        }

        private int CountMaterial(int color)
        {
            int materialCount = 0;

            for (int i = 0; i < 6; i++)
            {
                //Skip kings
                if (i == 5)
                    continue;

                Bitboard = board.PiecesBitboards[(6 * color) + i];

                pieceCount = BitOperations.PopCount(Bitboard);
                pieceValue = piecesValues[i];

                materialCount += pieceValue * pieceCount;// * (color == 0 ? 1 : -1);
            }
            return materialCount;
        }
    }
}