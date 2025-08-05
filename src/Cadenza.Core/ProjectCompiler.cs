using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cadenza.Core
{
    /// <summary>
    /// Project configuration loaded from cadenzac.json
    /// </summary>
    public class ProjectConfig
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = "";
        public BuildConfig Build { get; set; } = new();
        public string[] Include { get; set; } = new[] { "**/*.cdz" };
        public string[] Exclude { get; set; } = Array.Empty<string>();
        public Dictionary<string, string> Dependencies { get; set; } = new();
        public Dictionary<string, string> DevDependencies { get; set; } = new();
        public CompilerConfig Compiler { get; set; } = new();
    }

    public class BuildConfig
    {
        public string Source { get; set; } = "./";
        public string Output { get; set; } = "bin/";
        public string OutputType { get; set; } = "exe";
        public string? EntryPoint { get; set; }
        public string Target { get; set; } = "csharp";
        public string Framework { get; set; } = "net8.0";
    }

    public class CompilerConfig
    {
        public bool StrictMode { get; set; } = true;
        public bool EnableNullabilityChecks { get; set; } = true;
        public bool WarningsAsErrors { get; set; } = false;
        public bool DebugSymbols { get; set; } = true;
    }

    /// <summary>
    /// Represents a source file in the project
    /// </summary>
    public class SourceFile
    {
        public string FilePath { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public List<string> ImportedModules { get; set; } = new();
        public List<string> ExportedSymbols { get; set; } = new();
        public DateTime LastModified { get; set; }
        public bool HasMainFunction { get; set; }
    }

    /// <summary>
    /// Represents the dependency graph between source files
    /// </summary>
    public class DependencyGraph
    {
        public List<SourceFile> Files { get; set; } = new();
        public Dictionary<string, List<string>> Dependencies { get; set; } = new();
        
        public List<SourceFile> GetTopologicalOrder()
        {
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();
            var result = new List<SourceFile>();
            
            foreach (var file in Files)
            {
                if (!visited.Contains(file.ModuleName))
                {
                    if (!TopologicalSortVisit(file.ModuleName, visited, visiting, result))
                    {
                        throw new InvalidOperationException($"Circular dependency detected involving module: {file.ModuleName}");
                    }
                }
            }
            
            return result;
        }
        
        private bool TopologicalSortVisit(string moduleName, HashSet<string> visited, HashSet<string> visiting, List<SourceFile> result)
        {
            if (visiting.Contains(moduleName))
                return false; // Circular dependency
                
            if (visited.Contains(moduleName))
                return true; // Already processed
                
            visiting.Add(moduleName);
            
            if (Dependencies.ContainsKey(moduleName))
            {
                foreach (var dependency in Dependencies[moduleName])
                {
                    if (!TopologicalSortVisit(dependency, visited, visiting, result))
                        return false;
                }
            }
            
            visiting.Remove(moduleName);
            visited.Add(moduleName);
            
            var file = Files.FirstOrDefault(f => f.ModuleName == moduleName);
            if (file != null)
            {
                result.Add(file);
            }
            
            return true;
        }
    }

    /// <summary>
    /// Project compilation result
    /// </summary>
    public class ProjectCompilationResult
    {
        public bool Success { get; set; }
        public string? OutputPath { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public TimeSpan CompilationTime { get; set; }
        public int FilesCompiled { get; set; }
    }

    /// <summary>
    /// Handles multi-file project compilation
    /// </summary>
    public class ProjectCompiler
    {
        private readonly DirectCompiler _directCompiler;

        public ProjectCompiler()
        {
            _directCompiler = new DirectCompiler();
        }

        /// <summary>
        /// Compile a project from the current directory
        /// </summary>
        public async Task<ProjectCompilationResult> CompileProjectAsync(CLIOptions options)
        {
            var startTime = DateTime.Now;
            var result = new ProjectCompilationResult();

            try
            {
                // Load project configuration
                var config = await LoadProjectConfigAsync(options.ConfigFile);
                
                if (options.Verbose)
                {
                    Console.WriteLine($"Loaded project: {config.Name} v{config.Version}");
                    Console.WriteLine($"Source directory: {config.Build.Source}");
                    Console.WriteLine($"Output directory: {config.Build.Output}");
                }

                // Discover source files
                var sourceFiles = await DiscoverSourceFilesAsync(config);
                
                if (sourceFiles.Count == 0)
                {
                    result.Errors.Add("No .cdz files found in project");
                    return result;
                }

                if (options.Verbose)
                {
                    Console.WriteLine($"Found {sourceFiles.Count} source files");
                }

                // Build dependency graph
                var dependencyGraph = await BuildDependencyGraphAsync(sourceFiles);
                
                // Get compilation order
                var compilationOrder = dependencyGraph.GetTopologicalOrder();
                
                if (options.Verbose)
                {
                    Console.WriteLine("Compilation order:");
                    foreach (var file in compilationOrder)
                    {
                        Console.WriteLine($"  {file.FilePath}");
                    }
                }

                // Find entry point (only required for executables)
                var isLibrary = config.Build.OutputType == "library" || 
                               config.Build.OutputType == "dll" ||
                               config.Build.OutputType == "webapp" ||  // Webapp projects don't need main()
                               options.Library;
                               
                var entryPoint = FindEntryPoint(config, compilationOrder);
                if (entryPoint == null && !isLibrary)
                {
                    result.Errors.Add("No main() function found for executable output");
                    return result;
                }

                // Compile all files
                var compilationResult = await CompileMultipleFilesAsync(compilationOrder, config, options);
                
                result.Success = compilationResult.Success;
                result.OutputPath = GetOutputPath(config, options);
                if (!compilationResult.Success && compilationResult.Diagnostics != null)
                {
                    result.Errors.AddRange(compilationResult.Diagnostics.Select(d => d.ToString()));
                }
                result.FilesCompiled = sourceFiles.Count;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Compilation failed: {ex.Message}");
            }

            result.CompilationTime = DateTime.Now - startTime;
            return result;
        }

        /// <summary>
        /// Load project configuration from cadenzac.json or use defaults
        /// </summary>
        private async Task<ProjectConfig> LoadProjectConfigAsync(string? configPath)
        {
            var defaultPath = "cadenzac.json";
            var filePath = configPath ?? defaultPath;

            if (!File.Exists(filePath))
            {
                // Return default configuration
                var directoryName = Path.GetFileName(Directory.GetCurrentDirectory());
                return new ProjectConfig
                {
                    Name = directoryName,
                    Build = new BuildConfig()
                };
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var config = JsonSerializer.Deserialize<ProjectConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return config ?? throw new InvalidOperationException("Failed to deserialize project configuration");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load project configuration from {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Discover all .cdz files in the project
        /// </summary>
        private async Task<List<SourceFile>> DiscoverSourceFilesAsync(ProjectConfig config)
        {
            var sourceFiles = new List<SourceFile>();
            var sourceDirectory = Path.GetFullPath(config.Build.Source);

            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");
            }

            // Find all .cdz files
            var files = Directory.GetFiles(sourceDirectory, "*.cdz", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // Apply include/exclude patterns (simplified - just check extensions for now)
                if (ShouldIncludeFile(file, config))
                {
                    var sourceFile = await AnalyzeSourceFileAsync(file, sourceDirectory);
                    sourceFiles.Add(sourceFile);
                }
            }

            return sourceFiles;
        }

        /// <summary>
        /// Analyze a source file to extract imports, exports, and metadata
        /// </summary>
        private async Task<SourceFile> AnalyzeSourceFileAsync(string filePath, string sourceRoot)
        {
            var content = await File.ReadAllTextAsync(filePath);
            var relativePath = Path.GetRelativePath(sourceRoot, filePath);
            
            var sourceFile = new SourceFile
            {
                FilePath = relativePath,
                ModuleName = GetModuleNameFromPath(relativePath),
                LastModified = File.GetLastWriteTime(filePath)
            };

            try
            {
                // Simplified analysis - just check for main function
                sourceFile.HasMainFunction = content.Contains("function main(") || content.Contains("function main() ->");
                if (sourceFile.HasMainFunction)
                {
                    Console.WriteLine($"Found main function in: {sourceFile.FilePath}");
                }
                
                // For now, skip complex import/export analysis
                // This will be enhanced later when the parser is more stable
            }
            catch (Exception ex)
            {
                // For now, continue with basic file info if parsing fails
                Console.WriteLine($"Warning: Could not fully analyze {filePath}: {ex.Message}");
            }

            return sourceFile;
        }

        /// <summary>
        /// Build dependency graph from source files
        /// </summary>
        private Task<DependencyGraph> BuildDependencyGraphAsync(List<SourceFile> sourceFiles)
        {
            var graph = new DependencyGraph { Files = sourceFiles };

            foreach (var file in sourceFiles)
            {
                graph.Dependencies[file.ModuleName] = new List<string>();
                
                foreach (var import in file.ImportedModules)
                {
                    // Find the corresponding source file for this import
                    var dependency = sourceFiles.FirstOrDefault(f => f.ModuleName == import);
                    if (dependency != null)
                    {
                        graph.Dependencies[file.ModuleName].Add(import);
                    }
                }
            }

            return Task.FromResult(graph);
        }

        /// <summary>
        /// Compile multiple files into a single assembly
        /// </summary>
        private async Task<CompilationResult> CompileMultipleFilesAsync(
            List<SourceFile> files, 
            ProjectConfig config, 
            CLIOptions options)
        {
            // Use automatic UI component detection instead of explicit target

            // For now, we'll combine all files into a single compilation unit
            // This is a simplified approach - a full implementation would handle
            // cross-file references more sophisticatedly

            var combinedContent = new System.Text.StringBuilder();
            
            foreach (var file in files)
            {
                var fullPath = Path.Combine(config.Build.Source, file.FilePath);
                var content = await File.ReadAllTextAsync(fullPath);
                
                combinedContent.AppendLine($"// === {file.FilePath} ===");
                combinedContent.AppendLine(content);
                combinedContent.AppendLine();
            }

            // Create a temporary file with combined content
            var tempFile = Path.GetTempFileName() + ".cdz";
            await File.WriteAllTextAsync(tempFile, combinedContent.ToString());

            try
            {
                // Determine output path
                var outputPath = GetOutputPath(config, options);
                
                // Determine output kind
                var isLibrary = config.Build.OutputType == "library" || 
                               config.Build.OutputType == "dll" ||
                               options.Library;
                
                // Use existing DirectCompiler interface
                var compilationOptions = new Cadenza.Core.CompilationOptions(
                    SourceFile: tempFile,
                    OutputPath: outputPath,
                    OutputKind: isLibrary ? 
                        Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary : 
                        Microsoft.CodeAnalysis.OutputKind.ConsoleApplication,
                    OptimizeCode: !options.Debug,
                    IncludeDebugSymbols: config.Compiler.DebugSymbols || options.Debug
                );

                return await _directCompiler.CompileToAssemblyAsync(compilationOptions);
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }


        /// <summary>
        /// Helper methods
        /// </summary>
        private bool ShouldIncludeFile(string filePath, ProjectConfig config)
        {
            // Simplified include/exclude logic
            return filePath.EndsWith(".cdz") && 
                   !config.Exclude.Any(pattern => filePath.Contains(pattern.Replace("**", "")));
        }

        private string GetModuleNameFromPath(string relativePath)
        {
            return Path.GetFileNameWithoutExtension(relativePath).Replace(Path.DirectorySeparatorChar, '.');
        }

        private List<string> ExtractImports(ProgramNode ast)
        {
            // This would need full integration with the parser
            // For now, return empty list
            return new List<string>();
        }

        private List<string> ExtractExports(ProgramNode ast)
        {
            // This would need full integration with the parser
            // For now, return empty list
            return new List<string>();
        }

        private bool HasMainFunction(ProgramNode ast)
        {
            // Simplified for now - will be enhanced with proper AST integration
            return false;
        }

        private SourceFile? FindEntryPoint(ProjectConfig config, List<SourceFile> files)
        {
            if (!string.IsNullOrEmpty(config.Build.EntryPoint))
            {
                Console.WriteLine($"Looking for configured entry point: {config.Build.EntryPoint}");
                return files.FirstOrDefault(f => f.FilePath == config.Build.EntryPoint);
            }

            // Look for a file with main() function
            Console.WriteLine("Looking for main function in files:");
            foreach (var file in files)
            {
                Console.WriteLine($"  {file.FilePath}: HasMainFunction = {file.HasMainFunction}");
            }
            return files.FirstOrDefault(f => f.HasMainFunction);
        }

        private string GetOutputPath(ProjectConfig config, CLIOptions options)
        {
            if (!string.IsNullOrEmpty(options.OutputFile))
            {
                return options.OutputFile;
            }

            var outputDir = config.Build.Output;
            Directory.CreateDirectory(outputDir);

            // Use automatic UI component detection to determine output type

            // Determine if this should be a library or executable
            var isLibrary = config.Build.OutputType == "library" || 
                           config.Build.OutputType == "dll" ||
                           options.Library;
            
            var extension = isLibrary ? ".dll" : ".exe";
            return Path.Combine(outputDir, config.Name + extension);
        }
    }

}