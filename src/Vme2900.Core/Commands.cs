namespace Vme2900.Core;

internal abstract class VmeCommand : IVmeCommand
{
    public abstract string Name { get; }
    public abstract string Summary { get; }
    public virtual bool RequiresLogon => true;
    public abstract Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a,
        CancellationToken cancellationToken = default);
    protected static CommandResult Usage(string text) => CommandResult.Error("Usage: " + text);
}

internal sealed class HelpCommand(CommandRegistry registry) : VmeCommand
{
    public override string Name => "HELP";
    public override string Summary => "Display available commands or help for one command.";
    public override bool RequiresLogon => false;
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        var commands = registry.Commands;
        if (a.Count == 1)
        {
            var cmd = registry.Find(a[0]);
            return Task.FromResult(cmd is null ? CommandResult.Error($"No help available for {a[0]}.")
                : CommandResult.Ok($"{cmd.Name} - {cmd.Summary}"));
        }
        return Task.FromResult(CommandResult.Ok(string.Join(Environment.NewLine,
            commands.OrderBy(x => x.Name).Select(x => $"{x.Name,-10} {x.Summary}"))));
    }
}

internal sealed class LogonCommand : VmeCommand
{
    public override string Name => "LOGON";
    public override string Summary => "Start a simulated user session: LOGON <user>.";
    public override bool RequiresLogon => false;
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 1) return Task.FromResult(Usage("LOGON <user>"));
        c.Session.Logon(a[0]);
        return Task.FromResult(CommandResult.Ok($"Welcome {c.Session.User!.DisplayName}."));
    }
}

internal sealed class LogoffCommand : VmeCommand
{
    public override string Name => "LOGOFF";
    public override string Summary => "End the current user session.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    { c.Session.Logoff(); return Task.FromResult(CommandResult.Ok("Logged off.")); }
}

internal sealed class WhoAmICommand : VmeCommand
{
    public override string Name => "WHOAMI";
    public override string Summary => "Display the logged-on user.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
        => Task.FromResult(CommandResult.Ok(c.Session.User!.UserId));
}

internal sealed class DateTimeCommand : VmeCommand
{
    public override string Name => "DATE";
    public override string Summary => "Display the system date and time.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
        => Task.FromResult(CommandResult.Ok(DateTime.Now.ToString("ddd dd MMM yyyy HH:mm:ss")));
}

internal sealed class VersionCommand : VmeCommand
{
    public override string Name => "VERSION";
    public override string Summary => "Display emulator version and emulation scope.";
    public override bool RequiresLogon => false;
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
        => Task.FromResult(CommandResult.Ok("ICL 2900 VME command emulator 0.1 (simulation; not original VME)"));
}

internal sealed class PwdCommand : VmeCommand
{
    public override string Name => "PWD";
    public override string Summary => "Display current virtual directory.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
        => Task.FromResult(CommandResult.Ok(c.Session.CurrentDirectory));
}

internal sealed class CdCommand : VmeCommand
{
    public override string Name => "CD";
    public override string Summary => "Change virtual directory: CD <path>.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 1) return Task.FromResult(Usage("CD <path>"));
        var p = c.Path(a[0]);
        if (!c.FileSystem.DirectoryExists(p)) return Task.FromResult(CommandResult.Error("Directory not found."));
        c.Session.CurrentDirectory = p;
        return Task.FromResult(CommandResult.Ok(p));
    }
}

internal sealed class DirCommand : VmeCommand
{
    public override string Name => "DIR";
    public override string Summary => "List a virtual directory: DIR [path].";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count > 1) return Task.FromResult(Usage("DIR [path]"));
        var p = c.Path(a.Count == 0 ? "." : a[0]);
        try
        {
            var rows = c.FileSystem.List(p).Select(x =>
                $"{(x.IsDirectory ? "<DIR>" : x.Length.ToString().PadLeft(8))}  {x.Name}");
            return Task.FromResult(CommandResult.Ok(string.Join(Environment.NewLine, rows.DefaultIfEmpty("(empty)"))));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return Task.FromResult(CommandResult.Error(ex.Message)); }
    }
}

internal sealed class TypeCommand : VmeCommand
{
    public override string Name => "TYPE";
    public override string Summary => "Display a text file: TYPE <file>.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 1) return Task.FromResult(Usage("TYPE <file>"));
        var p = c.Path(a[0]);
        return Task.FromResult(c.FileSystem.FileExists(p)
            ? CommandResult.Ok(c.FileSystem.ReadAllText(p))
            : CommandResult.Error("File not found."));
    }
}

