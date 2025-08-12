using Michael.src;
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
                string bestMoveString = Notation.MoveToAlgebraic(Engine.GetBestMove());
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
}
