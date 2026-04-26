#:package Terminal.Gui@1.17.0

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace LeanLuceneTool;

public static class Program
{
    static TextView outputView;
    static Label statusLabel;

    static void Main()
    {
        Application.Init();
        var top = Application.Top;
        var win = new Window("Build / Test / Benchmark Console")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Output text view (scrollable)
        outputView = new TextView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(4),
            ReadOnly = true,
            AllowsTab = false
        };

        // Status label
        statusLabel = new Label("Ready")
        {
            X = 0,
            Y = Pos.Bottom(outputView),
            Width = Dim.Fill(),
            Height = 1
        };

        var btnBuild = new Button(" Build ")
        {
            X = 1,
            Y = 0,
            AutoSize = true
        };
        var btnTest = new Button(" Test ")
        {
            X = Pos.Right(btnBuild) + 2,
            Y = 0,
            AutoSize = true
        };
        var btnBench = new Button(" Benchmark ")
        {
            X = Pos.Right(btnTest) + 2,
            Y = 0,
            AutoSize = true
        };
        var btnQuit = new Button(" Quit ")
        {
            X = Pos.Right(btnBench) + 4,
            Y = 0,
            AutoSize = true
        };

        var buttonBar = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };
        buttonBar.Add(btnBuild, btnTest, btnBench, btnQuit);

        win.Add(buttonBar, outputView, statusLabel);
        top.Add(win);

        // Wire events
        btnBuild.Clicked += RunBuild;
        btnTest.Clicked += RunTest;
        btnBench.Clicked += RunBenchmark;
        btnQuit.Clicked += () => Application.RequestStop();

        Application.Run();
        Application.Shutdown();
    }

    static async void RunBuild()
    {
        SetStatus("Building...");
        AppendOutput($">>> [BUILD] Starting at {DateTime.Now:T}");
        var (ok, output) = await RunCommandAsync("dotnet", "build");
        AppendOutput(output);
        AppendOutput(ok ? ">>> Build succeeded." : ">>> Build FAILED.");
        SetStatus(ok ? "Build completed." : "Build failed.");
    }

    static async void RunTest()
    {
        SetStatus("Running tests...");
        AppendOutput($">>> [TEST] Starting at {DateTime.Now:T}");
        var (ok, output) = await RunCommandAsync("dotnet", "test");
        AppendOutput(output);
        AppendOutput(ok ? ">>> Tests passed." : ">>> Tests FAILED.");
        SetStatus(ok ? "Tests completed." : "Tests failed.");
    }

    static async void RunBenchmark()
    {
        SetStatus("Running benchmark (Release mode)...");
        AppendOutput($">>> [BENCHMARK] Starting at {DateTime.Now:T}");
        var sw = Stopwatch.StartNew();
        var (ok, output) = await RunCommandAsync("dotnet", "run -c Release");
        sw.Stop();
        AppendOutput(output);
        AppendOutput($">>> Benchmark finished in {sw.Elapsed.TotalSeconds:F2} seconds.");
        AppendOutput(ok ? ">>> Benchmark completed." : ">>> Benchmark failed.");
        SetStatus(ok ? "Benchmark done." : "Benchmark failed.");
    }

    static void AppendOutput(string text)
    {
        Application.MainLoop.Invoke(() =>
        {
            outputView.Text += text + Environment.NewLine;
            outputView.MoveEnd();
        });
    }

    static void SetStatus(string text)
    {
        Application.MainLoop.Invoke(() => statusLabel.Text = text);
    }

    static async Task<(bool success, string output)> RunCommandAsync(string command, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(command, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    output.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    error.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            bool success = process.ExitCode == 0;
            string fullOutput = output.ToString();
            if (error.Length > 0)
                fullOutput += Environment.NewLine + "ERRORS:" + Environment.NewLine + error.ToString();

            return (success, fullOutput);
        }
        catch (Exception ex)
        {
            return (false, $"Exception: {ex.Message}");
        }
    }
}