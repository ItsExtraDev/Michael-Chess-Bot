using System.Numerics;

namespace Michael.src.Evaluation
{
    public class Evaluator
    {

        // <- Variable Definetions -> //
        private Board board;

        // Refrances
        readonly Activity activity;
        readonly PawnStructure pawnStructure;

        public Evaluator()
        {
            board = MatchManager.board;
            activity = new Activity();
            pawnStructure = new PawnStructure();
        }

        static readonly int[] PieceValues =
        {
            100, //Pawn
            320, //Knight
            350, //Bishop
            500, //Rook
            900, //Queen
        };


        public int Evaluate()
        {
            board = MatchManager.board;

            int eval = 0;

            eval += CountMaterial(true);
            eval -= CountMaterial(false);

            eval += activity.EvaluatePieceSquares(board);

            eval += pawnStructure.EvaluatePawnStructure(true);
            eval -= pawnStructure.EvaluatePawnStructure(false);

            int colorBias = board.IsWhiteToMove ? 1 : -1;
            return eval * colorBias;
        }

        public int CountMaterial(bool isWhite)
        {
            int material = 0;

            for (int i = 0; i < 5; i++)
            {

                ulong PieceBitboard = board.PiecesBitboards[isWhite ? i : 6+i];
                int numPieces = BitOperations.PopCount(PieceBitboard);
                int pieceValue = PieceValues[i];

                material += numPieces * pieceValue;

            }

            return material;
        }
    }
}