// Cadenza Core Compiler - Hot Reload Service
// Hot reload functionality and file watching for Cadenza web server

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cadenza.Core;

// =============================================================================
// HOT RELOAD SERVICE
// =============================================================================

/// <summary>
/// Service for handling hot reload functionality
/// </summary>
public class CadenzaHotReloadService
{
    private readonly List<Func<Task>> _reloadCallbacks = new();
    
    public void RegisterReloadCallback(Func<Task> callback)
    {
        _reloadCallbacks.Add(callback);
    }
    
    public async Task TriggerReloadAsync()
    {
        foreach (var callback in _reloadCallbacks)
        {
            try
            {
                await callback();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hot reload callback failed: {ex.Message}");
            }
        }
    }
}