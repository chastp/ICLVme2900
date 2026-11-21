namespace Vme2900.Core;

public sealed class VmeKernel
{
    private readonly CommandRegistry _commands;
    private readonly CommandContext _context;

    public VmeKernel(string diskPath, TextWriter output)
    {
        _commands = CommandRegistry.CreateDefault();
        _context = new CommandContext(new VmeSession(),
            new DirectoryVmeFileSystem(diskPath), new InMemoryJobService(), output);
    }

    public VmeSession Session => _context.Session;

    public async Task<CommandResult> ExecuteLineAsync(string line, CancellationToken cancellationToken = default)
    {
        ParsedCommand parsed;
        try { parsed = CommandParser.Parse(line); }
        catch (FormatException ex) { return CommandResult.Error(ex.Message); }
        if (parsed.Name.Length == 0) return CommandResult.Ok();

        var command = _commands.Find(parsed.Name);
        if (command is null) return CommandResult.Error($"Unknown command '{parsed.Name}'. Type HELP.");
        if (command.RequiresLogon && !_context.Session.IsLoggedOn)
            return CommandResult.Error("Please LOGON before issuing this command.");

        try
        {
            return await command.ExecuteAsync(_context, parsed.Arguments, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return CommandResult.Error(ex.Message);
        }
    }
}
