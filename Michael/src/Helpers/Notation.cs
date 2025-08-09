using Michael.src.MoveGen;

namespace Michael.src.Helpers
{
    /// <summary>
    /// Provides utility methods for chess notation and move representation.
    /// Includes methods for converting moves to standard notation, square index to algebraic notation,
    /// and vice versa.
    /// mainly used for converting moves from the GUI to the internal representation
    /// </summary>
    public static class Notation
    {
        /// <summary>
        /// Converts a square algebraic notation (like "e4", "g3" etc),
        /// to an interntal index the program can understand (0-63).
        /// </summary>
        /// <param name="square">algebraic square notation</param>
        /// <returns>The square internal index</returns>
        public static int SquareToIndex(string square)
        {
            if (square.Length != 2)
                throw new ArgumentException("Square must be in the format 'a1', 'h8', etc.");
            int file = square[0] - 'a'; // Convert 'a' to 0, 'b' to 1, ..., 'h' to 7
            int rank = square[1] - '1'; // Convert '1' to 0, '2' to 1, ..., '8' to 7
            if (file < 0 || file > 7 || rank < 0 || rank > 7)
                throw new ArgumentOutOfRangeException("Square is out of bounds.");
            return rank * 8 + file; // Calculate index
        }

        /// <summary>
        /// Converts an algebraic move notation (like "e2e4", "g1f3" etc), 
        /// to an internal Move object that the program can understand. used to load moves from the GUI.
        /// </summary>
        /// <param name="algebraic">The algbraic move notation</param>
        /// <returns>The internal move object.</returns>
        public static Move AlgebraicToMove(string algebraic)
        {
            if (algebraic.Length < 4 || algebraic.Length > 5)
                throw new ArgumentException("Algebraic notation must be 4 to 5 characters long.");
            int startingSquare = SquareToIndex(algebraic.Substring(0, 2));
            int targetSquare = SquareToIndex(algebraic.Substring(2, 2));
            
            return new Move(startingSquare, targetSquare);
        }
    }
}