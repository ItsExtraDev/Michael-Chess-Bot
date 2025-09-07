using System.Numerics;

namespace Michael.src.Evaluation
{
    public class Evaluator
    {

        //Variables
        private Board board;
        private Activity activity;

        public Evaluator()
        {
            board = new Board();
            activity = new Activity();
        }

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

            if (board.IsDraw())
                return 0;

            if (board.IsCheckmate())
                return -1000000 + board.plyCount;

            int evaluation = 0;

            evaluation += CountMaterial();
            evaluation += activity.EvaluatePieceSquares();

            int colorBias = board.IsWhiteToMove ? 1 : -1;

            return evaluation * colorBias;
        }

        private int CountMaterial(int colorBias = 0)
        {
            int material = 0;

            ulong Bitboard;
            int pieceCount;
            int pieceValue;

            for (int i = 0; i < 11; i++)
            {
                //Skip kings
                if (i % 6 == 5)
                    continue;

                Bitboard = board.PiecesBitboards[i];
                pieceCount = BitOperations.PopCount(Bitboard);
                pieceValue = PiecesValues[i % 6];
                if (colorBias == 0)
                    colorBias = i < 6 ? 1 : -1;

                material += pieceCount * pieceValue * colorBias;
            }

            return material;
        }

        public int GetNonPawnMaterial()
        {
            int totalMaterial = CountMaterial(1);

            int pawnMaterial = 0;

            for (int color = 0; color < 2; color++)
            {
                int numPawns = BitOperations.PopCount(board.PiecesBitboards[color * 6]);

                pawnMaterial += numPawns * 100;
            }

            return totalMaterial - pawnMaterial;
        }

    }
}