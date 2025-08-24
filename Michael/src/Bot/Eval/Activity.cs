using System.Numerics;

namespace Michael.src.Bot.Eval
{
    public static class Activity
    {
        #region Square Tables

        // Middlegame tables
        private static readonly int[] earlyPawnTable = {
             0,  0,  0,  0,  0,  0,  0,  0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
             5,  5, 10, 25, 25, 10,  5,  5,
             0,  0,  0, 20, 20,  0,  0,  0,
             5, -5,-10,  0,  0,-10, -5,  5,
             5, 10, 10,-20,-20, 10, 10,  5,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        private static readonly int[] latePawnTable = {
             0,  0,  0,  0,  0,  0,  0,  0,
            50, 50, 50, 50, 50, 50, 50, 50,
            30, 30, 30, 30, 30, 30, 30, 30,
            20, 20, 20, 20, 20, 20, 20, 20,
            10, 10, 10, 10, 10, 10, 10, 10,
             5,  5,  5,  5,  5,  5,  5,  5,
             5,  5,  5,  5,  5,  5,  5,  5,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        private static readonly int[] knightTable = {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50
        };

        private static readonly int[] bishopTable = {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20
        };

        private static readonly int[] rookTable = {
             0,  0,  0,  0,  0,  0,  0,  0,
             5, 10, 10, 10, 10, 10, 10,  5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
             0,  0,  0,  5,  5,  0,  0,  0
        };

        private static readonly int[] queenTable = {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20
        };

        private static readonly int[] earlyKingTable = {
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -10,-20,-20,-20,-20,-20,-20,-10,
             20, 20,  0,  0,  0,  0, 20, 20,
             20, 30, 10,  0,  0, 10, 30, 20
        };

        private static readonly int[] lateKingTable = {
            -50,-40,-30,-20,-20,-30,-40,-50,
            -30,-20,-10,  0,  0,-10,-20,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-30,  0,  0,  0,  0,-30,-30,
            -50,-30,-30,-30,-30,-30,-30,-50
        };

        private static readonly int[][] MgPieceSquareTables = new int[][] {
            earlyPawnTable, knightTable, bishopTable, rookTable, queenTable, earlyKingTable
        };
        private static readonly int[][] EgPieceSquareTables = new int[][] {
            latePawnTable, knightTable, bishopTable, rookTable, queenTable, lateKingTable,
        };

        #endregion

        private static Board board;

        // Give a bonus or penalty for each piece position based on piece-square tables
        public static int EvaluatePieceSquares(Board b)
        {
            board = b;
            int eval = 0;
            int phase = CalculatePhase();

            for (int i = 0; i < 12; i++)
            {
                ulong bitboard = board.PiecesBitboards[i];
                int pieceType = i % 6;
                bool isWhite = i < 6;

                while (bitboard != 0)
                {
                    int square = BitOperations.TrailingZeroCount(bitboard);
                    int mgScore = MgPieceSquareTables[pieceType][!isWhite ? square : 63 - square];
                    int egScore = EgPieceSquareTables[pieceType][!isWhite ? square : 63 - square];

                    int blended = (mgScore * phase + egScore * (24 - phase)) / 24;
                    eval += isWhite ? blended : -blended;

                    bitboard &= bitboard - 1;
                }
            }
            return eval;
        }

        private static readonly int[] PhaseValues = { 0, 1, 1, 2, 4, 0 }; // pawn, knight, bishop, rook, queen, king
        private static int CalculatePhase()
        {
            int phase = 24;
            for (int i = 0; i < 12; i++)
            {
                ulong bb = board.PiecesBitboards[i];
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
