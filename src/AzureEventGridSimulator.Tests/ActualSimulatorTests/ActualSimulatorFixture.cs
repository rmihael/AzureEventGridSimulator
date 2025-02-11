﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AzureEventGridSimulator.Tests.ActualSimulatorTests;

public class ActualSimulatorFixture : IDisposable, IAsyncLifetime
{
    private const string SimulatorFileName = "AzureEventGridSimulator";
    private bool _disposed;
    private string _simulatorExePath;

    private Process _simulatorProcess;

    public async Task InitializeAsync()
    {
        var simulatorDirectory = Directory.GetCurrentDirectory();
        var executable = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
        _simulatorExePath = Path.Combine(simulatorDirectory, executable);

        KillExistingSimulators();

        _simulatorProcess = Process.Start(new ProcessStartInfo(_simulatorExePath)
        {
            WorkingDirectory = simulatorDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            Environment = { new KeyValuePair<string, string>("ASPNETCORE_ENVIRONMENT", "Test") }
        });

        await Task.Delay(1000);
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_simulatorProcess?.HasExited == false)
            {
                _simulatorProcess?.Kill(true);
                _simulatorProcess?.WaitForExit();
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private void KillExistingSimulators()
    {
        try
        {
            // Kill any existing instances of the test simulator that may still be hanging around.
            // Note: there shouldn't be any unless something went wrong and the test runner didn't exit cleanly.
            var simulatorProcesses = Process.GetProcesses()
                                            .Where(o => o.ProcessName == SimulatorFileName)
                                            .Where(o => string.Equals(o.MainModule?.FileName, _simulatorExePath, StringComparison.OrdinalIgnoreCase))
                                            .ToArray();

            foreach (var process in simulatorProcesses)
            {
                process.Kill();
            }
        }
        catch
        {
            //
        }
    }

    ~ActualSimulatorFixture()
    {
        Dispose();
    }
}
