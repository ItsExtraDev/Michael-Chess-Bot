using Michael.src.Helpers;
using System.Numerics;

namespace Michael.src.Evaluation
{
    public class KingSafety
    {
        private const int pawnInShieldBonus = 5;

        public int EvaluateKingSafety(bool isWhite)
        {
            Board board = MatchManager.board;

            int score = 0;

            ulong friendlyPawns = board.PiecesBitboards[isWhite ? 0 : 6];
            int kingSquare = BitOperations.TrailingZeroCount(board.PiecesBitboards[isWhite ? 5 : 11]);
            if (kingSquare > 63 || kingSquare < 0)
                return 0; //No king found, should not happen

            ulong passedPawn = BitboardHelper.GetPassedPawnMask(kingSquare, isWhite ? Piece.White : Piece.Black) & friendlyPawns;

            while (passedPawn != 0)
            {
                int passedPawnSquare = BitboardHelper.PopLSB(ref passedPawn);
                score += (isWhite ? 7 - BoardHelper.Rank(passedPawnSquare) : BoardHelper.Rank(passedPawnSquare)) * pawnInShieldBonus;
            }
            return score;
        }
    }
}