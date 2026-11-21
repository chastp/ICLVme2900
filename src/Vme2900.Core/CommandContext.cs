namespace Vme2900.Core;

public sealed class CommandContext
{
    public VmeSession Session { get; }
    public IVmeFileSystem FileSystem { get; }
    public IVmeJobService Jobs { get; }
    public TextWriter Output { get; }

    public CommandContext(VmeSession session, IVmeFileSystem fileSystem,
        IVmeJobService jobs, TextWriter output)
    {
        Session = session;
        FileSystem = fileSystem;
        Jobs = jobs;
        Output = output;
    }

    public string Path(string value) => FileSystem.Normalize(Session.CurrentDirectory, value);
}
