namespace Michael.src.Search
{
    public class MoveOrderer
    {
        public void OrderMoves(ref Move[] legalMoves, Move LookAtThisMoveFirst)
        {
            Board board = MatchManager.board;

            Array.Sort(legalMoves, (move1, move2) =>
            {
                int score1 = 0;
                int score2 = 0;

                if (!LookAtThisMoveFirst.IsNull())
                {
                    if (move1.Equals(LookAtThisMoveFirst)) score1 += 10000;

                    else if (move2.Equals(LookAtThisMoveFirst)) score2 += 10000;
                }

                board.MakeMove(move1);
                if (GameState.IsCapture(board.CurrentGameState))
                {
                    int victimType = Piece.PieceType(GameState.CapturedPiece(board.CurrentGameState));
                    int aggressorType = Piece.PieceType(GameState.MovingPiece(board.CurrentGameState));
                    score1 += (victimType * 10) - aggressorType;
                }
                board.UndoMove(move1);
                board.MakeMove(move2);
                if (GameState.IsCapture(board.CurrentGameState))
                {
                    int victimType = Piece.PieceType(GameState.CapturedPiece(board.CurrentGameState));
                    int aggressorType = Piece.PieceType(GameState.MovingPiece(board.CurrentGameState));
                    score2 += (victimType * 10) - aggressorType;
                }
                board.UndoMove(move2);

                if (move1.IsPromotion()) score1 += 100;
                if (move2.IsPromotion()) score2 += 100;

                return score2.CompareTo(score1); // descending order
            });
        }
    }
}