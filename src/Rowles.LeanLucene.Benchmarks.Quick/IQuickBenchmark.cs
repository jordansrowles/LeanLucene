namespace Rowles.LeanLucene.Benchmarks.Quick;

/// <summary>
/// Defines a single quick sanity benchmark. Implementations provide
/// setup/cleanup lifecycle hooks and the measured action.
/// </summary>
internal interface IQuickBenchmark
{
    /// <summary>Display name for the benchmark (appears in reports).</summary>
    string Name { get; }

    /// <summary>One-time initialisation before measurement begins.</summary>
    void Setup();

    /// <summary>The action under measurement. Called once per iteration.</summary>
    void Run();

    /// <summary>Teardown after all iterations complete.</summary>
    void Cleanup();
}
