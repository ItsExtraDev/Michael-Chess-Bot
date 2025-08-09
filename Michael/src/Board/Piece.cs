using System.Reflection.Metadata.Ecma335;

namespace Michael.src
{
    /// <summary>
    /// Provides constants and utility methods for representing and manipulating chess pieces.
    /// 
    /// Piece types are represented by integers (Pawn, Knight, Bishop, Rook, Queen, King),
    /// and colors are represented as White (0) and Black (1).
    /// 
    /// Pieces are encoded as integers combining type and color using bit masks and shifts.
    /// This class includes methods to create pieces from type and color, 
    /// extract the type or color from a piece, and check piece characteristics such as sliding moves.
    /// </summary>
    public static class Piece
    {
        // <- Piece Types -> //
        public const int None = 0;
        public const int Pawn = 1;
        public const int Knight = 2;
        public const int Bishop = 3;
        public const int Rook = 4;
        public const int Queen = 5;
        public const int King = 6;

        // <- Colors -> //
        public const int White = 0;
        public const int Black = 1;

        // <- Masks -> //
        public const int PieceTypeMask = 0b01111;
        public const int ColorMask = 0b10000;
        public const int ColorShift = 4;

        /// <summary>
        /// Creates a piece integer from a piece type and color.
        /// </summary>
        /// <param name="pieceType">The type of the piece (Pawn, Knight, etc.).</param>
        /// <param name="color">The color of the piece (White or Black).</param>
        /// <returns>An integer encoding the piece type and color.</returns>
        public static int CreatePiece(int pieceType, int color)
            => color << ColorShift | pieceType;

        /// <summary>
        /// Creates a piece integer from a piece type and a boolean indicating if the piece is white.
        /// </summary>
        /// <param name="pieceType">The type of the piece.</param>
        /// <param name="isWhite">True if the piece is white; false if black.</param>
        /// <returns>An integer encoding the piece type and color.</returns>
        public static int CreatePiece(int pieceType, bool isWhite)
            => CreatePiece(pieceType, isWhite ? White : Black);

        // <- Helper methods -> //

        /// <summary>
        /// Extracts the piece type from an encoded piece integer.
        /// </summary>
        /// <param name="piece">The encoded piece integer.</param>
        /// <returns>The piece type (Pawn, Knight, etc.).</returns>
        public static int PieceType(int piece)
            => piece & PieceTypeMask;

        /// <summary>
        /// Extracts the color from an encoded piece integer.
        /// </summary>
        /// <param name="piece">The encoded piece integer.</param>
        /// <returns>The color (White or Black).</returns>
        public static int Color(int piece)
            => piece >> ColorShift;

        /// <summary>
        /// Checks if the piece is white.
        /// </summary>
        /// <param name="piece">The encoded piece integer.</param>
        /// <returns>True if white; false otherwise.</returns>
        public static bool IsWhite(int piece)
            => piece >> ColorShift == White;

        /// <summary>
        /// Determines whether the piece is a sliding piece (Bishop, Rook, Queen).
        /// </summary>
        /// <param name="piece">The encoded piece integer.</param>
        /// <returns>True if sliding piece; false otherwise.</returns>
        public static bool IsSliding(int piece)
            => PieceType(piece) >= Bishop && PieceType(piece) <= Queen;

        /// <summary>
        /// Converts a FEN letter symbol to a piece type.
        /// </summary>
        /// <param name="Symbol">The FEN letter symbol.</param>
        /// <returns>The corresponding piece type.</returns>
        public static int SymbolToPieceType(char Symbol)
        {
            //Converts the symbol to upper case for the switch loop. otherwise
            //we will have to worry to make it upper case before every call.
            Symbol = char.ToUpper(Symbol);

            return (Symbol) switch
            {
                'P' => Pawn,
                'N' => Knight,
                'B' => Bishop,
                'R' => Rook,
                'Q' => Queen,
                'K' => King,
                _ => None
            };
        }

        /// <summary>
        /// Converts a piece type to a FEN letter symbol
        /// </summary>
        /// <param name="pieceType">The piece type.</param>
        /// <returns>The corresponding FEN letter symbol.</returns>
        public static char PieceTypeToSymbol(int pieceType)
        {
            return (pieceType) switch
            {
                Pawn => 'P',
                Knight => 'N',
                Bishop => 'B',
                Rook => 'R',
                Queen => 'Q',
                King => 'K'
            };
        }
    }
}