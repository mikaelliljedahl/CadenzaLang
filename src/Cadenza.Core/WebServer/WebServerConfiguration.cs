// Cadenza Core Compiler - Web Server Configuration Classes
// Configuration data structures for the Cadenza web server

namespace Cadenza.Core;

// =============================================================================
// WEB SERVER CONFIGURATION
// =============================================================================

/// <summary>
/// Configuration options for the Cadenza web server
/// </summary>
public class CadenzaWebServerOptions
{
    public required string InputFile { get; init; }
    public int Port { get; init; } = 5000;
    public bool OpenBrowser { get; init; } = true;
    public bool HotReload { get; init; } = true;
}

// =============================================================================
// PROJECT CONFIGURATION CLASSES
// =============================================================================

/// <summary>
/// Project configuration for UI project serving
/// </summary>
public class UIProjectConfig
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "";
    public UIBuildConfig Build { get; set; } = new();
    public UINavigationConfig UI { get; set; } = new();
}

/// <summary>
/// Build configuration for UI projects
/// </summary>
public class UIBuildConfig
{
    public string Source { get; set; } = "components/";
    public string OutputType { get; set; } = "webapp";
    public string Target { get; set; } = "blazor";
}

/// <summary>
/// UI navigation configuration
/// </summary>
public class UINavigationConfig
{
    public NavigationConfig Navigation { get; set; } = new();
}

/// <summary>
/// Navigation settings
/// </summary>
public class NavigationConfig
{
    public bool ShowNavigation { get; set; } = true;
    public string HomeComponent { get; set; } = "";
}