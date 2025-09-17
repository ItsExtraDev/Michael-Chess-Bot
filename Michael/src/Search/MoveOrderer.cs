using Michael.src.Helpers;

namespace Michael.src.Search
{
    public class MoveOrderer
    {
        // Killer moves [depth, id]
        private readonly Move[,] KillerMoves = new Move[255, 2];

        // History move [Piece, square]
        private readonly int[,] HistoryMove = new int[12, 64];

        private Board board;

        public MoveOrderer()
        {
            // initialise killers to NullMove if Move is a struct
            for (int i = 0; i < KillerMoves.GetLength(0); i++)
            {
                KillerMoves[i, 0] = Move.NullMove;
                KillerMoves[i, 1] = Move.NullMove;
            }
        }

        /// <summary>
        /// Orders moves in place. Pass in the current board and plyFromRoot.
        /// </summary>
        public void OrderMoves(ref Move[] legalMoves, Move pvMove, Board b, int plyFromRoot = 0)
        {
            board = b;
            Array.Sort(legalMoves, (m1, m2) =>
            {
                int s1 = ScoreMove(m1, pvMove, plyFromRoot);
                int s2 = ScoreMove(m2, pvMove, plyFromRoot);
                return s2.CompareTo(s1); // descending
            });
        }

        private int ScoreMove(Move move, Move pvMove, int plyFromRoot)
        {
            int score = 0;

            // principal variation move first
            if (!pvMove.IsNull() && move.Equals(pvMove))
                score += 10000;

            // MVV/LVA style capture scoring – relies on Move holding victim/attacker info

            board.MakeMove(move);
            if (GameState.IsCapture(board.CurrentGameState))
            {
                int victim = Piece.PieceType(GameState.CapturedPiece(board.CurrentGameState));
                int attacker = Piece.PieceType(GameState.MovingPiece(board.CurrentGameState));
                score += (victim * 100) - attacker;
            }

            // Promotion scoring
            if (move.IsPromotion())
            {
                switch (move.MoveFlag)
                {
                    case Piece.Queen: score += 900; break;
                    case Piece.Rook: score += 500; break;
                    case Piece.Bishop: score += 325; break;
                    case Piece.Knight: score += 300; break;
                }
            }

            // Killer moves only for quiet moves
            if (!GameState.IsCapture(board.CurrentGameState))
            {
                if (!KillerMoves[plyFromRoot, 0].IsNull() && KillerMoves[plyFromRoot, 0].Equals(move))
                    score += 100;
                else if (!KillerMoves[plyFromRoot, 1].IsNull() && KillerMoves[plyFromRoot, 1].Equals(move))
                    score += 80;

                // later: history heuristic
                //score += HistoryMove[movingPiece, move.TargetSquare];
            }
            board.UndoMove(move);

            return score;
        }

        public void AddKillerMove(Move move, int plyFromRoot)
        {
            if (plyFromRoot < 0 || plyFromRoot >= KillerMoves.GetLength(0)) return;

            if (KillerMoves[plyFromRoot, 0].Equals(move)) return; // already present
            KillerMoves[plyFromRoot, 1] = KillerMoves[plyFromRoot, 0];
            KillerMoves[plyFromRoot, 0] = move;
        }
    }
}
