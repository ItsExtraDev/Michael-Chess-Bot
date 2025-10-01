using Michael.src.Helpers;
using System.Numerics;

namespace Michael.src.Evaluation
{
    public class KingSafety
    {
        private const int pawnInShieldBonus = 5;

        public int EvaluateKingSafety(bool isWhite)
        {
            //Console.WriteLine("A");
            Board board = MatchManager.board;

            int score = 0;

            //Console.WriteLine("B");
            ulong friendlyPawns = board.PiecesBitboards[isWhite ? 0 : 6];
            int kingSquare = BitOperations.TrailingZeroCount(board.PiecesBitboards[isWhite ? 5 : 11]);
            if (kingSquare > 63 || kingSquare < 0)
                return 0; //No king found, should not happen
            //Get pawn shield mask
           // ulong pawnShieldMask = getPawnShieldMask(kingSquare, isWhite);
            //Count friendly pawns in the shield
           // int friendlyPawnsInShield = BitOperations.PopCount(pawnShieldMask & friendlyPawns);
            //Calculate score
            //score += friendlyPawnsInShield * pawnInShieldBonus; //Bonus for each friendly pawn in the shield
            //Console.WriteLine("C");
            ulong passedPawn = BitboardHelper.GetPassedPawnMask(kingSquare, isWhite ? Piece.White : Piece.Black) & friendlyPawns;

           // Console.WriteLine("D");
            while (passedPawn != 0)
            {
                int passedPawnSquare = BitboardHelper.PopLSB(ref passedPawn);
                score += (isWhite ? 7 - BoardHelper.Rank(passedPawnSquare) : BoardHelper.Rank(passedPawnSquare)) * pawnInShieldBonus;
            }
            //Console.WriteLine("E");
            return score;
        }

        private ulong getPawnShieldMask(int square, bool isWhite)
        {
            int file = BoardHelper.File(square);
            ulong mask = 1ul << square;


            if (file > 0)
            {
                mask |= 1ul << (square - 1);
            }
            if (file < 7)
            {
                mask |= 1ul << (square + 1);
            }

            int forwardShift = isWhite ? 8 : -8;

            return BitboardHelper.ShiftBitboard(mask, forwardShift);
        }
    }
}