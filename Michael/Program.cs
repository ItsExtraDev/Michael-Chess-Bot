using Michael.src.MoveGen;

//Inits
MatchManager.Init();
PrecomputeMoveData.Init();
Magic.Init();

UCI uci = new();

//Always listen UCI for command from the GUI, and respond accordingly.
while (true)
{
    string message = Console.ReadLine();
    uci.ProcessCommand(message);
}
