using Michael;
using Michael.src;
using Michael.src.Helpers;
using Michael.src.Search;

/// <summary>
/// UCI (Universal Chess Interface) is a protocol used by chess engines to communicate 
/// with graphical user interfaces (GUIs). This includes commands for engine initialization, 
/// board setup, search commands, and move generation.
/// More information: https://www.chessprogramming.org/UCI
/// </summary>
public class UCI
{
    //Write all commands, responses and search results to a file.
    //Used for debugging, by defualt set to false
    private const bool writeToFile = true;

    readonly Bot player;
    readonly LogWriter writer;

    public UCI()
    {
        player = new Bot();
        writer = new LogWriter(FileType.UCI, true);
        player.OnMoveChosen += OnMoveChosen;
    }

    /// <summary>
    /// Processes a UCI command received from the GUI and responds accordingly.
    /// Also includes debug commands for testing purposes.
    /// </summary>
    /// <param name="tokens">The split UCI command tokens.</param>
    public void ProcessCommand(string message)
    {
        writer.WriteToFile("");
        writer.WriteToFile($"Recived command: {message}");

        string[] tokens = message.Split(' ');

        switch (tokens[0])
        {
            case "uci":
                Respond("id name Michael Chess Engine");
                Respond("id author Extra_");
                Respond("uciok");
                break;

            case "isready":
                Respond("readyok");
                break;

            case "ucinewgame":
                break;

            case "position":
                MatchManager.LoadBoardFromPositionCommand(tokens);
                break;

            case "go":
                //Perfrom a perft test
                if (tokens.Length == 3 && tokens[1] == "perft")
                {
                    int depth;
                    if (int.TryParse(tokens[2], out depth))
                    {
                        Notation.PrintPerftTest(MatchManager.board, depth);
                        return; // Exit after perft test
                    }
                }
                ProcessGoCommand();
                break;

            case "stop":
                if (player.IsThinking)
                {
                    player.EndSearch();
                }
                break;

            case "quit":
                Environment.Exit(0);
                break;

            // Debug commands
            case "d":
                BoardHelper.PrintBoard(MatchManager.board);
                break;

            default:
                Respond($"Unknown command: {message}");
                break;
        }
    }

    private void Respond(string message)
    {
        Console.WriteLine(message);
        writer.WriteToFile("Response sent: " + message);
    }

    private void ProcessGoCommand()
    {
        if (player.UseMaxTimePerMove)
        {
            player.StartThinkingTimed(player.MaxTimePerMoveInMS);
        }
        //TODO add time per move calculation here
        else
        {
            player.StartThinkingTimed(player.MaxTimePerMoveInMS);
        }
    }

    public void OnMoveChosen(string move)
    {
        Respond("bestmove " + move);
    }
}
