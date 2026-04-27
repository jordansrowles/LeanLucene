using Cake.Frosting;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Test;

/*
dotnet run -- --target Default
dotnet run -- --target Build
dotnet run -- --target Test --configuration Debug
dotnet run --project build/Build.csproj -- --target Default
*/

public sealed class BuildContext : FrostingContext
{
    public BuildContext(ICakeContext context) : base(context)
    {
    }

    public string Configuration =>
        Arguments.HasArgument("configuration")
            ? Arguments.GetArgument("configuration")
            : "Release";

    public string Solution => "../Rowles.LeanLucene.sln";
    public string Artifacts => "../artifacts";
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (Directory.Exists(context.Artifacts))
            Directory.Delete(context.Artifacts, true);

        Directory.CreateDirectory(context.Artifacts);
        context.CleanDirectories("**/bin");
        context.CleanDirectories("**/obj");
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(CleanTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetBuild(context.Solution, new()
        {
            Configuration = context.Configuration
        });
    }
}

[TaskName("Test")]
[IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetTest(context.Solution, new()
        {
            Configuration = context.Configuration,
            NoBuild = true
        });
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(TestTask))]
public sealed class DefaultTask : FrostingTask
{
}