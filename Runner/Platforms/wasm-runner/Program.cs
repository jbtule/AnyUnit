using ManyConsole.CommandLineUtils;
using WasmRunner;

var commands = new ConsoleCommand[]
{
    new RunCommand(),
};

return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
