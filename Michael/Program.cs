using Michael.src;

//Inits
Engine.Init();

//Always listen UCI for command from the GUI, and respond accordingly.
while (true)
{
    string[] commandTokens = Console.ReadLine().Split(' ');
    UCI.ProcessCommand(commandTokens);
}