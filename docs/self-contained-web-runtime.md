# Cadenza Self-Contained Web Runtime

The Cadenza self-contained web runtime enables developers to create complete web applications using only Cadenza syntax. With a single command, Cadenza components are transpiled to Blazor Server applications and served with an embedded web server.

## Quick Start

### Starting a Web Application

Transform any Cadenza component into a running web application:

```bash
# Start web server with a Cadenza component
cadenzac --serve counter.cdz

# Custom port and options
cadenzac --serve myapp.cdz --port 8080 --no-open --no-hot-reload
```

### Basic Counter Example

Create a file `counter.cdz`:

```cadenza
component Counter() 
    uses [DOM] 
    state [count]
    events [on_increment, on_decrement]
    -> UIComponent 
{
    declare_state count: int = 0
    
    event_handler handle_increment() uses [DOM] {
        set_state(count, count + 1)
    }
    
    event_handler handle_decrement() uses [DOM] {
        set_state(count, count - 1)
    }
    
    render {
        container(
            class: "semantic: padding:xl, background:background-alt, center"
        ) {
            heading(
                level: 1, 
                text: "Counter: " + count.toString(),
                class: "semantic: text:primary, margin:lg"
            )
            container(
                class: "semantic: flex, gap:md"
            ) {
                button(
                    text: "Increment",
                    on_click: handle_increment,
                    class: "btn-primary"
                )
                button(
                    text: "Decrement", 
                    on_click: handle_decrement,
                    class: "btn-secondary"
                )
            }
        }
    }
}
```

Then run:

```bash
cadenzac --serve counter.cdz
```

This will:
1. Parse the Cadenza component
2. Generate a complete Blazor Server project
3. Start an embedded web server on http://localhost:5000
4. Automatically open your browser to the running application

## Command-Line Options

### Basic Usage
```bash
cadenzac --serve <component.cdz> [options]
```

### Available Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--serve` | `-s` | Start embedded web server | - |
| `--port` | | Port for web server | 5000 |
| `--no-open` | | Don't automatically open browser | false |
| `--no-hot-reload` | | Disable hot reload functionality | false |
| `--verbose` | | Show detailed output | false |

### Examples

```bash
# Basic usage - opens browser automatically
cadenzac --serve app.cdz

# Custom port
cadenzac --serve app.cdz --port 3000

# Development mode - no browser, verbose output
cadenzac --serve app.cdz --no-open --verbose

# Production-like - no hot reload
cadenzac --serve app.cdz --no-hot-reload
```

## What Gets Generated

The self-contained web runtime creates a complete Blazor Server application:

### Project Structure
```
/tmp/cadenza-web-[uuid]/
├── Components/
│   └── Counter.cs              # Generated Blazor component
├── Pages/
│   ├── _Host.cshtml           # HTML host page
│   ├── Index.razor            # Main page
│   └── App.razor              # Blazor app root
├── wwwroot/
│   ├── css/
│   │   ├── site.css           # Base styles
│   │   └── components.css     # Generated semantic styles
│   └── js/
├── MainLayout.razor           # Layout component
└── Program.cs                 # ASP.NET Core startup
```

### Generated Files

#### Blazor Component (Components/Counter.cs)
```csharp
public class Counter : ComponentBase
{
    private int _count = 0;
    
    private void handle_increment()
    {
        _count = _count + 1;
        StateHasChanged();
    }
    
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "semantic: padding:xl, background:background-alt, center");
        // ... more render tree code
        builder.CloseElement();
    }
}
```

#### Host Page (_Host.cshtml)
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Cadenza Web App</title>
    <base href="~/" />
    <link href="css/site.css" rel="stylesheet" />
    <link href="css/components.css" rel="stylesheet" />
</head>
<body>
    <div id="app">
        <component type="typeof(App)" render-mode="ServerPrerendered" />
    </div>
    <script src="_framework/blazor.server.js"></script>
