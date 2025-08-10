namespace Michael.src
{
    public static class GameState
    {
        // <- Masks -> //
        private static int CapturedPieceMask = 0b111111000000;
        private static int MovingPieceMask = 0b000000111111;
        private static int CapturedPieceShift = 6;

        public static int MakeGameState(int CapturedPiece, int MovingPiece)
            => (CapturedPiece << CapturedPieceShift) | MovingPiece;

        public static int MovingPiece(int gameState)
            => gameState & MovingPieceMask;

        public static int CapturedPiece(int gameState)
            => gameState >> CapturedPieceShift;

        public static bool IsCapture(int gameState)
            => (gameState & CapturedPieceMask) != 0;
    }
}
