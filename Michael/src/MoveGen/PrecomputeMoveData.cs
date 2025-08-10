using Michael.src.Helpers;

namespace Michael.src.MoveGen
{
    /// <summary>
    /// using this class, we precompute all the possible moves for pawns, knights, and kings before the game starts.
    /// this helps to speed up move generation during the game, as the engine have less work to do in runtime.
    /// Bishop, Rooks and queens will be precomputed using Magic Bitboards, in the Magic class, as the require move complex logic.
    /// The moves will be stored in a ulong array, where each index represents a square on the board.
    /// more information about this method can be found at: https://www.chessprogramming.org/Table-driven_Move_Generation
    /// </summary>
    public static class PrecomputeMoveData
    {
        //File masks for the bitboards, to make sure we don't go off the board.
        public const ulong NotAFile = 0xFEFEFEFEFEFEFEFE;
        public const ulong NotABFile = 0xFCFCFCFCFCFCFCFC;
        public const ulong NotHFile = 0x7F7F7F7F7F7F7F7F;
        public const ulong NotGHFile = 0x3F3F3F3F3F3F3F3F;

        /// <summary>
        /// Precompute all possible moves for pawns, knights, and kings.
        /// </summary>
        public static void Init()
        {
            PrecomputeKnightMoves();
        }

        #region Pawn
        #endregion

        #region Knight
        /// <summary>
        /// Precompute knight moves for all squares on the board.
        /// Each knight can move to 8 possible positions, but some may be off the board.
        /// Store these moves in a ulong array where each index represents a square on the board.
        /// If a bit is set in the ulong, it means the knight can move to that square from the index square.
        /// </summary>
        private static void PrecomputeKnightMoves()
        {
            //Loop over all the squares on the board.
            for (int square = 0; square < 64; square++)
            {
                MoveGenerator.KnightMoves[square] = CalculateKnightMoves(1ul<<square);
            }
        }

        /// <summary>
        /// Calculates all the possible knight moves from a given square.
        /// </summary>
        /// <param name="square">The square the knight stands on</param>
        /// <returns>all the squares the knight can attack from his starting square.</returns>
        private static ulong CalculateKnightMoves(ulong startingSquare)
        {
            ulong attacks = 0;

            attacks |= (startingSquare & NotHFile) << 17;
            attacks |= (startingSquare & NotGHFile) << 10;
            attacks |= (startingSquare & NotGHFile) >> 6;
            attacks |= (startingSquare & NotHFile) >> 15;
            attacks |= (startingSquare & NotAFile) << 15;
            attacks |= (startingSquare & NotABFile) << 6;
            attacks |= (startingSquare & NotABFile) >> 10;
            attacks |= (startingSquare & NotAFile) >> 17;

            return attacks;
        }
        #endregion

        #region King
        #endregion
    }
}