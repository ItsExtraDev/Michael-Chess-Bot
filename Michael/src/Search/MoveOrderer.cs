using Michael.src.Helpers;

namespace Michael.src.Search
{
    public class MoveOrderer
    {
        private Board board;
        private Move pvMove;

        private Move[,] killers = new Move[2,256];
        private int[,,] history = new int[12, 64, 64];

        private int[] piecesValues =
        {
            0, //NONE
            10, //PAWN
            32, //KNIGHT
            35, //BISHOP
            50, //ROOK
            90, //QUEEN
            0, //KING (Since he can't be captured, this value is meaningless)
        };

        public void ResetKillers()
        {
            Array.Clear(killers, 0, killers.Length);
            Array.Clear(history, 0, history.Length);
        }

        public void AddKiller(Move move, int plyFromRoot)
        {
            board = MatchManager.board;

            //Not a capture
            if (board.Squares[move.TargetSquare] == Piece.None)
            {
                killers[1, plyFromRoot] = killers[0, plyFromRoot];
                killers[0, plyFromRoot] = move;
            }
        }
        public void AddHistory(Move move, int movingPieceType, int depth)
        {
            board = MatchManager.board;

            //Not a capture
            if (board.Squares[move.TargetSquare] == Piece.None)
            {
                history[movingPieceType, move.StartingSquare, move.TargetSquare] += depth * depth;
            }
        }

        public Move[] OrderMoves(Move[] legalMoves, Move pvMove, int plyFromRoot)
        {
            board = MatchManager.board;
            this.pvMove = pvMove;

            // Build score array
            int[] scores = new int[legalMoves.Length];
            for (int i = 0; i < legalMoves.Length; i++)
                scores[i] = Score(legalMoves[i], plyFromRoot);

            // Sort both arrays by quicksort descending
            QuickSort(legalMoves, scores, 0, legalMoves.Length - 1);

            return legalMoves;
        }

        /// <summary>
        /// We aim to order the moves millions of times per second, and array.sort is kinda slow.
        /// instead, use the fastest sorting algorithm which is quicksort 
        /// (First time I actually had to use one outside of leetcode)
        /// </summary>
        private void QuickSort(Move[] moves, int[] scores, int left, int right)
        {
            if (left >= right) return;

            int pivotScore = scores[(left + right) / 2];
            int i = left, j = right;

            while (i <= j)
            {
                // For descending, reverse the comparisons:
                while (scores[i] > pivotScore) i++;
                while (scores[j] < pivotScore) j--;

                if (i <= j)
                {
                    // swap scores
                    (scores[j], scores[i]) = (scores[i], scores[j]);

                    // swap moves
                    (moves[j], moves[i]) = (moves[i], moves[j]);
                    i++;
                    j--;
                }
            }

            if (left < j) QuickSort(moves, scores, left, j);
            if (i < right) QuickSort(moves, scores, i, right);
        }

        private int Score(Move move, int plyFromRoot)
        {
            if (move.Equals(pvMove))
                return 10_000;

            if (move.Equals(killers[0,plyFromRoot]))
                return 750;
            if (move.Equals(killers[1, plyFromRoot]))
                return 500;

            int pieceType = Piece.PieceType(board.Squares[move.StartingSquare]);

            int score = history[pieceType, move.StartingSquare, move.TargetSquare];

            //If the move is a capture, rank it by material diff.
            if (board.Squares[move.TargetSquare] != Piece.None)
            {
                int attackingPieceValue = piecesValues[Piece.PieceType(board.Squares[move.StartingSquare])];
                int VictimPieceValue = piecesValues[Piece.PieceType(board.Squares[move.TargetSquare])];

                score += (VictimPieceValue * 100) - attackingPieceValue;
            }

            if (score > 10_000)
            {
                score = 9999;
            }

            return score;
        }
    }
}
