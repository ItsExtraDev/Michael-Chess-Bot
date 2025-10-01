using Michael.src.Helpers;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Michael.src.Evaluation
{
    public class Evaluator
    {
        // <- Variable Definitions -> //
        private Board board;

        // References
        readonly Activity activity;
        readonly PawnStructure pawnStructure;
        readonly KingSafety kingSafety;

        // Piece values (no king here)
        static readonly int[] PieceValues =
        {
            100, //Pawn
            320, //Knight
            350, //Bishop
            500, //Rook
            900, //Queen
        };

        private static readonly int[] PhaseValues = { 0, 1, 1, 2, 4, 0 };


        public Evaluator()
        {
            board = MatchManager.board;
            activity = new Activity();
            pawnStructure = new PawnStructure();
            kingSafety = new KingSafety();
        }

        public int Evaluate()
        {
            board = MatchManager.board;

            int phase = CalculatePhase(board);
            int w = 24 - phase;
            int e = phase;

            int mgScore = 0;
            int egScore = 0;

            // material
            mgScore += CountMaterial(true);
            mgScore -= CountMaterial(false);
            egScore += CountMaterial(true);
            egScore -= CountMaterial(false);
            // activity
            mgScore += activity.EvaluatePieceSquaresMG(board);
            egScore += activity.EvaluatePieceSquaresEG(board);
            // pawn structure
            mgScore += pawnStructure.EvaluatePawnStructure(true);
            mgScore -= pawnStructure.EvaluatePawnStructure(false);
            egScore += pawnStructure.EvaluatePawnStructure(true);
            egScore -= pawnStructure.EvaluatePawnStructure(false);

            // outposts
            mgScore += pawnStructure.EvaluateOutposts(true);
            mgScore -= pawnStructure.EvaluateOutposts(false);
            egScore += pawnStructure.EvaluateOutposts(true);
            egScore -= pawnStructure.EvaluateOutposts(false);
            // king safety
            mgScore += kingSafety.EvaluateKingSafety(true);
            mgScore -= kingSafety.EvaluateKingSafety(false);

            int blended = (mgScore * w + egScore * e) / 24;

            int colorBias = board.IsWhiteToMove ? 1 : -1;
            return blended * colorBias;
        }

        public int CountMaterial(bool isWhite)
        {
            int material = 0;

            for (int i = 0; i < 5; i++)
            {
                ulong pieceBitboard = board.PiecesBitboards[isWhite ? i : 6 + i];
                int numPieces = BitOperations.PopCount(pieceBitboard);
                int pieceValue = PieceValues[i];
                material += numPieces * pieceValue;
            }

            return material;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CalculatePhase(Board b)
        {
            int phase = 24;
            for (int i = 0; i < 12; i++)
            {
                ulong bb = b.PiecesBitboards[i];
                int pieceType = i % 6;
                while (bb != 0)
                {
                    phase -= PhaseValues[pieceType];
                    bb &= bb - 1;
                }
            }
            return Math.Clamp(phase, 0, 24);
        }
    }
}
