using Michael.src;
using Michael.src.Bot;
using Michael.src.Helpers;

/// <summary>
/// UCI (Universal Chess Interface) is a protocol used by chess engines to communicate 
/// with graphical user interfaces (GUIs). This includes commands for engine initialization, 
/// board setup, search commands, and move generation.
/// More information: https://www.chessprogramming.org/UCI
/// </summary>
public static class UCI
{
    /// <summary>
    /// Processes a UCI command received from the GUI and responds accordingly.
    /// Also includes debug commands for testing purposes.
    /// </summary>
    /// <param name="tokens">The split UCI command tokens.</param>
    public static void ProcessCommand(string[] tokens)
    {
        switch (tokens[0])
        {
            case "uci":
                Console.WriteLine("id name Michael Chess Engine");
                Console.WriteLine("id author Extra_");
                Console.WriteLine("uciok");
                break;

            case "isready":
                Console.WriteLine("readyok");
                break;

            case "ucinewgame":
                break;

            case "position":
                Engine.LoadBoardFromPositionCommand(tokens);
                break;

            case "go":
                //Perfrom a perft test
                if (tokens.Length == 3 && tokens[1] == "perft")
                {
                    int depth;
                    if (int.TryParse(tokens[2], out depth))
                    {
                        Notation.PrintPerftTest(Engine.board, depth);
                        return; // Exit after perft test
                    }
                }
                
                string bestMoveString = Notation.MoveToAlgebraic(Engine.GetBestMove(SetUpClock(tokens)));
                Console.WriteLine($"bestmove {bestMoveString}");
                break;

            case "stop":
                break;

            case "quit":
                Environment.Exit(0);
                break;

            // Debug commands
            case "d":
                BoardHelper.PrintBoard(Engine.board);
                break;

            default:
                Console.WriteLine("Unknown command: " + string.Join(' ', tokens));
                break;
        }
    }

    public static Clock SetUpClock(string[] tokens)
    {
        int color = Engine.board.ColorToMove;
        int timeLeftInMs = int.Parse(tokens[color == Piece.White ? 2 : 4]);
        int movesToGo = 0;
        int incrament = 0;

        for (int i = 0; i < tokens.Length; i++)
        {
            if ((tokens[i] == "winc" && color == Piece.White) ||
                (tokens[i] == "binc" && color == Piece.Black))
                incrament = int.Parse(tokens[i + 1]);

            else if (tokens[i] == "movestogo")
                movesToGo = int.Parse(tokens[i + 1]);
        }
        return new Clock(timeLeftInMs, movesToGo, incrament);
    }
}
