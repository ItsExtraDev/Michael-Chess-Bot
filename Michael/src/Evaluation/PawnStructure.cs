using Michael.src.Helpers;
using Michael.src.MoveGen;
using System.Numerics;

namespace Michael.src.Evaluation
{
    public class PawnStructure
    {
        private Board board;

        //Bonuses
        private const int PassedPawnBonus = 10;
        private const int IsolatedPawnPanilty = -15;
        private const int DoubledPawnPanilty = -10;
        private const int KnightOutpostBonus = 35;
        private const int BishopOutpostBonus = 25;

        public int EvaluatePawnStructure(bool isWhite)
        {
            board = MatchManager.board;

            int score = 0;

            ulong friendlyPawns = isWhite ? board.PiecesBitboards[0] : board.PiecesBitboards[6];
            ulong friendlyPawnsCopy = friendlyPawns;
            ulong enemyPawns = isWhite ? board.PiecesBitboards[6] : board.PiecesBitboards[0];

            while (friendlyPawns != 0)
            {
                int pawnSquare = BitboardHelper.PopLSB(ref friendlyPawns);
                int file = BoardHelper.File(pawnSquare);
                int rank = BoardHelper.Rank(pawnSquare);

                //Passed Pawns
                if ((BitboardHelper.PassedPawnMasks[pawnSquare, isWhite ? 0 : 1] & enemyPawns) == 0)
                {
                    if (!isWhite)
                        rank = 7 - rank; //Invert rank for black pawns
                    score += PassedPawnBonus * rank;
                }

                //Isolated pawns
                if ((BitboardHelper.AdjacentFileMasks[BoardHelper.File(pawnSquare)] & friendlyPawnsCopy) == 0)
                {
                   score += IsolatedPawnPanilty;
                }

            }

            
            for (int f = 0; f < 8; f++)
            {
                int count = BitOperations.PopCount(BitboardHelper.FileMasks[f] & friendlyPawnsCopy);
                if (count > 1)
                    score += DoubledPawnPanilty * (count - 1);
            }

            return score;
        }

        public int EvaluateOutposts(bool isWhite)
        {
            ulong knights = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Knight, isWhite)];
            ulong bishops = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Bishop, isWhite)];
            ulong piecesToCheck = knights | bishops;

            int eval = 0;

            while (piecesToCheck != 0)
            {
                int square = BitboardHelper.PopLSB(ref piecesToCheck);
                //Detect if a piece is defended by a pawn
                ulong defendedSquares = (isWhite ? MoveGenerator.BlackPawnAttacks : MoveGenerator.WhitePawnAttacks)[square];

                if ((defendedSquares & board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Pawn, isWhite)]) == 0)
                {
                    continue;
                }

                if ((BitboardHelper.GetPassedPawnMask(square, (isWhite ? Piece.White : Piece.Black)) & (isWhite ? board.PiecesBitboards[6] : board.PiecesBitboards[0])) != 0)
                {
                    continue;
                }

                if ((knights & 1ul << square) != 0)
                {
                    eval += KnightOutpostBonus;
                }
                else
                {
                    eval += BishopOutpostBonus;
                }
            }

            return eval;
        }
    }
}