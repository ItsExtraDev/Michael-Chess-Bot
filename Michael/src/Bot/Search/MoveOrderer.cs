using Michael.src.Helpers;
using Michael.src.MoveGen;

namespace Michael.src.Bot.Search
{
    public static class MoveOrderer
    {
        public static void OrderMoves(Board board, Move LookAtThismoveFirst, ref Move[] moves)
        {
            // Simple move ordering: prioritize captures and checks
            Array.Sort(moves, (move1, move2) =>
            {
                int score1 = 0;
                int score2 = 0;

                //Best move from the first search
                if (move1.StartingSquare == LookAtThismoveFirst.StartingSquare &&
                    move1.TargetSquare == LookAtThismoveFirst.TargetSquare &&
                    move1.MoveFlag == LookAtThismoveFirst.MoveFlag)
                {
                    Console.WriteLine("Looking at this move first: " + Notation.MoveToAlgebraic(move1));
                    score1 += 1000;
                }
                if (move2.StartingSquare == LookAtThismoveFirst.StartingSquare &&
                    move2.TargetSquare == LookAtThismoveFirst.TargetSquare &&
                    move2.MoveFlag == LookAtThismoveFirst.MoveFlag)
                {
                    Console.WriteLine("Looking at this move first: " + Notation.MoveToAlgebraic(move2));
                    score2 += 1000;
                }

                // Prioritize captures
                if (!BitboardHelper.IsBitSet(board.ColoredBitboards[2], move1.TargetSquare))
                    score1 += 10 * (Piece.PieceType(board.Squares[move1.TargetSquare]) - Piece.PieceType(board.Squares[move1.StartingSquare]));
                if (!BitboardHelper.IsBitSet(board.ColoredBitboards[2], move2.TargetSquare))
                    score2 += 10 * (Piece.PieceType(board.Squares[move2.TargetSquare]) - Piece.PieceType(board.Squares[move2.StartingSquare]));

                if (move1.IsPromotion())
                    score1 += 900;
                if (move2.IsPromotion())
                    score2 += 900;
                return score1.CompareTo(score2); // Higher score first

            });
        }
    }
}