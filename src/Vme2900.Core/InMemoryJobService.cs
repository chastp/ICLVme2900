namespace Vme2900.Core;

public sealed class InMemoryJobService : IVmeJobService
{
    private readonly List<VmeJob> _jobs = [];
    private long _nextId = 1000;

    public VmeJob Submit(string userId, string sourceFile)
    {
        var job = new VmeJob
        {
            Id = Interlocked.Increment(ref _nextId),
            UserId = userId,
            SourceFile = sourceFile,
            SubmittedUtc = DateTime.UtcNow,
            Status = "QUEUED"
        };
        _jobs.Add(job);
        return job;
    }

    public IReadOnlyList<VmeJob> List() => _jobs.OrderByDescending(j => j.Id).ToArray();

    public bool Cancel(long jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == jobId && j.Status == "QUEUED");
        if (job is null) return false;
        job.Status = "CANCELLED";
        return true;
    }
}
