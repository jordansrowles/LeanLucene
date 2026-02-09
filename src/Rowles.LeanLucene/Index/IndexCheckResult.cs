using System.Text.Json;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index;

/// <summary>
/// Result of an <see cref="IndexValidator.Validate"/> call.
/// </summary>
public sealed class IndexCheckResult
{
    public bool IsHealthy => Issues.Count == 0;
    public IReadOnlyList<string> Issues => _issues;
    public int SegmentsChecked { get; internal set; }
    public int DocumentsChecked { get; internal set; }

    private readonly List<string> _issues = [];
    internal void AddIssue(string issue) => _issues.Add(issue);
}
