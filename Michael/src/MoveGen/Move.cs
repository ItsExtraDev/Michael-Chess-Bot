namespace Michael.src.MoveGen
{
    /// <summary>
    /// Represents a chess move using a compact ushort encoding for efficiency.
    /// Stores starting square, target square, and optional move flags.
    /// </summary>
    public class Move
    {
        // Bit masks and shifts used to extract move components from the packed ushort value.
        private const ushort StartingSquareMask = 0b1111110000000000;
        private const ushort TargetSquareMask = 0b0000001111110000;
        private const ushort MoveFlagMask = 0b0000000000001111;
        private const int TargetSquareShift = 4;
        private const int StartingSquareShift = 10;

        // Packed move data stored as a single integer for quick operations.
        private readonly int RawMove;

        // Properties to decode and access individual move components.
        public int StartingSquare => RawMove >> StartingSquareShift;
        public int TargetSquare => (RawMove & TargetSquareMask) >> TargetSquareShift;
        public int MoveFlag => RawMove & MoveFlagMask;

        /// <summary>
        /// Creates a normal move without any special flag.
        /// </summary>
        public Move(int startingSquare, int targetSquare)
        {
            RawMove = startingSquare << StartingSquareShift | targetSquare << TargetSquareShift;
        }

        /// <summary>
        /// Creates a move with a specific move flag (promotion, castling, etc.).
        /// </summary>
        public Move(int startingSquare, int targetSquare, int moveFlag)
        {
            RawMove = startingSquare << StartingSquareShift | targetSquare << TargetSquareShift | moveFlag;
        }

        /// <summary>
        /// Returns a "null" move representing no action.
        /// </summary>
        public static Move NullMove() => new Move(0, 0);
    }

    /// <summary>
    /// Defines constants for different move types (promotion, castling, en passant, etc.).
    /// </summary>
    public static class MoveFlag
    {
        public const int EnPassant = 0b0001;
        public const int PromotionKnight = 0b0010;
        public const int PromotionBishop = 0b0011;
        public const int PromotionRook = 0b0100;
        public const int PromotionQueen = 0b0101;
        public const int CastleShort = 0b0110;
        public const int CastleLong = 0b0111;
        public const int DoublePawnPush = 0b1000;
    }
}