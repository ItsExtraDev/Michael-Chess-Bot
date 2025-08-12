using Michael.src.MoveGen;
using System.Diagnostics;

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
            
            if (algebraic.Length == 5)
            {
                // Handle promotion notation (e.g., "e7e8q")
                char promotionPiece = algebraic[4];
                int moveFlag = Piece.SymbolToPieceType(promotionPiece);
                return new Move(startingSquare, targetSquare, moveFlag);
            }
            return new Move(startingSquare, targetSquare);
        }

        /// <summary>
        /// Converts an internal square index (0-63) to algebraic notation (like "e4", "g3" etc).
        /// </summary>
        /// <param name="index">The internal square index</param>
        /// <returns>The algbaric value of the square index</returns>
        public static string IndexToSquare(int index)
        {
            if (index < 0 || index > 63)
                throw new ArgumentOutOfRangeException("Index must be between 0 and 63.");
            int file = index % 8; // Get file (0-7)
            int rank = index / 8; // Get rank (0-7)
            return $"{(char)('a' + file)}{rank + 1}"; // Convert to algebraic notation
        }

        /// <summary>
        /// Converts a Move object to algebraic notation (like "e2e4", "g1f3" etc).
        /// </summary>
        /// <param name="move">A move object</param>
        /// <returns>An algebraic notation of the move</returns>
        public static string MoveToAlgebraic(Move move)
        {
            string startingSquare = IndexToSquare(move.StartingSquare);
            string targetSquare = IndexToSquare(move.TargetSquare);
            char moveFlag = ' ';
            if (move.IsPromotion())
            {
                moveFlag = char.ToLower(Piece.PieceTypeToSymbol(move.MoveFlag));
            }
            return $"{startingSquare}{targetSquare}{moveFlag}";
        }

        /// <summary>
        /// Performs a perft test on the current position, and prints the results to the console in a readable format.
        /// </summary>
        public static void PrintPerftTest(Board b, int depth)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int totalNodes = 0;

            foreach(Move move in b.GetLegalMoves())
            {
                int nodes = 1;
                if (depth > 1)
                {
                    b.MakeMove(move); // Make the move on the board
                    nodes = MoveGenerator.Perft(b, depth - 1); // Perform perft on the new board
                    b.UndoMove(move); // Undo the move to restore the board state
                }
                totalNodes += nodes; // Add the nodes to the total count
                Console.WriteLine($"{MoveToAlgebraic(move)}: {nodes}");
            }
            sw.Stop();
            Console.WriteLine($"Looked at a total of {totalNodes} Nodes in {sw.ElapsedMilliseconds} ms.");
            Console.WriteLine($"That is an avarge of {totalNodes / sw.ElapsedMilliseconds * 1000} nps.");
        }
    }
}