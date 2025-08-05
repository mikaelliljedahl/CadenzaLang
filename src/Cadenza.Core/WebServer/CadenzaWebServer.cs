// Cadenza Core Compiler - Embedded Web Server
// Main server lifecycle management and orchestration

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace Cadenza.Core;

// =============================================================================
// EMBEDDED WEB SERVER
// =============================================================================

/// <summary>
/// Embedded Kestrel web server for serving Cadenza components as Blazor applications
/// </summary>
public class CadenzaWebServer
{
    private readonly CadenzaWebServerOptions _options;
    private readonly BlazorProjectGenerator _projectGenerator;
    private string? _currentProjectDir;
    private System.Diagnostics.Process? _currentProcess;
    private volatile bool _isShuttingDown = false;
    
    public CadenzaWebServer(CadenzaWebServerOptions options)
    {
        _options = options;
        _projectGenerator = new BlazorProjectGenerator();
        
        // Setup cleanup on process termination
        Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true; // Prevent immediate termination
            Console.WriteLine("\n🛑 Shutting down server...");
            _isShuttingDown = true;
            TerminateBlazorProcess();
            CleanupTempDirectory();
            Environment.Exit(0);
        };
    }

    /// <summary>
    /// Determines if the input file is a project configuration or single component
    /// </summary>
    private bool IsProjectConfig(string inputFile)
    {
        return inputFile.EndsWith("cadenzac.json");
    }

    /// <summary>
    /// Parses a cadenzac.json project configuration file
    /// </summary>
    private async Task<UIProjectConfig> ParseProjectConfigAsync(string configPath)
    {
        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<UIProjectConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return config ?? throw new InvalidOperationException("Failed to deserialize project configuration");
    }

    /// <summary>
    /// Discovers component files in the project source directory
    /// </summary>
    private Task<List<string>> DiscoverComponentFilesAsync(string projectDir, string sourceDir)
    {
        var fullSourcePath = Path.Combine(projectDir, sourceDir);
        
        if (!Directory.Exists(fullSourcePath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {fullSourcePath}");
        }

        var componentFiles = Directory.GetFiles(fullSourcePath, "*.cdz", SearchOption.AllDirectories)
                                     .OrderBy(Path.GetFileName)
                                     .ToList();

        Console.WriteLine($"📁 Discovered {componentFiles.Count} component files in {sourceDir}");
        foreach (var file in componentFiles)
        {
            Console.WriteLine($"   - {Path.GetRelativePath(projectDir, file)}");
        }

        return Task.FromResult(componentFiles);
    }

    /// <summary>
    /// Starts the Blazor web server by launching the generated project as a subprocess
    /// </summary>
    public async Task StartAsync()
    {
        Console.WriteLine($"🌐 Starting Cadenza web server on port {_options.Port}...");
        
        // Create a debug directory for easier inspection of generated files
        _currentProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "debug", $"cadenza-web-{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(_currentProjectDir);
        var tempProjectDir = _currentProjectDir;
        
        try
        {
            // Check if input is a project configuration or single file
            if (IsProjectConfig(_options.InputFile))
            {
                // Handle project-based serving
                await HandleProjectServing(tempProjectDir);
            }
            else
            {
                // Handle single file serving (backward compatibility)
                await _projectGenerator.GenerateBlazorProjectAsync(_options.InputFile, tempProjectDir);
            }
            
            // Build the generated project
            await BuildGeneratedProjectAsync(tempProjectDir);
            
            // Launch the generated project as a subprocess
            await LaunchBlazorProjectAsync(tempProjectDir);
        }
        finally
        {
            // Ensure process is terminated and clean up temporary directory
            TerminateBlazorProcess();
            CleanupTempDirectory();
        }
    }

    /// <summary>
    /// Handles project-based serving with cadenzac.json configuration
    /// </summary>
    private async Task HandleProjectServing(string tempProjectDir)
    {
        var configPath = _options.InputFile;
        var projectDir = Path.GetDirectoryName(configPath) ?? throw new InvalidOperationException("Invalid config path");
        
        Console.WriteLine($"📋 Loading project configuration: {Path.GetFileName(configPath)}");
        
        // Parse project configuration
        var config = await ParseProjectConfigAsync(configPath);
        
        Console.WriteLine($"📦 Project: {config.Name} v{config.Version}");
        if (!string.IsNullOrEmpty(config.Description))
        {
            Console.WriteLine($"   {config.Description}");
        }
        
        // Discover component files
        var componentFiles = await DiscoverComponentFilesAsync(projectDir, config.Build.Source);
        
        if (componentFiles.Count == 0)
        {
            throw new InvalidOperationException($"No component files found in {config.Build.Source}");
        }
        
        // Generate Blazor project from multiple components
        await _projectGenerator.GenerateBlazorProjectFromFilesAsync(componentFiles, tempProjectDir, config);
    }

    /// <summary>
    /// Terminates the running Blazor process gracefully with fallback to force kill
    /// </summary>
    private void TerminateBlazorProcess()
    {
        if (_currentProcess != null)
        {
            int processId = -1;
            try
            {
                // Capture process ID before potential disposal
                if (!_currentProcess.HasExited)
                {
                    processId = _currentProcess.Id;
                    Console.WriteLine($"🔄 Terminating Blazor process (PID: {processId})...");
                }
                else
                {
                    Console.WriteLine("✅ Blazor process already exited");
                    return;
                }
                
                // First attempt: Send SIGTERM (graceful shutdown signal)
                try
                {
                    _currentProcess.CloseMainWindow();
                    if (_currentProcess.WaitForExit(2000))
                    {
                        Console.WriteLine("✅ Blazor process terminated gracefully");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🔄 Graceful termination failed: {ex.Message}");
                }
                
                // Second attempt: Force kill the main process
                Console.WriteLine("⚡ Force killing Blazor process...");
                try
                {
                    _currentProcess.Kill(entireProcessTree: true); // Kill entire process tree
                    if (_currentProcess.WaitForExit(3000))
                    {
                        Console.WriteLine("✅ Blazor process force-killed successfully");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Force kill failed: {ex.Message}");
                }
                
                // Third attempt: Kill by process name (for Windows/WSL compatibility)
                Console.WriteLine("🔧 Attempting to kill dotnet processes...");
                try
                {
                    var processName = "dotnet";
                    var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                    foreach (var proc in processes)
                    {
                        try
                        {
                            // Only kill processes that are likely our Blazor app
                            if (proc.ProcessName == processName && 
                                proc.StartTime > DateTime.Now.AddMinutes(-5)) // Started recently
                            {
                                proc.Kill();
                                Console.WriteLine($"🔧 Killed dotnet process (PID: {proc.Id})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"🔧 Could not kill process {proc.Id}: {ex.Message}");
                        }
                    }
                    
                    // Give time for file handles to be released
                    Thread.Sleep(1000);
                    Console.WriteLine("✅ Process cleanup completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Process cleanup failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Warning: Error terminating Blazor process: {ex.Message}");
            }
            finally
            {
                _currentProcess?.Dispose();
                _currentProcess = null;
                
                // Additional delay to ensure file handles are released in Windows/WSL
                Console.WriteLine("⏳ Waiting for file handles to be released...");
                Thread.Sleep(2000);
            }
        }
    }

    /// <summary>
    /// Cleans up the temporary project directory with retry logic and selective cleanup
    /// </summary>
    private void CleanupTempDirectory()
    {
        if (!string.IsNullOrEmpty(_currentProjectDir) && Directory.Exists(_currentProjectDir))
        {
            const int maxRetries = 5;
            const int baseDelayMs = 500;
            bool showVerboseOutput = false; // Only show verbose output if needed
            
            Console.WriteLine($"🧹 Cleaning up temporary directory...");
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // First attempt: try to clear build output directories specifically
                    if (attempt == 1)
                    {
                        CleanupBuildOutputDirectories(_currentProjectDir, showVerboseOutput);
                    }
                    
                    // Now try to delete the entire directory
                    Directory.Delete(_currentProjectDir, true);
                    _currentProjectDir = null;
                    Console.WriteLine("✅ Temporary directory cleaned up successfully");
                    return; // Success!
                }
                catch (UnauthorizedAccessException) when (attempt < maxRetries)
                {
                    if (attempt == 1)
                    {
                        Console.WriteLine($"⏳ Some files are locked, retrying cleanup...");
                        showVerboseOutput = true; // Show details on subsequent attempts
                    }
                    Thread.Sleep(baseDelayMs * attempt); // Exponential backoff
                }
                catch (IOException) when (attempt < maxRetries)
                {
                    if (attempt == 1)
                    {
                        Console.WriteLine($"⏳ Directory in use, retrying cleanup...");
                        showVerboseOutput = true; // Show details on subsequent attempts
                    }
                    Thread.Sleep(baseDelayMs * attempt); // Exponential backoff
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        Console.WriteLine($"⚠️  Warning: Could not fully clean up temporary directory: {ex.Message}");
                        Console.WriteLine($"   Directory: {Path.GetFileName(_currentProjectDir)}");
                    }
                    else
                    {
                        if (attempt == 1)
                        {
                            Console.WriteLine($"⏳ Cleanup encountered issues, retrying...");
                            showVerboseOutput = true;
                        }
                        Thread.Sleep(baseDelayMs * attempt);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attempts to clean up build output directories that commonly have locked files
    /// </summary>
    private void CleanupBuildOutputDirectories(string projectDir, bool verbose = false)
    {
        try
        {
            var buildDirs = new[] { "bin", "obj" };
            
            foreach (var buildDir in buildDirs)
            {
                var fullPath = Path.Combine(projectDir, buildDir);
                if (Directory.Exists(fullPath))
                {
                    try
                    {
                        Directory.Delete(fullPath, true);
                        if (verbose) Console.WriteLine($"✅ Cleaned {buildDir} directory");
                    }
                    catch (Exception ex)
                    {
                        if (verbose) Console.WriteLine($"⚠️  Could not clean {buildDir}: {ex.Message}");
                        
                        // Try to delete individual files in the directory  
                        CleanupFilesSelectively(fullPath, verbose);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (verbose) Console.WriteLine($"⚠️  Error during build directory cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to delete files selectively, skipping locked files
    /// </summary>
    private void CleanupFilesSelectively(string directory, bool verbose = false)
    {
        try
        {
            int deletedCount = 0;
            int skippedCount = 0;
            
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    if (verbose) Console.WriteLine($"🔧 Skipped locked file: {Path.GetFileName(file)}");
                }
            }

            // Try to remove empty directories
            foreach (var dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                    }
                }
                catch (Exception)
                {
                    // Ignore errors removing directories
                }
            }
            
            if (verbose && (deletedCount > 0 || skippedCount > 0))
            {
                Console.WriteLine($"🔧 Selective cleanup: {deletedCount} files deleted, {skippedCount} files skipped");
            }
        }
        catch (Exception ex)
        {
            if (verbose) Console.WriteLine($"⚠️  Error during selective cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Launches the generated Blazor project as a subprocess
    /// </summary>
    private async Task LaunchBlazorProjectAsync(string projectDir)
    {
        // Find an available port if the specified port is in use
        var availablePort = await FindAvailablePortAsync(_options.Port);
        if (availablePort != _options.Port)
        {
            Console.WriteLine($"⚠️  Port {_options.Port} is in use, using port {availablePort} instead");
        }
        
        Console.WriteLine($"🚀 Launching Blazor project on port {availablePort}...");
        
        var projectFile = Path.Combine(projectDir, "CadenzaWebApp.csproj");
        var arguments = $"run --project \"{projectFile}\" --urls http://localhost:{availablePort}";
        
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false // Allow seeing the process for debugging
        };

        // Add environment variables for the subprocess
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = $"http://localhost:{availablePort}";
        
        Console.WriteLine($"🔧 Executing: dotnet {arguments}");
        Console.WriteLine($"🔧 Environment: ASPNETCORE_URLS=http://localhost:{availablePort}");

        _currentProcess = new System.Diagnostics.Process { StartInfo = startInfo };
        
        // Handle process output
        _currentProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[Blazor] {e.Data}");
            }
        };
        
        _currentProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[Blazor Error] {e.Data}");
            }
        };

        try
        {
            _currentProcess.Start();
            _currentProcess.BeginOutputReadLine();
            _currentProcess.BeginErrorReadLine();
            
            Console.WriteLine($"✅ Blazor server started!");
            Console.WriteLine($"   URL: http://localhost:{availablePort}");
            Console.WriteLine($"   Component: {_options.InputFile}");
            Console.WriteLine($"   Process ID: {_currentProcess.Id}");
            
            if (_options.OpenBrowser)
            {
                // Wait a moment for the server to start, then open browser
                await Task.Delay(2000);
                OpenBrowser($"http://localhost:{availablePort}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press Ctrl+C to stop the server.");
            
            // Wait for the process to exit or be cancelled
            try
            {
                await _currentProcess.WaitForExitAsync();
                
                if (!_isShuttingDown)
                {
                    Console.WriteLine($"Blazor server stopped with exit code: {_currentProcess.ExitCode}");
                }
            }
            catch (InvalidOperationException)
            {
                // Process was already disposed by termination handler - this is expected during Ctrl+C
                if (!_isShuttingDown)
                {
                    throw; // Only re-throw if this wasn't an intentional shutdown
                }
            }
        }
        catch (Exception ex)
        {
            // Only show error if this wasn't an intentional shutdown
            if (!_isShuttingDown)
            {
                Console.WriteLine($"❌ Error starting Blazor project: {ex.Message}");
                throw;
            }
        }
    }
    
    /// <summary>
    /// Finds an available port starting from the specified port
    /// </summary>
    private async Task<int> FindAvailablePortAsync(int startPort)
    {
        for (int port = startPort; port < startPort + 100; port++)
        {
            if (await IsPortAvailableAsync(port))
            {
                return port;
            }
        }
        
        // If we can't find an available port in the range, return a random high port
        var random = new Random();
        return random.Next(8000, 9000);
    }
    
    /// <summary>
    /// Checks if a port is available for binding
    /// </summary>
    private async Task<bool> IsPortAvailableAsync(int port)
    {
        try
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (System.Net.Sockets.SocketException)
        {
            return false;
        }
    }
    
    private async Task BuildGeneratedProjectAsync(string projectDir)
    {
        Console.WriteLine($"🔧 Building generated project...");
        
        var buildProcess = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build --configuration Release --no-restore --verbosity normal",
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = System.Diagnostics.Process.Start(buildProcess);
        if (process != null)
        {
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                Console.WriteLine($"Build failed: {error}");
                // Continue anyway - the project might still work
            }
            else
            {
                Console.WriteLine($"   Project built successfully");
            }
        }
    }
    
    private void OpenBrowser(string url)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open browser: {ex.Message}");
        }
    }
}