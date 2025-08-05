# Phase 5 - Blazor Frontend Implementation Plan

## Overview
Implement Blazor code generation for Cadenza UI components. This will enable full-stack development with Cadenza → C# → Blazor pipeline.

## Why Blazor Over React
- **Perfect Language Alignment**: Cadenza → C# → Blazor (no translation layer)
- **Shared Type System**: C# types work natively with Blazor components
- **Effect System Integration**: Blazor services map naturally to Cadenza effects
- **Backend-First Philosophy**: Aligns with Cadenza's server-side focus

## Current UI Syntax Analysis
**Existing Cadenza UI examples**:
- `examples/simple_ui_test.cdz` - Basic component with state and events
- `examples/advanced_ui_test.cdz` - Complex component with effects and lifecycle
- `docs/ui-components.md` - Complete UI syntax documentation

**Cadenza UI Pattern**:
```cadenza
component HelloWorld(name: string) 
    uses [DOM] 
    state [message, counter]
    events [on_click, on_reset]
    -> UIComponent 
{
    declare_state message: string = "Hello"
    declare_state counter: int = 0
    
    on_mount {
        set_state(message, "Component mounted!")
    }
    
    event_handler handle_click() uses [DOM] {
        set_state(counter, counter + 1)
    }
    
    render {
        div(class: "hello-world") {
            h1(text: message)
            button(text: "Click me", on_click: handle_click)
        }
    }
}
```

## Tasks

### Task 1: Design Blazor Mapping
**Cadenza → Blazor Component Mapping**:
- `component` → Blazor `@page` component
- `declare_state` → Blazor `@code` block with fields
- `event_handler` → Blazor event methods  
- `render` → Blazor markup (`.razor` file)
- `uses [DOM]` → Blazor services injection

### Task 2: Create Blazor Generator
**Create**: `src/Cadenza.Tools/frontend/blazor-generator.cdz`
**Functionality**:
- Parse Cadenza UI syntax
- Generate `.razor` files with proper Blazor syntax
- Create supporting C# classes if needed
- Handle component registration and services

### Task 3: Extend DirectCompiler
**Add `--target blazor` to DirectCompiler**:
- Modify `src/Cadenza.Core/DirectCompiler.cs`
- Add Blazor target to compilation options
- Generate `.razor` files instead of `.cs`
- Create proper project structure for Blazor apps

### Task 4: Blazor Syntax Mapping
**Component Structure**:
```blazor
@page "/hello"
@using Microsoft.AspNetCore.Components

<div class="hello-world">
    <h1>@message</h1>
    <button @onclick="HandleClick">Click me</button>
</div>

@code {
    [Parameter] public string Name { get; set; } = "";
    
    private string message = "Hello";
    private int counter = 0;
    
    protected override void OnInitialized()
    {
        message = "Component mounted!";
    }
    
    private void HandleClick()
    {
        counter++;
    }
}
```

### Task 5: Test Implementation
**Test with existing UI examples**:
```bash
# Target commands:
./bin/release/cadenzac-core --target blazor examples/simple_ui_test.cdz -o output/SimpleUI.razor
./bin/release/cadenzac-core --target blazor examples/advanced_ui_test.cdz -o output/Dashboard.razor

# Create Blazor project:
dotnet new blazor -n CadenzaUI
cp output/*.razor CadenzaUI/Components/
dotnet run --project CadenzaUI
```

### Task 6: Integration with Dev Server
**Hot Reload Support**:
- Integrate with existing `dev-server.cdz` (once runtime bridge fixed)
- Support file watching for `.cdz` UI files
- Auto-regenerate `.razor` files on changes
- Browser refresh on UI compilation

## Expected Outcomes
- Working Blazor code generation from Cadenza UI syntax
- Full-stack development capability (Cadenza backend + Cadenza frontend)
- Hot reload development experience
- Foundation for Cadenza UI ecosystem

## Success Criteria
- [ ] `simple_ui_test.cdz` generates working Blazor component
- [ ] `advanced_ui_test.cdz` generates complex Blazor component  
- [ ] Generated components work in browser
- [ ] Hot reload integration functional
- [ ] Complete Cadenza → Blazor pipeline working

## Dependencies
- **Runtime Bridge Fixes**: Need working .cdz tools for dev server integration
- **DirectCompiler Extension**: Need to add Blazor target support

## Priority
**HIGH** - Major differentiator for Cadenza in full-stack development space.