</body>
</html>
```

## Architecture

### Components

1. **CadenzaWebServer**: Embedded Kestrel web server
2. **BlazorProjectGenerator**: Creates complete Blazor project structure
3. **EnhancedBlazorGenerator**: Transpiles Cadenza to Blazor with semantic styling
4. **CadenzaSemanticStyling**: Generates design system CSS

### Process Flow

1. **Parse**: Cadenza source → AST
2. **Generate**: AST → Blazor components + CSS
3. **Project**: Create complete Blazor Server project
4. **Serve**: Start embedded web server
5. **Browse**: Open application in browser

### Hot Reload (Future)

The runtime includes infrastructure for hot reload:
- File watching for Cadenza source changes
- Automatic re-compilation and browser refresh
- SignalR integration for real-time updates

## Use Cases

### Rapid Prototyping
```bash
# Quickly test UI ideas
cadenzac --serve prototype.cdz
```

### Demo Applications
```bash
# Share demos without deployment
cadenzac --serve demo.cdz --port 8080
```

### Development Server
```bash
# Development with hot reload
cadenzac --serve app.cdz --verbose
```

### LLM-Generated Apps
```bash
# LLMs can create complete web apps
# Just generate Cadenza component syntax
cadenzac --serve llm-generated-app.cdz
```

## Comparison with Traditional Development

### Traditional Web Development
```bash
# Multiple steps required
npm create react-app my-app
cd my-app
npm install
npm run build
npm run start
# Edit multiple files: HTML, CSS, JavaScript, package.json
```

### Cadenza Self-Contained Runtime
```bash
# Single command, single file
cadenzac --serve my-app.cdz
# Edit one file: my-app.cdz (Cadenza syntax only)
```

## Benefits

### For Developers
- **Zero Configuration**: No webpack, package.json, or build tools
- **Single File**: Entire application in one Cadenza file
- **Instant Feedback**: Immediate browser preview
- **Production Ready**: Generated code follows ASP.NET Core best practices

### For LLMs
- **Simple Syntax**: Only need to know Cadenza component syntax
- **No Web Knowledge**: Don't need HTML, CSS, JavaScript, or frameworks
- **Complete Applications**: Can generate full-stack apps
- **Consistent Output**: Predictable file structure and behavior

### For Teams
- **Shareable**: Send a single .cdz file to share applications
- **Reproducible**: Same Cadenza file produces identical results
- **Maintainable**: Changes only need to be made in Cadenza syntax
- **Testable**: Generated code can be unit tested normally

## Limitations

### Current Version
- Single component per application
- No API endpoints (UI only)
- No persistent storage
- Temporary project files (cleaned up on exit)

### Future Roadmap
- Multi-file projects with file-system routing
- API endpoints alongside UI components
- Database integration
- Static site generation
- Docker containerization
- Cloud deployment integration

## Troubleshooting

### Common Issues

**Port Already in Use**
```bash
Error: Address already in use
# Solution: Use different port
cadenzac --serve app.cdz --port 5001
```

**Component Parse Errors**
```bash
Error: Expected '(' after component name
# Solution: Check Cadenza syntax matches examples
```

**Browser Doesn't Open**
```bash
# Manual navigation required
# Open browser to: http://localhost:5000
```

### Debugging

**Verbose Output**
```bash
cadenzac --serve app.cdz --verbose
```

**Check Generated Files**
```bash
# Temporary directory shown in output:
# Content root path: /tmp/cadenza-web-[uuid]
```

## Examples

See the `examples/` directory for more Cadenza components that work with the self-contained runtime:

- `examples/counter.cdz` - Basic counter with semantic styling
- `examples/BlazorCounter.cdz` - Counter with Blazor-style syntax
- `examples/simple_ui_test.cdz` - UI component showcase
- `examples/realistic_todo_app.cdz` - Todo application example

## Related Documentation

- [Semantic Styling System](semantic-styling.md) - CSS-in-Cadenza documentation
- [Component Architecture](../specifications.md) - Cadenza component syntax
- [BlazorGenerator Implementation](BlazorGenerator_Implementation.md) - Technical details