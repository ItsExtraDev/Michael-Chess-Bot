namespace Michael.src.Helpers
{
    /// <summary>
    /// Provides helper methods for working with the board class,
    /// including converting squares to ranks, printing the board etc.
    /// </summary>
    public static class BoardHelper
    {
        /// <summary>
        /// Calculates the rank of the given square
        /// </summary>
        /// <param name="square"></param>
        /// <returns>The rank of the given square</returns>
        public static int Rank(int square)
            => square / 8;

        /// <summary>
        /// Calculates the file of the given square
        /// </summary>
        /// <param name="square"></param>
        /// <returns>The file of the given square</returns>
        public static int File(int square)
            => square % 8;

        /// <summary>
        /// Prints a copy of the board to command promt.
        /// mainly used to help visualize and for debugging
        /// </summary>
        /// <param name="board"></param>
        public static void PrintBoard(Board board)
        {
            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {

                }
            }
        }
    }
}