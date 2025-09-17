using Michael.src.Helpers;

namespace Michael.src
{
    /// <summary>
    /// Represents a compact encoding of a chess game's state in a single integer.
    /// This includes:
    /// - Moving piece
    /// - Captured piece (if any)
    /// - En passant square
    /// - Castling rights
    /// 
    /// The encoding allows fast storage, retrieval, and undoing of moves.
    /// </summary>
    public static class GameState
    {
        // --- Bit masks for extracting information from gameState integer ---
        private static int MovingPieceMask = 0b000000000000111111; // bits 0-5
        private static int CapturedPieceMask = 0b000000111111000000; // bits 6-11
        private static int EnPassantSquareMask = 0b111111000000000000; // bits 12-17
        // Castling rights stored in remaining upper bits (bit 18+)

        // --- Bit shifts for packing values into a single integer ---
        private static int CapturedPieceShift = 6;
        private static int EnPassantShift = 12;
        private static int CastlingShift = 18;

        /// <summary>
        /// Packs moving piece, captured piece, en passant square, and castling rights into a single integer.
        /// </summary>
        public static int MakeGameState(int capturedPiece, int movingPiece, int enPassantSquare, int castlingRight)
            => (castlingRight << CastlingShift) | (enPassantSquare << EnPassantShift) | (capturedPiece << CapturedPieceShift) | movingPiece;

        /// <summary>
        /// Returns the moving piece from the game state.
        /// </summary>
        public static int MovingPiece(int gameState)
            => gameState & MovingPieceMask;

        /// <summary>
        /// Returns the captured piece (if any) from the game state.
        /// </summary>
        public static int CapturedPiece(int gameState)
            => (gameState & CapturedPieceMask) >> CapturedPieceShift;

        /// <summary>
        /// Returns the castling rights (4-bit mask) stored in the game state.
        /// Bits: 0 = White short, 1 = White long, 2 = Black short, 3 = Black long
        /// </summary>
        public static int GetCastlingRights(int gameState)
            => (gameState >> CastlingShift);

        /// <summary>
        /// Returns the en passant target square from the game state.
        /// </summary>
        public static int GetEnPassantSquare(int gameState)
            => (gameState & EnPassantSquareMask) >> EnPassantShift;

        /// <summary>
        /// Returns true if the move captured a piece.
        /// </summary>
        public static bool IsCapture(int gameState)
            => (gameState & CapturedPieceMask) != 0;

        // --- Castling checks ---
        public static bool CanWhiteCastleShort(int gameState)
            => BitboardHelper.IsBitSet((ulong)(gameState >> CastlingShift), 0);

        public static bool CanWhiteCastleLong(int gameState)
            => BitboardHelper.IsBitSet((ulong)(gameState >> CastlingShift), 1);

        public static bool CanBlackCastleShort(int gameState)
            => BitboardHelper.IsBitSet((ulong)(gameState >> CastlingShift), 2);

        public static bool CanBlackCastleLong(int gameState)
            => BitboardHelper.IsBitSet((ulong)(gameState >> CastlingShift), 3);
    }
}
