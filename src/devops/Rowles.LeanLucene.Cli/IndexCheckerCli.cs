using System.Text.Json;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Cli;

/// <summary>
/// Implements the LeanLucene command-line checker.
/// </summary>
public static class IndexCheckerCli
{
    /// <summary>
    /// Runs the command-line checker.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="output">Standard output writer.</param>
    /// <param name="error">Standard error writer.</param>
    /// <returns>Exit code: 0 for healthy, 1 for validation errors, 2 for invalid arguments or CLI failures.</returns>
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                WriteHelp(output);
                return 0;
            }

            if (!string.Equals(args[0], "check", StringComparison.OrdinalIgnoreCase))
            {
                error.WriteLine($"Unknown command '{args[0]}'.");
                WriteHelp(error);
                return 2;
            }

            if (args.Length == 2 && IsHelp(args[1]))
            {
                WriteHelp(output);
                return 0;
            }

            if (!TryParseCheckArguments(args, error, out var indexPath, out var options, out bool json))
                return 2;

            using var directory = new MMapDirectory(indexPath);
            var result = IndexValidator.Check(directory, options);
            if (json)
                WriteJson(output, result);
            else
                WriteText(output, result);

            return result.IsHealthy ? 0 : 1;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static bool TryParseCheckArguments(
        string[] args,
        TextWriter error,
        out string indexPath,
        out IndexCheckOptions options,
        out bool json)
    {
        indexPath = string.Empty;
        options = new IndexCheckOptions();
        json = false;

        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (IsHelp(arg))
            {
                error.WriteLine("Help must be requested as 'leanlucene check --help'.");
                return false;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                switch (arg)
                {
                    case "--deep":
                        options.Deep = true;
                        break;
                    case "--json":
                        json = true;
                        break;
                    case "--postings":
                        options.VerifyPostings = true;
                        break;
                    case "--stored-fields":
                        options.VerifyStoredFields = true;
                        break;
                    case "--doc-values":
                        options.VerifyDocValues = true;
                        break;
                    case "--vectors":
                        options.VerifyVectors = true;
                        break;
                    case "--hnsw":
                        options.VerifyHnsw = true;
                        break;
                    case "--live-docs":
                        options.VerifyLiveDocs = true;
                        break;
                    default:
                        error.WriteLine($"Unknown option '{arg}'.");
                        return false;
                }
                continue;
            }

            if (!string.IsNullOrEmpty(indexPath))
            {
                error.WriteLine("Only one index path can be supplied.");
                return false;
            }

            indexPath = arg;
        }

        if (string.IsNullOrWhiteSpace(indexPath))
        {
            error.WriteLine("Missing index path.");
            return false;
        }

        if (!Directory.Exists(indexPath))
        {
            error.WriteLine($"Index path '{indexPath}' does not exist.");
            return false;
        }

        return true;
    }

    private static bool IsHelp(string arg)
        => string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase);

    private static void WriteHelp(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  leanlucene check <index-path> [--deep] [--json] [--postings] [--stored-fields] [--doc-values] [--vectors] [--hnsw] [--live-docs]");
    }

    private static void WriteText(TextWriter writer, IndexCheckResult result)
    {
        writer.WriteLine(result.IsHealthy
            ? $"Healthy: checked {result.SegmentsChecked} segment(s), {result.DocumentsChecked} document(s), {result.FilesChecked} file(s)."
            : $"Unhealthy: checked {result.SegmentsChecked} segment(s), {result.DocumentsChecked} document(s), {result.FilesChecked} file(s).");

        foreach (var issue in result.DetailedIssues)
            writer.WriteLine($"{issue.Severity} {issue.Code} {issue.SegmentId ?? "-"} {issue.FileName ?? "-"} {issue.Message}");
    }

    private static void WriteJson(TextWriter writer, IndexCheckResult result)
    {
        var dto = CliIndexCheckResultDto.FromResult(result);
        var json = JsonSerializer.Serialize(dto, IndexCheckCliJsonContext.Default.CliIndexCheckResultDto);
        writer.WriteLine(json);
    }
}

internal sealed class CliIndexCheckResultDto
{
    public required bool IsHealthy { get; init; }
    public int? CommitGeneration { get; init; }
    public required int SegmentsChecked { get; init; }
    public required int DocumentsChecked { get; init; }
    public required int FilesChecked { get; init; }
    public required List<CliIndexCheckIssueDto> Issues { get; init; }

    public static CliIndexCheckResultDto FromResult(IndexCheckResult result)
    {
        var issues = new List<CliIndexCheckIssueDto>(result.DetailedIssues.Count);
        foreach (var issue in result.DetailedIssues)
            issues.Add(CliIndexCheckIssueDto.FromIssue(issue));

        return new CliIndexCheckResultDto
        {
            IsHealthy = result.IsHealthy,
            CommitGeneration = result.CommitGeneration,
            SegmentsChecked = result.SegmentsChecked,
            DocumentsChecked = result.DocumentsChecked,
            FilesChecked = result.FilesChecked,
            Issues = issues
        };
    }
}

internal sealed class CliIndexCheckIssueDto
{
    public required string Severity { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? FileName { get; init; }
    public string? SegmentId { get; init; }
    public required bool IsRepairable { get; init; }

    public static CliIndexCheckIssueDto FromIssue(IndexCheckIssue issue)
        => new()
        {
            Severity = issue.Severity.ToString(),
            Code = issue.Code,
            Message = issue.Message,
            FileName = issue.FileName,
            SegmentId = issue.SegmentId,
            IsRepairable = issue.IsRepairable
        };
}
