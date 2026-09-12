using ManyConsole.CommandLineUtils;
using BrowserWasmRunner;

var commands = new ConsoleCommand[]
{
    new RunCommand(),
};

return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
