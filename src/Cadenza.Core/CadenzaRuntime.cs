// Cadenza Runtime Bridge - Provides system operation interfaces for Cadenza tools
// This allows Cadenza code to call .NET system functions for HTTP, file operations, etc.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace Cadenza.Runtime
{
    // =============================================================================
    // HTTP SERVER RUNTIME
    // =============================================================================
    
    public static class HttpServerRuntime
    {
        public static HttpListener CreateServer(int port)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            return listener;
        }
        
        public static void StartServer(HttpListener listener)
        {
            listener.Start();
        }
        
        public static async Task<HttpListenerContext> WaitForRequest(HttpListener listener)
        {
            return await listener.GetContextAsync();
        }
        
        public static void SendResponse(HttpListenerContext context, string content, string contentType = "text/html")
        {
            var response = context.Response;
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            response.ContentType = contentType;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        
        public static void StopServer(HttpListener listener)
        {
            listener.Stop();
            listener.Close();
        }
    }
    
    // =============================================================================
    // FILE SYSTEM RUNTIME
    // =============================================================================
    
    public static class FileSystemRuntime
    {
        public static string ReadFile(string path)
        {
            return File.ReadAllText(path);
        }
        
        public static void WriteFile(string path, string content)
        {
            File.WriteAllText(path, content);
        }
        
        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }
        
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
        
        public static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
        
        public static string[] ListFiles(string directory)
        {
            return Directory.GetFiles(directory);
        }
        
        public static string[] ListDirectories(string directory)
        {
            return Directory.GetDirectories(directory);
        }
        
        public static FileSystemWatcher CreateWatcher(string path)
        {
            return new FileSystemWatcher(path);
        }
        
        public static void StartWatching(FileSystemWatcher watcher, Action<string> onChanged)
        {
            watcher.Changed += (sender, e) => onChanged(e.FullPath);
            watcher.Created += (sender, e) => onChanged(e.FullPath);
            watcher.Deleted += (sender, e) => onChanged(e.FullPath);
            watcher.EnableRaisingEvents = true;
        }
        
        public static void StartWatching(FileSystemWatcher watcher, string filter)
        {
            watcher.Filter = filter;
            watcher.EnableRaisingEvents = true;
        }
        
        public static List<string> FindFiles(string directory, string pattern, bool recursive = false)
        {
            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(directory, pattern, searchOption);
                return files.ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
    
    // =============================================================================
    // WEBSOCKET RUNTIME
    // =============================================================================
    
    public static class WebSocketRuntime
    {
        private static readonly List<WebSocket> _connections = new();
        
        public static HttpListener CreateWebSocketServer(int port)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            return listener;
        }
        
        public static async Task<WebSocket> AcceptWebSocketAsync(HttpListenerContext context)
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            _connections.Add(webSocket);
            return webSocket;
        }
        
        public static async Task SendMessage(WebSocket webSocket, string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        
        public static async Task BroadcastMessage(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var tasks = new List<Task>();
            
            foreach (var connection in _connections.ToArray())
            {
                if (connection.State == WebSocketState.Open)
                {
                    tasks.Add(connection.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None));
                }
                else
                {
                    _connections.Remove(connection);
                }
            }
            
            await Task.WhenAll(tasks);
        }
        
        public static void CloseConnection(WebSocket webSocket)
        {
            _connections.Remove(webSocket);
            if (webSocket.State == WebSocketState.Open)
            {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", System.Threading.CancellationToken.None);
            }
        }
    }
    
    // =============================================================================
    // PROCESS RUNTIME
    // =============================================================================
    
    public static class ProcessRuntime
    {
        public class ProcessResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; } = "";
            public string Error { get; set; } = "";
        }
        
        public static ProcessResult ExecuteCommand(string command, string[] args)
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = string.Join(" ", args);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = output,
                Error = error
            };
        }
        
        public static int GetExitCode(ProcessResult result)
        {
            return result.ExitCode;
        }
        
        public static string GetOutput(ProcessResult result)
        {
            return result.Output;
        }
        
        public static string GetErrorOutput(ProcessResult result)
        {
            return result.Error;
        }
        
        public static async Task<ProcessResult> ExecuteCommandAsync(string command, string[] args)
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = string.Join(" ", args);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = output,
                Error = error
            };
        }
    }
    
    // =============================================================================
    // LOGGING RUNTIME
    // =============================================================================
    
    public static class LoggingRuntime
    {
        public static void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }
        
        public static void LogWarning(string message)
        {
            Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }
        
        public static void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }
        
        public static void LogDebug(string message)
        {
            Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }
    }
    
    // =============================================================================
    // JSON RUNTIME
    // =============================================================================
    
    public static class JsonRuntime
    {
        public static string Stringify(object obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }
        
        public static T Parse<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        
        public static JsonElement ParseToElement(string json)
        {
            return JsonDocument.Parse(json).RootElement;
        }
        
        public static bool TryParse<T>(string json, out T result)
        {
            try
            {
                result = JsonSerializer.Deserialize<T>(json);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }
    }
    
    // =============================================================================
    // COMMAND LINE RUNTIME
    // =============================================================================
    
    public static class CommandLineRuntime
    {
        public class ParsedArgs
        {
            public Dictionary<string, string> Options { get; set; } = new();
            public List<string> Arguments { get; set; } = new();
            public Dictionary<string, bool> Flags { get; set; } = new();
        }
        
        public static ParsedArgs ParseArgs(string[] args)
        {
            var result = new ParsedArgs();
            
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                
                if (arg.StartsWith("--"))
                {
                    var key = arg.Substring(2);
                    if (key.Contains("="))
                    {
                        var parts = key.Split('=', 2);
                        result.Options[parts[0]] = parts[1];
                    }
                    else if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        result.Options[key] = args[i + 1];
                        i++; // Skip the next argument as it's the value
                    }
                    else
                    {
                        result.Flags[key] = true;
                    }
                }
                else if (arg.StartsWith("-"))
                {
                    var key = arg.Substring(1);
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        result.Options[key] = args[i + 1];
                        i++; // Skip the next argument as it's the value
                    }
                    else
                    {
                        result.Flags[key] = true;
                    }
                }
                else
                {
                    result.Arguments.Add(arg);
                }
            }
            
            return result;
        }
        
        public static string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }
        
        public static string[] GetEnvironmentArgs()
        {
            return Environment.GetCommandLineArgs();
        }
    }
    
    // =============================================================================
    // GLOB RUNTIME
    // =============================================================================
    
    public static class GlobRuntime
    {
        public static string[] MatchFiles(string pattern, string directory = ".")
        {
            if (!Directory.Exists(directory))
                return new string[0];
                
            var regex = GlobToRegex(pattern);
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            
            return files.Where(file => regex.IsMatch(Path.GetRelativePath(directory, file).Replace('\\', '/'))).ToArray();
        }
        
        public static string[] MatchFilesInDirectory(string pattern, string directory)
        {
            if (!Directory.Exists(directory))
                return new string[0];
                
            var regex = GlobToRegex(pattern);
            var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
            
            return files.Where(file => regex.IsMatch(Path.GetFileName(file))).ToArray();
        }
        
        private static Regex GlobToRegex(string pattern)
        {
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace(@"\*\*", ".*")     // ** matches any number of directories
                .Replace(@"\*", "[^/]*")    // * matches any characters except directory separator
                .Replace(@"\?", ".") + "$"; // ? matches any single character
            
            return new Regex(regexPattern, RegexOptions.IgnoreCase);
        }
    }
    
    // =============================================================================
    // CONFIG RUNTIME
    // =============================================================================
    
    public static class ConfigRuntime
    {
        public class ProjectConfig
        {
            public string Name { get; set; } = "";
            public string Version { get; set; } = "1.0.0";
            public List<string> Dependencies { get; set; } = new();
            public Dictionary<string, object> Settings { get; set; } = new();
        }
        
        public static ProjectConfig LoadFlowcJson(string path)
        {
            if (!File.Exists(path))
                return new ProjectConfig();
                
            var json = File.ReadAllText(path);
            return JsonRuntime.Parse<ProjectConfig>(json);
        }
        
        public static void SaveFlowcJson(string path, ProjectConfig config)
        {
            var json = JsonRuntime.Stringify(config);
            File.WriteAllText(path, json);
        }
        
        public static bool ConfigExists(string path)
        {
            return File.Exists(path);
        }
    }
    
    // =============================================================================
    // STRING UTILITIES RUNTIME
    // =============================================================================
    
    public static class StringRuntime
    {
        public static bool StartsWith(string str, string prefix)
        {
            return str.StartsWith(prefix);
        }
        
        public static bool EndsWith(string str, string suffix)
        {
            return str.EndsWith(suffix);
        }
        
        public static bool Contains(string str, string substring)
        {
            return str.Contains(substring);
        }
        
        public static string Replace(string str, string oldValue, string newValue)
        {
            return str.Replace(oldValue, newValue);
        }
        
        public static string Substring(string str, int start, int length = -1)
        {
            if (length == -1)
                return str.Substring(start);
            return str.Substring(start, length);
        }
        
        public static string[] Split(string str, string separator)
        {
            return str.Split(new[] { separator }, StringSplitOptions.None);
        }
        
        public static string Join(string[] items, string separator)
        {
            return string.Join(separator, items);
        }
        
        public static string Trim(string str)
        {
            return str.Trim();
        }
        
        public static string ToUpper(string str)
        {
            return str.ToUpper();
        }
        
        public static string ToLower(string str)
        {
            return str.ToLower();
        }
        
        public static string ToString(int value)
        {
            return value.ToString();
        }
        
        public static string ToString(bool value)
        {
            return value.ToString();
        }
        
        public static string ToString(double value)
        {
            return value.ToString();
        }
    }
    
    // =============================================================================
    // CONSOLE RUNTIME
    // =============================================================================
    
    public static class ConsoleRuntime
    {
        public static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
        
        public static void Write(string message)
        {
            Console.Write(message);
        }
        
        public static string ReadLine()
        {
            return Console.ReadLine() ?? "";
        }
        
        public static ConsoleKeyInfo ReadKey()
        {
            return Console.ReadKey();
        }
        
        public static void Clear()
        {
            Console.Clear();
        }
        
        public static void SetForegroundColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
        }
        
        public static void ResetColor()
        {
            Console.ResetColor();
        }
    }
}