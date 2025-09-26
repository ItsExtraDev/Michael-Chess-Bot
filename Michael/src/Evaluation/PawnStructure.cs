using Michael.src.Helpers;

namespace Michael.src.Evaluation
{
    public class PawnStructure
    {
        private Board board;

        //Bonuses
        private const int PassedPawnBonus = 10;

        public int EvaluatePawnStructure(bool isWhite)
        {
            board = MatchManager.board;

            int score = 0;

            ulong friendlyPawns = isWhite ? board.PiecesBitboards[0] : board.PiecesBitboards[6];
            ulong enemyPawns = isWhite ? board.PiecesBitboards[6] : board.PiecesBitboards[0];

            while (friendlyPawns != 0)
            {
                int pawnSquare = BitboardHelper.PopLSB(ref friendlyPawns);
                int rank = BoardHelper.Rank(pawnSquare);

                //Passed Pawns
                if ((BitboardHelper.PassedPawnMasks[pawnSquare, isWhite ? 0 : 1] & enemyPawns) == 0)
                {
                    if (!isWhite)
                        rank = 7 - rank; //Invert rank for black pawns
                    score += PassedPawnBonus * rank;
                }
            }

            return score;
        }
    }
}