using Michael;
using Michael.src.Helpers;

/// <summary>
/// UCI (Universal Chess Interface) is a protocol used by chess engines to communicate 
/// with graphical user interfaces (GUIs). This includes commands for engine initialization, 
/// board setup, search commands, and move generation.
/// More information: https://www.chessprogramming.org/UCI
/// </summary>
public class UCI
{
    // --- Readonly references ---
    // The Bot instance responsible for move calculation and search.
    readonly Bot player;

    // Writer for logging UCI interactions to a file (debugging / analysis).
    readonly LogWriter writer;

    /// <summary>
    /// Initializes a new UCI instance, sets up the engine, and subscribes to move events.
    /// </summary>
    public UCI()
    {
        player = new Bot();
        writer = new LogWriter(FileType.UCI, false);

        // Subscribe to the Bot's move-chosen event to send moves to the GUI.
        player.OnMoveChosen += OnMoveChosen;
    }

    /// <summary>
    /// Processes a UCI command received from the GUI and responds accordingly.
    /// Supports engine commands, position setup, search commands, and debug commands.
    /// </summary>
    /// <param name="message">The raw UCI command string.</param>
    public void ProcessCommand(string message)
    {
        // Log received command
        writer.WriteToFile("");
        writer.WriteToFile($"Received command: {message}");

        string[] tokens = message.Split(' ');

        switch (tokens[0])
        {
            // --- Engine identification ---
            case "uci":
                Respond("id name Michael Chess Engine");   // Engine name
                Respond("id author ItsExtra");             // Author
                Respond("uciok");                          // Acknowledge UCI mode
                break;

            // --- Engine readiness check ---
            case "isready":
                Respond("readyok");
                break;

            // --- Start a new game ---
            case "ucinewgame":
                // Typically reset internal engine state here
                break;

            // --- Position setup ---
            case "position":
                MatchManager.LoadBoardFromPositionCommand(tokens);
                break;

            // --- Search / move calculation ---
            case "go":
                // Optional perft testing: e.g., "go perft 4"
                if (tokens.Length == 3 && tokens[1] == "perft")
                {
                    if (int.TryParse(tokens[2], out int depth))
                    {
                        Notation.PrintPerftTest(MatchManager.board, depth);
                        return; // Exit after perft test
                    }
                }
                ProcessGoCommand(); // Normal move calculation
                break;

            // --- Stop the engine's search ---
            case "stop":
                if (player.IsThinking)
                {
                    player.EndSearch();
                }
                break;

            // --- Quit the engine ---
            case "quit":
                Environment.Exit(0);
                break;

            // --- Debug command: print board ---
            case "d":
                BoardHelper.PrintBoard(MatchManager.board);
                break;

            // --- Unknown commands ---
            default:
                Respond($"Unknown command: {message}");
                break;
        }
    }

    /// <summary>
    /// Sends a response back to the GUI and logs it.
    /// </summary>
    /// <param name="message">The response message.</param>
    private void Respond(string message)
    {
        Console.WriteLine(message);                 // Send to GUI
        writer.WriteToFile("Response sent: " + message); // Log for debugging
    }

    /// <summary>
    /// Handles the "go" command by starting the engine's search.
    /// Currently only supports timed search per move.
    /// TODO: Implement proper time management based on remaining game time.
    /// </summary>
    private void ProcessGoCommand()
    {
        if (player.UseMaxTimePerMove)
        {
            player.StartThinkingTimed(player.MaxTimePerMoveInMS);
        }
        else
        {
            // For now, also use max time per move if no time control specified
            player.StartThinkingTimed(player.MaxTimePerMoveInMS);
        }
    }

    /// <summary>
    /// Event handler called when the Bot chooses a move.
    /// Sends the move to the GUI in the UCI "bestmove" format.
    /// </summary>
    /// <param name="move">The move in UCI notation.</param>
    public void OnMoveChosen(string move)
    {
        Respond("bestmove " + move);
    }
}
