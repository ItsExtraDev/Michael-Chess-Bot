using Michael.src;
using Michael.src.MoveGen;

//Inits
Engine.Init();
PrecomputeMoveData.Init();
Console.WriteLine(Engine.board.GetLegalMoves().Length + " legal moves in the starting position.");
//Always listen UCI for command from the GUI, and respond accordingly.
while (true)
{
    string[] commandTokens = Console.ReadLine().Split(' ');
    UCI.ProcessCommand(commandTokens);
}