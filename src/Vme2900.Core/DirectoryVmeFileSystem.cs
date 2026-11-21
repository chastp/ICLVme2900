namespace Vme2900.Core;

/// <summary>
/// Development virtual disk backed by a host directory. All resolved paths are checked
/// to remain beneath RootPath; this is not a historical VME disk format.
/// </summary>
public sealed class DirectoryVmeFileSystem : IVmeFileSystem
{
    public string RootPath { get; }

    public DirectoryVmeFileSystem(string rootPath)
    {
        RootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(RootPath);
    }

    public string Normalize(string currentDirectory, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) path = currentDirectory;
        string combined = path.StartsWith('/') || path.StartsWith('\\')
            ? path.TrimStart('/', '\\')
            : $"{currentDirectory.Trim('/')}/{path}";
        var parts = new List<string>();
        foreach (var part in combined.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".") continue;
            if (part == "..") { if (parts.Count > 0) parts.RemoveAt(parts.Count - 1); continue; }
            parts.Add(part);
        }
        return "/" + string.Join('/', parts);
    }

    private string Resolve(string virtualPath)
    {
        var normalized = Normalize("/", virtualPath).TrimStart('/');
        var full = Path.GetFullPath(Path.Combine(RootPath, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = RootPath.EndsWith(Path.DirectorySeparatorChar)
            ? RootPath : RootPath + Path.DirectorySeparatorChar;
        if (!full.Equals(RootPath, StringComparison.OrdinalIgnoreCase) &&
            !full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path escapes the virtual disk.");
        return full;
    }

    public IEnumerable<VmeFileEntry> List(string virtualPath)
    {
        var path = Resolve(virtualPath);
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException(virtualPath);
        var dirs = Directory.EnumerateDirectories(path).Select(d =>
        {
            var i = new DirectoryInfo(d);
            return new VmeFileEntry(i.Name, true, 0, i.LastWriteTimeUtc);
        });
        var files = Directory.EnumerateFiles(path).Select(f =>
        {
            var i = new FileInfo(f);
            return new VmeFileEntry(i.Name, false, i.Length, i.LastWriteTimeUtc);
        });
        return dirs.Concat(files).OrderByDescending(x => x.IsDirectory).ThenBy(x => x.Name);
    }

    public bool FileExists(string p) => File.Exists(Resolve(p));
    public bool DirectoryExists(string p) => Directory.Exists(Resolve(p));
    public string ReadAllText(string p) => File.ReadAllText(Resolve(p));
    public void WriteAllText(string p, string c)
    {
        var full = Resolve(p);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, c);
    }
    public void DeleteFile(string p) => File.Delete(Resolve(p));
    public void CopyFile(string s, string d, bool overwrite = false)
    {
        var dest = Resolve(d);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(Resolve(s), dest, overwrite);
    }
    public void MoveFile(string s, string d)
    {
        var dest = Resolve(d);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Move(Resolve(s), dest);
    }
    public void CreateDirectory(string p) => Directory.CreateDirectory(Resolve(p));
    public void DeleteDirectory(string p) => Directory.Delete(Resolve(p), recursive: false);
}
