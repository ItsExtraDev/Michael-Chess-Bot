namespace Michael.src
{
    public static class GameState
    {
        // <- Masks -> //
        private static int CapturedPieceMask = 0b000000111111000000;
        private static int MovingPieceMask = 0b000000000000111111;
        private static int EnPassantSquareMask = 0b111111000000000000;
        private static int CapturedPieceShift = 6;
        private static int EnPassantShift = 12;

        public static int MakeGameState(int CapturedPiece, int MovingPiece)
            => (CapturedPiece << CapturedPieceShift) | MovingPiece;
        public static int MakeGameState(int CapturedPiece, int MovingPiece, int enPassantSquare)
            => (enPassantSquare << EnPassantShift) | (CapturedPiece << CapturedPieceShift) | MovingPiece;

        public static int MovingPiece(int gameState)
            => gameState & MovingPieceMask;

        public static int CapturedPiece(int gameState)
            => (gameState & CapturedPieceMask) >> CapturedPieceShift;

        public static int GetEnPassantSquare(int gameState)
            => gameState >> EnPassantShift;

        public static bool IsCapture(int gameState)
            => (gameState & CapturedPieceMask) != 0;
    }
}
