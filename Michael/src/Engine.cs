using Michael.src.Helpers;
using Michael.src.MoveGen;

namespace Michael.src
{
    /// <summary>
    /// This class serves as the main connection between the chess engine itself, the logic of the game,
    /// and the GUI. It is responsible for initializing the game state, managing the board, making moves
    /// and communicating with the GUI via the Universal Chess Interface (UCI).
    /// </summary>
    public static class Engine
    {
        //Main board instance of the engine.
        //Any class the uses the board would call this one board instance.
        public static Board board;

        /// <summary>
        /// Initializes the chess engine by starting a new game with the default starting position.
        /// </summary>
        public static void Init()
        { 
            StartNewGame();
        }

        /// <summary>
        /// Initializes the chess engine, setting up the board and any necessary game state.
        /// Loads the starting position by default, but can be changed by giving a FEN string.
        /// </summary>
        public static void StartNewGame(string fenString = FEN.StartingFEN)
        {
            board = new Board(fenString);
        }

        /// <summary>
        /// Returns the best move for the current position on the board, according to the engine.
        /// </summary>
        /// <returns>The best move in the current position</returns>
        public static Move GetBestMove()
        {
            //Placeholder for the best move logic.
            //Currently , it returns a random legal move from the board. but some sort of search algorithm should be implemented here.
            Random random = new Random();
            return board.GetLegalMoves()[random.Next(board.GetLegalMoves().Length)];
        }

        /// <summary>
        /// Loads the board from a position command from the GUI.
        /// </summary>
        public static void LoadBoardFromPositionCommand(string[] commandTokens)
        {

            if (commandTokens[1] == "startpos")
            {
                //If the command is "startpos", load the starting position.
                StartNewGame();
            }
            else if (commandTokens[1] == "fen")
            {
                //If the command is "fen", load the position from the FEN string provided.
                if (commandTokens.Length > 2)
                {
                    string fenString = string.Join(" ", commandTokens.Skip(2).Take(6));
                    StartNewGame(fenString);
                }
            }
            else
            {
                return; // Invalid command, do nothing
            }//position startpos moves a2a4 a7a6 a4a5 b7b5 a5b6

            bool hasSeenMove = false;
            for (int index = 0; index < commandTokens.Length; index++)
            {
                if (!hasSeenMove)
                {
                    if (commandTokens[index] == "moves")
                    {
                        hasSeenMove = true; // Start processing moves after the "moves" token
                    }
                    continue; // Skip the "moves" token
                }
                //Make each move in the position command.
                string moveString = commandTokens[index];
                Move move = Notation.AlgebraicToMove(moveString);

                //En passant
                if (board.EnPassantSquare == move.TargetSquare && board.Squares[move.StartingSquare] == Piece.Pawn)
                {
                    board.MakeMove(new Move(move.StartingSquare, move.TargetSquare, MoveFlag.EnPassant));
                    continue;
                }
                //Double pawn push
                else if (Math.Abs(BoardHelper.Rank(move.TargetSquare) - BoardHelper.Rank(move.StartingSquare)) ==2 && Piece.PieceType(board.Squares[move.StartingSquare]) == Piece.Pawn)
                {
                    board.MakeMove(new Move(move.StartingSquare, move.TargetSquare,MoveFlag.DoublePawnPush));
                    continue;
                }
                //Castle
                if (Piece.PieceType(board.Squares[move.StartingSquare]) == Piece.King && Math.Abs(move.StartingSquare - move.TargetSquare) == 2)
                {
                    int flag = MoveFlag.CastleShort;

                    if (BoardHelper.File(move.TargetSquare) == 2)
                        flag++;

                    board.MakeMove(new Move(move.StartingSquare, move.TargetSquare, flag));
                    continue;
                }
                board.MakeMove(move);
                board.plyCount++; // Increment the ply count after each move made   
            }
        }
    }
}