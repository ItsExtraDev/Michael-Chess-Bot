using Michael.src.MoveGen;

//Inits
MatchManager.Init();
PrecomputeMoveData.Init();
Magic.Init();

//Bug in position fen 8/8/3b4/3b4/3k4/8/p2K4/8 b - - 1 86 moves a2a1q


UCI uci = new();
//Always listen UCI for command
//rom the GUI, and respond accordingly.
while (true)
{
    string message = Console.ReadLine();
    uci.ProcessCommand(message);
}
