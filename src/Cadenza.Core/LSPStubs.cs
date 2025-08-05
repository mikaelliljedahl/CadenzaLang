// LSP Stubs for missing Language Server Protocol components
// These provide minimal implementations for testing until full LSP is implemented

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cadenza.Core
{
    // LSP-related classes  
    public class ManagedDocument
    {
        public string FilePath { get; init; } = "";
        public string Content { get; init; } = "";
        public DateTime LastModified { get; init; } = DateTime.Now;
        public string Uri { get; init; } = "";
        public int Version { get; init; } = 1;
        public List<Token> Tokens { get; set; } = new();
        public ASTNode? AST { get; set; } = null;
        public List<string> ParseErrors { get; set; } = new();
        
        public ManagedDocument()
        {
        }
        
        public ManagedDocument(string filePath, string content)
        {
            FilePath = filePath;
            Content = content;
            LastModified = DateTime.Now;
        }
    }
}

// LSP namespace stub
namespace Cadenza.LanguageServer
{
    public class CompletionProvider
    {
        public List<string> GetCompletions(string text, int position)
        {
            // Stub implementation
            return new List<string>();
        }
    }
    
    public class DiagnosticsProvider
    {
        public List<string> GetDiagnostics(string text)
        {
            // Stub implementation
            return new List<string>();
        }
    }
    
    public class DocumentManager
    {
        public void OpenDocument(string filePath, string content)
        {
            // Stub implementation
        }
        
        public void CloseDocument(string filePath)
        {
            // Stub implementation
        }
    }
}