// Cadenza Core Compiler - Compilation Cache
// Extracted from Compiler.cs

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace Cadenza.Core;

// =============================================================================
// COMPILATION CACHE
// =============================================================================

/// <summary>
/// Caches compilation objects for performance optimization
/// </summary>
public class CompilationCache
{
    private readonly Dictionary<string, CSharpCompilation> _compilationCache = new();
    private readonly Dictionary<string, DateTime> _lastModified = new();

    public bool TryGetCachedCompilation(string sourceFile, out CSharpCompilation? compilation)
    {
        compilation = null;

        if (!_compilationCache.ContainsKey(sourceFile))
            return false;

        var fileInfo = new FileInfo(sourceFile);
        if (!fileInfo.Exists)
            return false;

        if (_lastModified.ContainsKey(sourceFile) && 
            _lastModified[sourceFile] >= fileInfo.LastWriteTime)
        {
            compilation = _compilationCache[sourceFile];
            return true;
        }

        // File has been modified, remove from cache
        _compilationCache.Remove(sourceFile);
        _lastModified.Remove(sourceFile);
        return false;
    }

    public void CacheCompilation(string sourceFile, CSharpCompilation compilation)
    {
        var fileInfo = new FileInfo(sourceFile);
        if (!fileInfo.Exists) return;

        _compilationCache[sourceFile] = compilation;
        _lastModified[sourceFile] = fileInfo.LastWriteTime;
    }

    public void ClearCache()
    {
        _compilationCache.Clear();
        _lastModified.Clear();
    }
}