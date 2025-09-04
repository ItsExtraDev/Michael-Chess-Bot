public class Move
{
    private const ushort StartingSquareMask = 0b1111110000000000;
    private const ushort TargetSquareMask = 0b0000001111110000;
    private const ushort MoveFlagMask = 0b0000000000001111;
    private const int TargetSquareShift = 4;
    private const int StartingSquareShift = 10;

    private readonly int RawMove;

    public int StartingSquare => RawMove >> StartingSquareShift;
    public int TargetSquare => (RawMove & TargetSquareMask) >> TargetSquareShift;
    public int MoveFlag => RawMove & MoveFlagMask;

    public Move(int startingSquare, int targetSquare)
    {
        RawMove = startingSquare << StartingSquareShift | targetSquare << TargetSquareShift;
    }

    public Move(int startingSquare, int targetSquare, int moveFlag)
    {
        RawMove = startingSquare << StartingSquareShift | targetSquare << TargetSquareShift | moveFlag;
    }

    public bool IsCastle() => MoveFlag >= 7;
    public bool IsPromotion() => MoveFlag >= 2 && MoveFlag <= 5;
    public bool IsNull() => RawMove == 0;

    public override bool Equals(object obj) => obj is Move other && RawMove == other.RawMove;
    public bool Equals(Move other) => RawMove == other.RawMove;
    public override int GetHashCode() => RawMove;

    public override string ToString() => $"Move({StartingSquare}->{TargetSquare}, flag={MoveFlag})";

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
        public const int DoublePawnPush = 0b0110;
        public const int CastleShort = 0b0111;
        public const int CastleLong = 0b1000;
    }
    /// <summary>
    /// Contatins data about castling rights for both players and both sides.
    /// </summary>
    public static class CastlingRights
    {
        public const int WhiteShort = 0b0001; // White can castle short (kingside)
        public const int WhiteLong = 0b0010; // White can castle long (queenside)
        public const int BlackShort = 0b0100; // Black can castle short (kingside)
        public const int BlackLong = 0b1000; // Black can castle long (queenside)
    }
