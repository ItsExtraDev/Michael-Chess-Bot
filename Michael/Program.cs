using Michael.src;
using Michael.src.MoveGen;

//Inits
Engine.Init();
PrecomputeMoveData.Init();

//Always listen UCI for command from the GUI, and respond accordingly.
while (true)
{
    string[] commandTokens = Console.ReadLine().Split(' ');
    UCI.ProcessCommand(commandTokens);
}