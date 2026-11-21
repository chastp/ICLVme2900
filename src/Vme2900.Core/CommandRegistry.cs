namespace Vme2900.Core;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, IVmeCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<IVmeCommand> Commands => _commands.Values.ToArray();

    public void Register(IVmeCommand command) => _commands[command.Name] = command;
    public IVmeCommand? Find(string name) => _commands.GetValueOrDefault(name);

    public static CommandRegistry CreateDefault()
    {
        var r = new CommandRegistry();
        r.Register(new LogonCommand());
        r.Register(new LogoffCommand());
        r.Register(new WhoAmICommand());
        r.Register(new DateTimeCommand());
        r.Register(new VersionCommand());
        r.Register(new PwdCommand());
        r.Register(new CdCommand());
        r.Register(new DirCommand());
        r.Register(new TypeCommand());
        r.Register(new CreateCommand());
        r.Register(new DeleteCommand());
        r.Register(new CopyCommand());
        r.Register(new RenameCommand());
        r.Register(new MkdirCommand());
        r.Register(new RmdirCommand());
        r.Register(new SubmitCommand());
        r.Register(new JobsCommand());
        r.Register(new CancelCommand());
        r.Register(new ExitCommand());
        r.Register(new HelpCommand(r));
        return r;
    }
}
