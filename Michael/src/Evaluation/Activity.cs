using Michael.src;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

public class Activity
{
    #region Square Tables
    // Middlegame tables
    private static readonly int[] earlyPawnTable = {
        0,0,0,0,0,0,0,0,
        50,50,50,50,50,50,50,50,
        10,10,20,30,30,20,10,10,
        5,5,10,25,25,10,5,5,
        0,0,0,20,20,0,0,0,
        5,-5,-10,0,0,-10,-5,5,
        5,10,10,-20,-20,10,10,5,
        0,0,0,0,0,0,0,0
    };

    private static readonly int[] latePawnTable = {
        0,0,0,0,0,0,0,0,
        50,50,50,50,50,50,50,50,
        30,30,30,30,30,30,30,30,
        20,20,20,20,20,20,20,20,
        10,10,10,10,10,10,10,10,
        5,5,5,5,5,5,5,5,
        5,5,5,5,5,5,5,5,
        0,0,0,0,0,0,0,0
    };

    private static readonly int[] knightTable = {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,0,0,0,0,-20,-40,
        -30,0,10,15,15,10,0,-30,
        -30,5,15,20,20,15,5,-30,
        -30,0,15,20,20,15,0,-30,
        -30,5,10,15,15,10,5,-30,
        -40,-20,0,5,5,0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50
    };

    private static readonly int[] bishopTable = {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,0,0,0,0,0,0,-10,
        -10,0,5,10,10,5,0,-10,
        -10,5,5,10,10,5,5,-10,
        -10,0,10,10,10,10,0,-10,
        -10,10,10,10,10,10,10,-10,
        -10,5,0,0,0,0,5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20
    };

    private static readonly int[] rookTable = {
        0,0,0,0,0,0,0,0,
        5,10,10,10,10,10,10,5,
        -5,0,0,0,0,0,0,-5,
        -5,0,0,0,0,0,0,-5,
        -5,0,0,0,0,0,0,-5,
        -5,0,0,0,0,0,0,-5,
        -5,0,0,0,0,0,0,-5,
        0,0,0,5,5,0,0,0
    };

    private static readonly int[] queenTable = {
        -20,-10,-10,-5,-5,-10,-10,-20,
        -10,0,0,0,0,0,0,-10,
        -10,0,5,5,5,5,0,-10,
        -5,0,5,5,5,5,0,-5,
        0,0,5,5,5,5,0,-5,
        -10,5,5,5,5,5,0,-10,
        -10,0,5,0,0,0,0,-10,
        -20,-10,-10,-5,-5,-10,-10,-20
    };

    private static readonly int[] earlyKingTable = {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
        20,20,0,0,0,0,20,20,
        20,30,10,0,0,10,30,20
    };

    private static readonly int[] lateKingTable = {
        -50,-40,-30,-20,-20,-30,-40,-50,
        -30,-20,-10,0,0,-10,-20,-30,
        -30,-10,20,30,30,20,-10,-30,
        -30,-10,30,40,40,30,-10,-30,
        -30,-10,30,40,40,30,-10,-30,
        -30,-10,20,30,30,20,-10,-30,
        -30,-30,0,0,0,0,-30,-30,
        -50,-30,-30,-30,-30,-30,-30,-50
    };

    private static readonly int[][] MgPieceSquareTables = {
        earlyPawnTable,
        knightTable,
        bishopTable,
        rookTable,
        queenTable,
        earlyKingTable
    };

    private static readonly int[][] EgPieceSquareTables = {
        latePawnTable,
        knightTable,
        bishopTable,
        rookTable,
        queenTable,
        lateKingTable
    };
    #endregion

    private static readonly int[] PhaseValues = { 0, 1, 1, 2, 4, 0 };
    private static readonly int[] Mirror = Enumerable.Range(0, 64)
        .Select(sq => 63 - sq).ToArray();

    public int EvaluatePieceSquares(Board b)
    {
        int phase = CalculatePhase(b);
        int w = 24 - phase;
        int e = phase;
        int eval = 0;

        for (int i = 0; i < 12; i++)
        {
            ulong bitboard = b.PiecesBitboards[i];
            int pieceType = i % 6;
            bool isWhite = i < 6;

            while (bitboard != 0)
            {
                int square = BitOperations.TrailingZeroCount(bitboard);
                int idx = isWhite ? Mirror[square] : square;

                int mgScore = MgPieceSquareTables[pieceType][idx];
                int egScore = EgPieceSquareTables[pieceType][idx];
                int blended = (mgScore * w + egScore * e) / 24;

                bitboard &= bitboard - 1;
                eval += isWhite ? blended : -blended;
            }
        }

        return eval;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CalculatePhase(Board b)
    {
        int phase = 24;
        for (int i = 0; i < 12; i++)
        {
            ulong bb = b.PiecesBitboards[i];
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