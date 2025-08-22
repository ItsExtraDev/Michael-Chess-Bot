using Michael.src.Helpers;

namespace Michael.src
{
    public static class GameState
    {
        // <- Masks -> //
        private static int CapturedPieceMask = 0b000000111111000000;
        private static int MovingPieceMask = 0b000000000000111111;
        private static int EnPassantSquareMask = 0b111111000000000000;
        private static int CastlingMask    = 0b1111000000000000000000; // optional

        private static int CapturedPieceShift = 6;
        private static int EnPassantShift = 12;
        private static int CastlingShift = 18;

        public static int MakeGameState(int capturedPiece, int movingPiece, int enPassantSquare, int castlingRight)
            => (castlingRight << CastlingShift) | (enPassantSquare << EnPassantShift) | (capturedPiece << CapturedPieceShift) | movingPiece;

        public static int MovingPiece(int gameState)
            => gameState & MovingPieceMask;

        public static int CapturedPiece(int gameState)
            => (gameState & CapturedPieceMask) >> CapturedPieceShift;

        public static int GetCastlingRights(int gameState)
            => (gameState >> CastlingShift);

        public static int GetEnPassantSquare(int gameState)
            => (gameState & EnPassantSquareMask) >> EnPassantShift;

        public static bool IsCapture(int gameState)
            => (gameState & CapturedPieceMask) != 0;

        // Castling rights (bits: 0 = W short, 1 = W long, 2 = B short, 3 = B long)
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
