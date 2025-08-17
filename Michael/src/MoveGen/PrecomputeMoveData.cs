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
            PrecomputePawnAttacks();
            PrecomputeKnightMoves();
            PrecomputeKingMoves();
        }

        #region Pawn
        /// <summary>
        /// Precompute pawn captures for all squares on the board.
        /// Each pawn can capture to the left or right diagonally,
        /// Store these moves in a ulong array where each index represents a square on the board.
        /// If a bit is set in the ulong, it means the knight can move to that square from the index square.
        /// </summary>
        private static void PrecomputePawnAttacks()
        {
            //Loop over all the squares on the board.
            for (int square = 0; square < 64; square++)
            {
                MoveGenerator.WhitePawnAttacks[square] = CalculatePawnMoves(1UL << square, true);
                MoveGenerator.BlackPawnAttacks[square] = CalculatePawnMoves(1UL << square, false);
            }
        }

        /// <summary>
        /// Calculates all the possible pawn attacks from a given square and color.
        /// </summary>
        /// <param name="square">The square the pawn stands on</param>
        /// <param name="isWhite">True if the pawn is white, false if it is black.</param>
        /// <returns>all the squares the pawn can attack from his starting square.</returns>
        private static ulong CalculatePawnMoves(ulong startingSquare, bool isWhite)
        {
            ulong attacks = 0;

            if (isWhite)
            {
                // White pawns attack diagonally up-left and up-right
                attacks |= (startingSquare & NotHFile) << 9;  // Up-right
                attacks |= (startingSquare & NotAFile) << 7;  // Up-left
                return attacks;
            }
            // Black pawns attack diagonally down-left and down-right
            attacks |= (startingSquare & NotHFile) >> 7;  // Down-right
            attacks |= (startingSquare & NotAFile) >> 9;  // Down-left

            return attacks;
        }
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
        /// <summary>
        /// Precompute king moves for all squares on the board.
        /// Each king can move to 8 possible positions, but some may be off the board.
        /// Store these moves in a ulong array where each index represents a square on the board.
        /// If a bit is set in the ulong, it means the king can move to that square from the index square.
        /// </summary>
        private static void PrecomputeKingMoves()
        {
            //Loop over all the squares on the board.
            for (int square = 0; square < 64; square++)
            {
                MoveGenerator.KingMoves[square] = CalculateKingMoves(1ul << square);
            }
        }

        /// <summary>
        /// Calculates all the possible king moves from a given square.
        /// </summary>
        /// <param name="square">The square the king stands on</param>
        /// <returns>all the squares the king can attack from his starting square.</returns>
        public static ulong CalculateKingMoves(ulong startingSquare)
        {
            ulong attacks = 0;
            attacks |= (startingSquare & NotHFile) << 9;  // Move to the right
            attacks |= (startingSquare) << 8; // Move up
            attacks |= (startingSquare & NotAFile) << 7;  // Move to the left
            attacks |= (startingSquare) >> 1; // Move down
            attacks |= (startingSquare & NotAFile) >> 1;  // Move down-left
            attacks |= (startingSquare & NotHFile) >> 8; // Move down-right
            attacks |= (startingSquare & NotAFile) >> 9;  // Move left
            attacks |= (startingSquare & NotHFile) >> 7; // Move up-right
            return attacks;
        }   
        #endregion
    }
}