using Michael.src.Helpers;

/// <summary>
/// Represents a single chess move in a compact, bit-packed format.
/// Stores the move in a single integer (`RawMove`) using bitfields for:
/// - Starting square (6 bits)
/// - Target square (6 bits)
/// - Move flags (4 bits, e.g., promotion, castling, en passant, double pawn push)
/// This format allows fast move generation, comparison, and storage.
/// </summary>
public readonly struct Move
{
    // --- Bit masks ---
    // 6 bits for target square (0–63), stored at bits 4–9
    private const ushort TargetSquareMask = 0b0000001111110000;

    // 4 bits for move flags (0–15), stored at bits 0–3
    private const ushort MoveFlagMask = 0b0000000000001111;

    // --- Bit shifts ---
    private const int TargetSquareShift = 4;     // Target square bits start at 4
    private const int StartingSquareShift = 10;  // Starting square bits start at 10

    // The entire move stored in one integer
    public readonly int RawMove;

    // --- Properties to decode the move ---
    /// <summary>Starting square index (0–63)</summary>
    public int StartingSquare => RawMove >> StartingSquareShift;

    /// <summary>Target square index (0–63)</summary>
    public int TargetSquare => (RawMove & TargetSquareMask) >> TargetSquareShift;

    /// <summary>Move flag (0–15), e.g., promotion, castling, en passant</summary>
    public int MoveFlag => RawMove & MoveFlagMask;

    // --- Constructors ---
    /// <summary>Constructs a normal move (no flags)</summary>
    public Move(int startingSquare, int targetSquare)
    {
        RawMove = startingSquare << StartingSquareShift |
                  targetSquare << TargetSquareShift;
    }

    /// <summary>Constructs a move with a flag (promotion, castling, etc.)</summary>
    public Move(int startingSquare, int targetSquare, int moveFlag)
    {
        RawMove = startingSquare << StartingSquareShift |
                  targetSquare << TargetSquareShift |
                  moveFlag;
    }

    // --- Move type checks ---
    /// <summary>Returns true if this move is null (no move)</summary>
    public bool IsNull() => RawMove == 0;

    /// <summary>Returns true if this move is a castling move</summary>
    public bool IsCastle() => MoveFlag >= 7;

    /// <summary>Returns true if this move is a promotion</summary>
    public bool IsPromotion() => MoveFlag >= 2 && MoveFlag <= 5;

    // --- Equality and hash ---
    public override bool Equals(object obj) => obj is Move other && RawMove == other.RawMove;
    public bool Equals(Move other) => RawMove == other.RawMove;
    public override int GetHashCode() => RawMove;

    // --- Display ---
    public override string ToString() => $"{Notation.MoveToAlgebraic(this)}";

    // --- Static convenience moves ---
    /// <summary>A static null move representing “no move”</summary>
    public static Move NullMove => new Move(0, 0);
}

/// <summary>
/// Defines constants for move types (promotion, castling, en passant, etc.)
/// Values are used in the lower 4 bits of the Move integer
/// </summary>
public static class MoveFlag
{
    public const int EnPassant = 0b0001;        // En passant capture
    public const int PromotionKnight = 0b0010;  // Pawn promoted to knight
    public const int PromotionBishop = 0b0011;  // Pawn promoted to bishop
    public const int PromotionRook = 0b0100;    // Pawn promoted to rook
    public const int PromotionQueen = 0b0101;   // Pawn promoted to queen
    public const int DoublePawnPush = 0b0110;   // Pawn moved two squares
    public const int CastleShort = 0b0111;      // Kingside castling
    public const int CastleLong = 0b1000;       // Queenside castling
}

/// <summary>
/// Stores castling rights for both players and sides
/// Each bit represents one right (1 = right available, 0 = lost)
/// </summary>
public static class CastlingRights
{
    public const int WhiteShort = 0b0001; // White can castle kingside
    public const int WhiteLong = 0b0010;  // White can castle queenside
    public const int BlackShort = 0b0100; // Black can castle kingside
    public const int BlackLong = 0b1000;  // Black can castle queenside
}
