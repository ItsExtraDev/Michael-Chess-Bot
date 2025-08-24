using Michael.src.Helpers;
using Michael.src.MoveGen;

namespace Michael.src.Bot.Search
{
    public static class MoveOrderer
    {
        public static void OrderMoves(Board board, ref Move[] moves, Move? pvMove = null)
        {
            Array.Sort(moves, (move1, move2) =>
            {
                int score1 = GetMoveScore(board, move1, pvMove);
                int score2 = GetMoveScore(board, move2, pvMove);
                return score2.CompareTo(score1); // Higher score first
            });
        }

        private static int GetMoveScore(Board board, Move move, Move? pvMove)
        {
            int score = 0;

            // Principal Variation move gets the highest priority
            if (pvMove != null && move.StartingSquare == pvMove.StartingSquare && move.TargetSquare == pvMove.TargetSquare && move.MoveFlag == pvMove.MoveFlag)
                score += 1000000;

            // MVV-LVA (capture ordering)
            if (!BitboardHelper.IsBitSet(board.ColoredBitboards[2], move.TargetSquare))
            {
                int capturedValue = Piece.PieceType(board.Squares[move.TargetSquare]);
                int attackerValue = Piece.PieceType(board.Squares[move.StartingSquare]);
                score += 10 * (capturedValue - attackerValue);
            }

            // Promotion bonus
            if (move.IsPromotion())
                score += 900;

            return score;
        }
    }
}
