using Michael.src;
using Michael.src.Helpers;
using Michael.src.MoveGen;

//Inits
Engine.Init();
PrecomputeMoveData.Init();
//Notation.PrintPerftTest(Engine.board, 11); //Prints the perft test for the current position.
//Always listen UCI for command from the GUI, and respond accordingly.
while (true)
{
    string[] commandTokens = Console.ReadLine().Split(' ');
    UCI.ProcessCommand(commandTokens);
}