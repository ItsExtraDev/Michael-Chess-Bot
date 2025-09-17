using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Michael.src.MoveGen
{
    /// <summary>
    /// Magic bitboards work like regular bitboards, but since we can know how can the sliding piece
    /// move given any square and any combination of blockers, we can compute this data ahead of time,
    /// and save a lot of time during move gen thus improving engine speed since most of the move gen
    /// is legal move verification / sliding pieces.
    /// Magic pieces are for rooks, bishops and queens only. more info can be found at:
    /// https://www.chessprogramming.org/Magic_Bitboards
    /// </summary>
    public static class Magic
    {
        public static void Init()
        {
            PrecomputeEdges();
            PrecomputeRookAttacks();
            PrecomputeBishopAttacks();
        }


        #region Magic Numbers
        private static readonly ulong[] MagicRookNumbers = new ulong[]
{
    0x8a80104000800020, 0x140002000100040, 0x2801880a0017001, 0x100081001000420,
    0x200020010080420, 0x3001c0002010008, 0x8480008002000100, 0x2080088004402900,
    0x800098204000, 0x2024401000200040, 0x100802000801000, 0x120800800801000,
    0x208808088000400, 0x2802200800400, 0x2200800100020080, 0x801000060821100,
    0x80044006422000, 0x100808020004000, 0x12108a0010204200, 0x140848010000802,
    0x481828014002800, 0x8094004002004100, 0x4010040010010802, 0x20008806104,
    0x100400080208000, 0x2040002120081000, 0x21200680100081, 0x20100080080080,
    0x2000a00200410, 0x20080800400, 0x80088400100102, 0x80004600042881,
    0x4040008040800020, 0x440003000200801, 0x4200011004500, 0x188020010100100,
    0x14800401802800, 0x2080040080800200, 0x124080204001001, 0x200046502000484,
    0x480400080088020, 0x1000422010034000, 0x30200100110040, 0x100021010009,
    0x2002080100110004, 0x202008004008002, 0x20020004010100, 0x2048440040820001,
    0x101002200408200, 0x40802000401080, 0x4008142004410100, 0x2060820c0120200,
    0x1001004080100, 0x20c020080040080, 0x2935610830022400, 0x44440041009200,
    0x280001040802101, 0x2100190040002085, 0x80c0084100102001, 0x4024081001000421,
    0x20030a0244872, 0x12001008414402, 0x2006104900a0804, 0x1004081002402
};

        private static readonly int[] MagicRookShifts = new int[]
        {
    52, 53, 53, 53, 53, 53, 53, 52,
    53, 54, 54, 54, 54, 54, 54, 53,
    53, 54, 54, 54, 54, 54, 54, 53,
    53, 54, 54, 54, 54, 54, 54, 53,
    53, 54, 54, 54, 54, 54, 54, 53,
    53, 54, 54, 54, 54, 54, 54, 53,
    53, 54, 54, 54, 54, 54, 54, 53,
    52, 53, 53, 53, 53, 53, 53, 52
        };

        private static readonly ulong[] MagicBishopNumbers = new ulong[]
        {
    0x40040844404084, 0x2004208a004208, 0x10190041080202, 0x108060845042010,
    0x581104180800210, 0x2112080446200010, 0x1080820820060210, 0x3c0808410220200,
    0x4050404440404, 0x21001420088, 0x24d0080801082102, 0x1020a0a020400,
    0x40308200402, 0x4011002100800, 0x401484104104005, 0x801010402020200,
    0x400210c3880100, 0x404022024108200, 0x810018200204102, 0x4002801a02003,
    0x85040820080400, 0x810102c808880400, 0xe900410884800, 0x8002020480840102,
    0x220200865090201, 0x2010100a02021202, 0x152048408022401, 0x20080002081110,
    0x4001001021004000, 0x800040400a011002, 0xe4004081011002, 0x1c004001012080,
    0x8004200962a00220, 0x8422100208500202, 0x2000402200300c08, 0x8646020080080080,
    0x80020a0200100808, 0x2010004880111000, 0x623000a080011400, 0x42008c0340209202,
    0x209188240001000, 0x400408a884001800, 0x110400a6080400, 0x1840060a44020800,
    0x90080104000041, 0x201011000808101, 0x1a2208080504f080, 0x8012020600211212,
    0x500861011240000, 0x180806108200800, 0x4000020e01040044, 0x300000261044000a,
    0x802241102020002, 0x20906061210001, 0x5a84841004010310, 0x4010801011c04,
    0xa010109502200, 0x4a02012000, 0x500201010098b028, 0x8040002811040900,
    0x28000010020204, 0x6000020202d0240, 0x8918844842082200, 0x4010011029020020
        };

        private static readonly int[] MagicBishopShifts = new int[]
        {
    58, 59, 59, 59, 59, 59, 59, 58,
    59, 59, 59, 59, 59, 59, 59, 59,
    59, 59, 57, 57, 57, 57, 59, 59,
    59, 59, 57, 55, 55, 57, 59, 59,
    59, 59, 57, 55, 55, 57, 59, 59,
    59, 59, 57, 57, 57, 57, 59, 59,
    59, 59, 59, 59, 59, 59, 59, 59,
    58, 59, 59, 59, 59, 59, 59, 58
        };
        #endregion

        #region Rook

        private static readonly int[] RookOffsets = { -8, -1, 1, 8 };
        private static ulong[][] PrecomputedRookAttacks = new ulong[64][];
        public static ulong[] PrecomputedRookAttackMask = new ulong[64];
        private static bool[,] RookEdgeSquares = new bool[64, 4]; // per square per direction

        //Apperantly newer CPUs have custom instruction which does the same thing as magic bitboards, but faster.
        //If the CPU can use this (Bmi), use it, otherwise resort to regular magic number calculation

        private static int GetRookIndex(int square, ulong blockers)
        {
            ulong mask = PrecomputedRookAttackMask[square];
            // Fast path: use PEXT (parallel bit extract) if available
            if (Bmi2.X64.IsSupported)
            {
                // Extract only the bits covered by mask into low-order bits
                ulong compressed = Bmi2.X64.ParallelBitExtract(blockers, mask);
                return (int)compressed;
            }

            // Fallback: classic magic multiplication & shift
            blockers &= mask;
            return (int)((blockers * MagicRookNumbers[square]) >> MagicRookShifts[square]);
        }

        public static ulong GetRookAttacks(int square, ulong blockers)
        {
            int index = GetRookIndex(square, blockers);
            return PrecomputedRookAttacks[square][index];
        }

        //Create a mask of all the squares can attack from a given square,
        //Excludes the last square in each direction and all enemy / friendly pieces.
        public static ulong GetRookAttackMask(int square, ulong blockers = 0, bool isFromMoveGen = false)
        {
            ulong attackMask = 0;

            for (int index = 0; index < RookOffsets.Length; index++)
            {
                int offset = RookOffsets[index];
                int currentSquare = square;

                while (true)
                {
                    currentSquare += offset;

                    if (!IsSquareOnBoard(currentSquare) || !AreSquaresInRow(square, currentSquare))
                    {
                        break;
                    }
                    if (!isFromMoveGen && RookEdgeSquares[currentSquare, index] || isFromMoveGen && RookEdgeSquares[currentSquare - offset, index])
                        break;

                    attackMask |= 1ul << currentSquare;

                    if ((blockers & 1ul << currentSquare) != 0)
                        break;
                }
            }

            return attackMask;
        }

        public static ulong[] GetRookBlockerCombinations(int square)
        {
            ulong mask = GetRookAttackMask(square);
            int bitsInMask = BitOperations.PopCount(mask);
            int combinationCount = 1 << bitsInMask;

            ulong[] blockers = new ulong[combinationCount];
            List<int> setBits = GetSetBits(mask); // positions of bits that can be blockers

            for (int i = 0; i < combinationCount; i++)
            {
                ulong blocker = 0;
                for (int j = 0; j < bitsInMask; j++)
                {
                    if ((i & (1 << j)) != 0)
                        blocker |= 1UL << setBits[j];
                }
                blockers[i] = blocker;
            }

            return blockers;
        }

        private static void PrecomputeRookAttacks()
        {
            for (int square = 0; square < 64; square++)
            {
                ulong[] blockersCombinations = GetRookBlockerCombinations(square);
                PrecomputedRookAttacks[square] = new ulong[blockersCombinations.Length];

                for (int i = 0; i < blockersCombinations.Length; i++)
                {
                    ulong blocker = blockersCombinations[i];
                    int index = GetRookIndex(square, blocker);
                    PrecomputedRookAttacks[square][index] = GetRookAttackMask(square, blocker, true);
                    PrecomputedRookAttackMask[square] = GetRookAttackMask(square);
                }
            }
        }

        #endregion

        #region Bishop

        #region Bishop

        private static readonly int[] BishopOffsets = { -9, -7, 7, 9 };
        private static ulong[][] PrecomputedBishopAttacks = new ulong[64][];
        public static ulong[] PrecomputedBishopAttackMask = new ulong[64];
        private static bool[,] BishopEdgeSquares = new bool[64, 4]; // per square per diagonal

        //Apperantly newer CPUs have custom instruction which does the same thing as magic bitboards, but faster.
        //If the CPU can use this (Bmi), use it, otherwise resort to regular magic number calculation
        private static int GetBishopIndex(int square, ulong blockers)
        {
            ulong mask = PrecomputedBishopAttackMask[square];
            if (Bmi2.X64.IsSupported)
            {
                ulong compressed = Bmi2.X64.ParallelBitExtract(blockers, mask);
                return (int)compressed;
            }

            blockers &= mask;
            return (int)((blockers * MagicBishopNumbers[square]) >> MagicBishopShifts[square]);
        }

        public static ulong GetBishopAttacks(int square, ulong blockers)
        {
            int index = GetBishopIndex(square, blockers);
            return PrecomputedBishopAttacks[square][index];
        }

        public static ulong GetBishopAttackMask(int square, ulong blockers = 0, bool isFromMoveGen = false)
        {
            ulong attackMask = 0;

            for (int i = 0; i < BishopOffsets.Length; i++)
            {
                int offset = BishopOffsets[i];
                int currentSquare = square;

                while (true)
                {
                    currentSquare += offset;
                    if (!IsSquareOnBoard(currentSquare)) break;
                    if (!AreSquaresInDiagonal(square, currentSquare)) break;
                    if (isFromMoveGen && BishopEdgeSquares[currentSquare - offset, i] ||
                        (!isFromMoveGen && BishopEdgeSquares[currentSquare, i]))
                        break;

                    attackMask |= 1ul << currentSquare;

                    if ((blockers & (1ul << currentSquare)) != 0)
                        break;
                }
            }

            return attackMask;
        }

        public static ulong[] GetBishopBlockerCombinations(int square)
        {
            ulong mask = GetBishopAttackMask(square);
            int bitsInMask = BitOperations.PopCount(mask);
            int combinationCount = 1 << bitsInMask;

            ulong[] blockers = new ulong[combinationCount];
            List<int> setBits = GetSetBits(mask);

            for (int i = 0; i < combinationCount; i++)
            {
                ulong blocker = 0;
                for (int j = 0; j < bitsInMask; j++)
                {
                    if ((i & (1 << j)) != 0)
                        blocker |= 1UL << setBits[j];
                }
                blockers[i] = blocker;
            }

            return blockers;
        }

        private static void PrecomputeBishopAttacks()
        {
            for (int square = 0; square < 64; square++)
            {
                ulong[] blockersCombinations = GetBishopBlockerCombinations(square);
                PrecomputedBishopAttacks[square] = new ulong[blockersCombinations.Length];

                for (int i = 0; i < blockersCombinations.Length; i++)
                {
                    ulong blocker = blockersCombinations[i];
                    int index = GetBishopIndex(square, blocker);
                    PrecomputedBishopAttacks[square][index] = GetBishopAttackMask(square, blocker, true);
                    PrecomputedBishopAttackMask[square] = GetBishopAttackMask(square);
                }
            }
        }

        #endregion


        #endregion

        #region Helpers

        private static void PrecomputeEdges()
        {
            for (int square = 0; square < 64; square++)
            {
                int rank = square >> 3;
                int file = square & 7;

                // Rook edges
                RookEdgeSquares[square, 0] = rank == 0;  // -8
                RookEdgeSquares[square, 1] = file == 0;  // -1
                RookEdgeSquares[square, 2] = file == 7;  // +1
                RookEdgeSquares[square, 3] = rank == 7;  // +8

                // Bishop edges
                BishopEdgeSquares[square, 0] = rank == 0 || file == 0;  // -9
                BishopEdgeSquares[square, 1] = rank == 0 || file == 7;  // -7
                BishopEdgeSquares[square, 2] = rank == 7 || file == 0;  // +7
                BishopEdgeSquares[square, 3] = rank == 7 || file == 7;  // +9
            }
        }
        private static bool IsSquareOnBoard(int square)
            => (uint)square < 64;

        private static List<int> GetSetBits(ulong bb)
        {
            List<int> bits = new List<int>();
            for (int i = 0; i < 64; i++)
            {
                if (((bb >> i) & 1) != 0)
                    bits.Add(i);
            }
            return bits;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreSquaresInDiagonal(int sq1, int sq2)
        {
            int r1 = sq1 >> 3;      // rank
            int f1 = sq1 & 7;       // file
            int r2 = sq2 >> 3;
            int f2 = sq2 & 7;

            // instead of Math.Abs:
            int dr = r1 - r2;
            if (dr < 0) dr = -dr;
            int df = f1 - f2;
            if (df < 0) df = -df;

            return dr == df;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreSquaresInRow(int sq1, int sq2)
    => ((sq1 >> 3) == (sq2 >> 3)) || ((sq1 & 7) == (sq2 & 7));

        #endregion
    }
}