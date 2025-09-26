using Michael.src;
using Michael.src.Helpers;

/// <summary>
/// MatchManager serves as the main connection between the chess engine, 
/// the game logic, and the GUI. 
/// It manages the board state, move execution, and communication with the GUI via UCI.
/// </summary>
public static class MatchManager
{
    /// <summary>
    /// The main board instance used throughout the engine.
    /// All classes that interact with the board should use this instance.
    /// </summary>
    public static Board board;

    /// <summary>
    /// Initializes the chess engine by starting a new game at the default starting position.
    /// </summary>
    public static void Init()
    {
        StartNewGame();
    }

    /// <summary>
    /// Starts a new game, optionally from a FEN string.
    /// If no FEN is provided, it defaults to the standard starting position.
    /// </summary>
    /// <param name="fenString">FEN string representing the position (optional).</param>
    public static void StartNewGame(string fenString = FEN.StartingFEN)
    {
        board = new Board(fenString);
    }

    /// <summary>
    /// Loads a board position from a UCI 'position' command sent by the GUI.
    /// Supports both 'startpos' (default starting position) and FEN strings.
    /// Applies any moves included in the command in order.
    /// </summary>
    /// <param name="commandTokens">The UCI command tokens split by spaces.</param>
    public static void LoadBoardFromPositionCommand(string[] commandTokens)
    {
        // Determine starting position
        if (commandTokens[1] == "startpos")
        {
            StartNewGame(); // Load standard starting position
        }
        else if (commandTokens[1] == "fen" && commandTokens.Length > 2)
        {
            // Combine the 6 FEN fields to form the full FEN string
            string fenString = string.Join(" ", commandTokens.Skip(2).Take(6));
            StartNewGame(fenString);
        }
        else
        {
            return; // Invalid or unsupported command, do nothing
        }

        // Process moves after the "moves" token
        bool hasSeenMove = false;
        for (int index = 0; index < commandTokens.Length; index++)
        {
            if (!hasSeenMove)
            {
                if (commandTokens[index] == "moves")
                    hasSeenMove = true; // Start processing moves
                continue; // Skip until we reach "moves"
            }

            string moveString = commandTokens[index];
            Move move = Notation.AlgebraicToMove(moveString);

            // Handle special move types first

            // En passant
            if (board.EnPassantSquare == move.TargetSquare && BoardHelper.Rank(move.TargetSquare) > 0 && BoardHelper.Rank(move.TargetSquare) < 7 && Piece.PieceType(board.Squares[move.StartingSquare]) == Piece.Pawn)
            {
                board.MakeMove(new Move(move.StartingSquare, move.TargetSquare, MoveFlag.EnPassant));
                continue;
            }

            // Double pawn push
            else if (Math.Abs(BoardHelper.Rank(move.TargetSquare) - BoardHelper.Rank(move.StartingSquare)) == 2
                     && Piece.PieceType(board.Squares[move.StartingSquare]) == Piece.Pawn)
            {
                board.MakeMove(new Move(move.StartingSquare, move.TargetSquare, MoveFlag.DoublePawnPush));
                continue;
            }

            // Castling
            if (Piece.PieceType(board.Squares[move.StartingSquare]) == Piece.King
                && Math.Abs(move.StartingSquare - move.TargetSquare) == 2)
            {
                int flag = MoveFlag.CastleShort;

                // If king moves to the queenside file, adjust flag for long castle
                if (BoardHelper.File(move.TargetSquare) == 2)
                    flag = MoveFlag.CastleLong;

                board.MakeMove(new Move(move.StartingSquare, move.TargetSquare, flag));
                continue;
            }

            // Regular move
            board.MakeMove(move);
            board.plyCount++; // Increment the ply count after each move
        }
    }
}
