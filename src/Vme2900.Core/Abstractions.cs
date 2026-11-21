namespace Vme2900.Core;

public sealed record VmeUser(string UserId, string DisplayName);

public sealed class VmeSession
{
    public VmeUser? User { get; private set; }
    public string CurrentDirectory { get; set; } = "/";
    public bool IsLoggedOn => User is not null;

    public void Logon(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A user identifier is required.", nameof(userId));

        User = new VmeUser(userId.Trim().ToUpperInvariant(), userId.Trim());
        CurrentDirectory = "/";
    }

    public void Logoff()
    {
        User = null;
        CurrentDirectory = "/";
    }
}

public sealed record CommandResult(bool Success, string Message = "", bool ExitRequested = false)
{
    public static CommandResult Ok(string message = "") => new(true, message);
    public static CommandResult Error(string message) => new(false, message);
    public static CommandResult Exit() => new(true, "Session ended.", true);
}

public sealed record ParsedCommand(string Name, IReadOnlyList<string> Arguments);

public interface IVmeCommand
{
    string Name { get; }
    string Summary { get; }
    bool RequiresLogon { get; }
    Task<CommandResult> ExecuteAsync(CommandContext context, IReadOnlyList<string> args,
        CancellationToken cancellationToken = default);
}

public interface IVmeFileSystem
{
    IEnumerable<VmeFileEntry> List(string virtualPath);
    bool FileExists(string virtualPath);
    bool DirectoryExists(string virtualPath);
    string ReadAllText(string virtualPath);
    void WriteAllText(string virtualPath, string content);
    void DeleteFile(string virtualPath);
    void CopyFile(string source, string destination, bool overwrite = false);
    void MoveFile(string source, string destination);
    void CreateDirectory(string virtualPath);
    void DeleteDirectory(string virtualPath);
    string Normalize(string currentDirectory, string path);
}

public sealed record VmeFileEntry(string Name, bool IsDirectory, long Length, DateTime LastWriteTimeUtc);

public interface IVmeJobService
{
    VmeJob Submit(string userId, string sourceFile);
    IReadOnlyList<VmeJob> List();
    bool Cancel(long jobId);
}

public sealed class VmeJob
{
    public long Id { get; init; }
    public string UserId { get; init; } = "";
    public string SourceFile { get; init; } = "";
    public DateTime SubmittedUtc { get; init; }
    public string Status { get; set; } = "QUEUED";
}