internal sealed class CreateCommand : VmeCommand
{
    public override string Name => "CREATE";
    public override string Summary => "Create a text file; finish input with a line containing a single dot.";
    public override async Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 1) return Usage("CREATE <file>");
        var lines = new List<string>();
        await c.Output.WriteLineAsync("Enter text; a single '.' ends input.");
        while (true)
        {
            var line = await Console.In.ReadLineAsync(t);
            if (line is null || line == ".") break;
            lines.Add(line);
        }
        c.FileSystem.WriteAllText(c.Path(a[0]), string.Join(Environment.NewLine, lines));
        return CommandResult.Ok("File created.");
    }
}

internal sealed class DeleteCommand : VmeCommand
{
    public override string Name => "DELETE";
    public override string Summary => "Delete a file: DELETE <file>.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 1) return Task.FromResult(Usage("DELETE <file>"));
        var p = c.Path(a[0]);
        if (!c.FileSystem.FileExists(p)) return Task.FromResult(CommandResult.Error("File not found."));
        c.FileSystem.DeleteFile(p);
        return Task.FromResult(CommandResult.Ok("File deleted."));
    }
}

internal sealed class CopyCommand : VmeCommand
{
    public override string Name => "COPY";
    public override string Summary => "Copy a file: COPY <source> <destination>.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 2) return Task.FromResult(Usage("COPY <source> <destination>"));
        try { c.FileSystem.CopyFile(c.Path(a[0]), c.Path(a[1])); return Task.FromResult(CommandResult.Ok("File copied.")); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return Task.FromResult(CommandResult.Error(ex.Message)); }
    }
}

internal sealed class RenameCommand : VmeCommand
{
    public override string Name => "RENAME";
    public override string Summary => "Rename or move a file: RENAME <source> <destination>.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 2) return Task.FromResult(Usage("RENAME <source> <destination>"));
        try { c.FileSystem.MoveFile(c.Path(a[0]), c.Path(a[1])); return Task.FromResult(CommandResult.Ok("File renamed.")); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return Task.FromResult(CommandResult.Error(ex.Message)); }
    }
}

internal sealed class MkdirCommand : VmeCommand
{
    public override string Name => "MKDIR";
    public override string Summary => "Create a directory: MKDIR <path>.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 1) return Task.FromResult(Usage("MKDIR <path>"));
        c.FileSystem.CreateDirectory(c.Path(a[0]));
        return Task.FromResult(CommandResult.Ok("Directory created."));
    }
}

internal sealed class RmdirCommand : VmeCommand
{
    public override string Name => "RMDIR";
    public override string Summary => "Remove an empty directory: RMDIR <path>.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 1) return Task.FromResult(Usage("RMDIR <path>"));
        try { c.FileSystem.DeleteDirectory(c.Path(a[0])); return Task.FromResult(CommandResult.Ok("Directory removed.")); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return Task.FromResult(CommandResult.Error(ex.Message)); }
    }
}

internal sealed class SubmitCommand : VmeCommand
{
    public override string Name => "SUBMIT";
    public override string Summary => "Place a source file on the simulated batch queue: SUBMIT <file>.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 1) return Task.FromResult(Usage("SUBMIT <file>"));
        var p = c.Path(a[0]);
        if (!c.FileSystem.FileExists(p)) return Task.FromResult(CommandResult.Error("Source file not found."));
        var job = c.Jobs.Submit(c.Session.User!.UserId, p);
        return Task.FromResult(CommandResult.Ok($"Job {job.Id} queued."));
    }
}

internal sealed class JobsCommand : VmeCommand
{
    public override string Name => "JOBS";
    public override string Summary => "List simulated batch jobs.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        var rows = c.Jobs.List().Select(j => $"{j.Id,6}  {j.Status,-10} {j.UserId,-12} {j.SourceFile}");
        return Task.FromResult(CommandResult.Ok(string.Join(Environment.NewLine, rows.DefaultIfEmpty("No jobs."))));
    }
}

internal sealed class CancelCommand : VmeCommand
{
    public override string Name => "CANCEL";
    public override string Summary => "Cancel a queued job: CANCEL <job-id>.";
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
    {
        if (a.Count != 1 || !long.TryParse(a[0], out var id)) return Task.FromResult(Usage("CANCEL <job-id>"));
        return Task.FromResult(c.Jobs.Cancel(id) ? CommandResult.Ok("Job cancelled.")
            : CommandResult.Error("Queued job not found."));
    }
}

internal sealed class ExitCommand : VmeCommand
{
    public override string Name => "EXIT";
    public override string Summary => "Exit the emulator.";
    public override bool RequiresLogon => false;
    public override Task<CommandResult> ExecuteAsync(CommandContext c, IReadOnlyList<string> a, CancellationToken t)
        => Task.FromResult(CommandResult.Exit());
}
