# Cadenza Frontend Development - Getting Started 🚀

**Build full-stack applications with Cadenza's LLM-optimized UI component system. Write both backend services and frontend components in the same language with complete type safety.**

## Quick Start

### 1. Multi-Component Projects (Recommended)

The recommended approach for building UI applications is to use a project structure with multiple component files and a `cadenzac.json` configuration:

```bash
# Build once (from repo root)
dotnet build src/Cadenza.Core/cadenzac-core.csproj -c Release

# Serve a multi-component project
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj --serve examples/multi-component-project --port 5179

# Flags:
#   --serve <project-path>  Required; path to directory containing cadenzac.json or direct path to cadenzac.json
#   --port <number>         Optional; default 5000. If in use, a nearby port is auto-selected  
#   --no-open               Optional; do not auto-open browser
```

**Project Structure:**
```
my-ui-project/
├── cadenzac.json          # Project configuration
├── components/            # UI components (auto-discovered)
│   ├── Counter.cdz       # Individual component files
│   ├── Dashboard.cdz
│   └── Settings.cdz
└── styles/               # Optional shared styles
    └── theme.css
```

**Project Configuration (`cadenzac.json`):**
```json
{
  "name": "MyUIApp",
  "version": "1.0.0",
  "description": "A multi-component UI application",
  "build": {
    "source": "components/",
    "outputType": "webapp",
    "target": "blazor"
  },
  "ui": {
    "navigation": {
      "showNavigation": true,
      "homeComponent": "Counter"
    },
    "styles": ["styles/theme.css"]
  }
}
```

**What happens:**
- All `.cdz` files in `components/` directory are auto-discovered as UI components
- A temporary Blazor project is generated under a debug/ folder
- The project is built and launched (Blazor Server, .NET 10)
- Navigate to http://localhost:PORT
  - **Root URL** (`/`): Shows navigation page with project info and links to all components
  - **Component URLs** (`/counter`, `/dashboard`, `/settings`): Individual components with full interactivity

### 2. Single File Components (Simple)

For quick prototyping or simple components, you can still serve individual `.cdz` files:

```bash
# Serve a single .cdz UI component
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj --serve examples/counter.cdz --port 5179

# Or serve the alternative BlazorCounter sample
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj --serve examples/BlazorCounter.cdz --port 5179

# Flags:
#   --serve <file.cdz>      Required; a single .cdz file containing component declaration(s)
#   --port <number>         Optional; default 5000. If in use, a nearby port is auto-selected
#   --no-open               Optional; do not auto-open browser
```

**What happens:**
- **Single component**: Component renders directly at `/` and also at `/{component-name-lower}` (e.g., `/counter`)
- **Multiple components in one file**: Navigation page shown at `/` with links to individual components

Troubleshooting:
- If the port is taken, the host will choose another and log it.
- If buttons render but do not respond, check the browser console and ensure the Blazor script “_framework/blazor.web.js” is loaded (200).
- Always serve via the host; opening generated files directly in a browser will not wire Blazor interactivity.

### 2. Create a new UI project (planned templates)

**main.cdz:**
```cadenza
// Simple counter application - LLM-friendly structure
component CounterApp() 
    uses [DOM] 
    state [count, message]
    events [on_increment, on_decrement, on_reset]
    -> UIComponent 
{
    // Explicit state declarations - LLMs see everything
    declare_state count: int = 0
    declare_state message: string = "Click to count!"
    
    // Explicit event handlers with clear effects
    event_handler handle_increment() uses [DOM] {
        set_state(count, count + 1)
        set_state(message, $"Count: {count}")
    }
    
    event_handler handle_decrement() uses [DOM] {
        set_state(count, count - 1)
        set_state(message, $"Count: {count}")
    }
    
    event_handler handle_reset() uses [DOM] {
        set_state(count, 0)
        set_state(message, "Reset!")
    }
    
    // Explicit render logic - no hidden behavior
    render {
        container(class: "counter-app") {
            heading(level: 1, text: "Cadenza Counter")
            text(content: message, class: "message")
            
            button_group(class: "controls") {
                button(
                    text: "-",
                    on_click: handle_decrement,
                    disabled: count <= 0
                )
                button(
                    text: "Reset", 
                    on_click: handle_reset
                )
                button(
                    text: "+",
                    on_click: handle_increment
                )
            }
        }
    }
}
```

### 3. Serve any UI component file

```bash
# Serve a different component file
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- --serve path/to/YourComponent.cdz --port 5180
```

Notes:
- The embedded host parses the .cdz file and generates corresponding Blazor components.
- Route mapping is name-based: component “Counter” -> route “/counter”.
- Root route “/” renders the only component if there is a single one; otherwise the host shows an index of components (planned multi-file support).

### 4. Manual transpilation to C# (advanced)

If you need the raw C# output of a .cdz file:

```bash
# Traditional transpilation to a .cs file
dotnet run --project src/Cadenza.Core/cadenzac-core.csproj -- examples/counter.cdz ./tmp-blazor-out/Counter.g.cs
```

Then include the generated C# into your own Blazor project manually. This flow is useful for custom integration, but for quick testing prefer --serve.

## Core Concepts for LLM Developers

### 🎯 **Explicit Component Structure**

Every Cadenza UI component follows the same predictable pattern:

```cadenza
component ComponentName(parameters) 
    uses [effects]           // Side effects used
    state [state_vars]       // State variables  
    events [event_handlers]  // Event handlers
    -> UIComponent 
{
    // 1. State declarations
    declare_state var: type = initial_value
    
    // 2. Lifecycle methods (optional)
    on_mount { /* initialization */ }
    
    // 3. Event handlers
    event_handler handle_event() uses [effects] {
        // Handler logic with explicit state updates
    }
    
    // 4. Render method
    render {
        // UI element tree
    }
}
```

### 🔒 **Type-Safe State Management**

```cadenza
// Global application state with explicit mutations
app_state TodoAppState uses [LocalStorage] {
    // Explicit state shape
    todos: List<Todo> = []
    filter: TodoFilter = TodoFilter.All
    
    // Named actions with explicit effects and updates
    action add_todo(text: string) 
        uses [LocalStorage] 
        updates [todos]
        -> Result<Unit, Error> 
    {
        let new_todo = Todo {
            id: generate_id(),
            text: text,
            completed: false,
            created_at: current_time()
        }
        
        let updated_todos = todos.append(new_todo)
        set_state(todos, updated_todos)
        storage_save("todos", updated_todos)
        
        return Ok(unit)
    }
    
    action toggle_todo(todo_id: string) 
        uses [LocalStorage] 
        updates [todos]
        -> Result<Unit, Error> 
    {
        let updated_todos = todos.map(todo -> 
            if todo.id == todo_id {
                todo with { completed: !todo.completed }
            } else {
                todo
            }
        )
        
        set_state(todos, updated_todos)
        storage_save("todos", updated_todos)
        
        return Ok(unit)
    }
}
```

### 🌐 **API Integration**

```cadenza
// Backend service definition
service TodoService uses [Database] {
    endpoint get_todos() -> Result<List<Todo>, ApiError>
    endpoint create_todo(text: string) -> Result<Todo, ApiError>
    endpoint update_todo(todo: Todo) -> Result<Todo, ApiError>
    endpoint delete_todo(id: string) -> Result<Unit, ApiError>
}

// Auto-generated frontend API client
api_client TodoApi from TodoService {
    base_url: "https://api.myapp.com"
    auth_type: bearer_token
    timeout: 30s
    
    // Type-safe methods automatically generated
    function get_todos() uses [Network] -> Result<List<Todo>, ApiError>
    function create_todo(text: string) uses [Network] -> Result<Todo, ApiError>
    function update_todo(todo: Todo) uses [Network] -> Result<Todo, ApiError>
    function delete_todo(id: string) uses [Network] -> Result<Unit, ApiError>
}

// Usage in components
component TodoList() uses [Network] -> UIComponent {
    declare_state todos: List<Todo> = []
    declare_state loading: bool = false
    
    on_mount {
        load_todos()
    }
    
    event_handler load_todos() uses [Network] {
        set_state(loading, true)
        
        let result = TodoApi.get_todos()
        match result {
            Ok(todos) -> {
                set_state(todos, todos)
                set_state(loading, false)
            }
            Error(api_error) -> {
                set_state(loading, false)
                show_error_message(api_error.message)
            }
        }
    }
}
```

## Advanced UI Patterns

### 🔄 **Conditional Rendering**

```cadenza
render {
    container(class: "todo-app") {
        if loading {
            loading_spinner(message: "Loading todos...")
        } else if todos.is_empty() {
            empty_state(
                message: "No todos yet",
                action_text: "Add your first todo",
                on_action: handle_add_todo
            )
        } else {
            todo_list(
                todos: filtered_todos,
                on_toggle: handle_toggle_todo,
                on_delete: handle_delete_todo
            )
        }
    }
}
```

### 📋 **List Rendering**

```cadenza
render {
    todo_list_container {
        for todo in todos {
            todo_item(
                todo: todo,
                on_toggle: () -> handle_toggle_todo(todo.id),
                on_delete: () -> handle_delete_todo(todo.id),
                on_edit: (new_text) -> handle_edit_todo(todo.id, new_text)
            )
        }
    }
}
```

### 🎨 **Styling and CSS Classes**

```cadenza
render {
    container(
        class: build_classes("todo-item", todo.completed ? "completed" : "pending"),
        data_id: todo.id
    ) {
        checkbox(
            checked: todo.completed,
            on_change: handle_toggle
        )
        
        text_input(
            value: todo.text,
            class: "todo-text",
            readonly: !edit_mode,
            on_change: handle_text_change
        )
        
        action_buttons(class: "todo-actions") {
            button(
                text: edit_mode ? "Save" : "Edit",
                class: "btn-edit",
                on_click: handle_edit_toggle
            )
            button(
                text: "Delete",
                class: "btn-delete btn-danger",
                on_click: handle_delete
            )
        }
    }
}

// Helper function for dynamic CSS classes
pure function build_classes(base: string, modifier: string) -> string {
    return base + " " + modifier
}
```

## Full-Stack Application Example

### Project Structure

```
my-fullstack-app/
├── cadenzac.json
├── shared/
│   └── types/
│       ├── User.cdz
│       ├── Todo.cdz
│       └── ApiErrors.cdz
├── backend/
│   ├── services/
│   │   ├── UserService.cdz
│   │   └── TodoService.cdz
│   └── main.cdz
├── frontend/
│   ├── components/
│   │   ├── TodoApp.cdz
│   │   ├── TodoList.cdz
│   │   └── TodoItem.cdz
│   ├── state/
│   │   └── AppState.cdz
│   └── main.cdz
```

### Shared Types

**shared/types/Todo.cdz:**
```cadenza
type Todo {
    id: string
    text: string
    completed: bool
    created_at: DateTime
    user_id: string
}

type TodoFilter {
    All,
    Completed,
    Pending
}
```

### Backend Service

**backend/services/TodoService.cdz:**
```cadenza
service TodoService uses [Database, Logging] {
    endpoint get_todos(user_id: string) -> Result<List<Todo>, ApiError>
    endpoint create_todo(user_id: string, text: string) -> Result<Todo, ApiError>
    endpoint update_todo(todo: Todo) -> Result<Todo, ApiError>
    endpoint delete_todo(todo_id: string) -> Result<Unit, ApiError>
}

function get_todos(user_id: string) uses [Database, Logging] -> Result<List<Todo>, ApiError> {
    log_info($"Fetching todos for user: {user_id}")
    
    let todos = database_query("SELECT * FROM todos WHERE user_id = ?", [user_id])
    match todos {
        Ok(rows) -> {
            let parsed_todos = rows.map(row -> parse_todo_from_row(row))
            return Ok(parsed_todos)
        }
        Error(db_error) -> {
            log_error($"Failed to fetch todos: {db_error}")
            return Error(ApiError.DatabaseError(db_error))
        }
    }
}
```

### Frontend Components

**frontend/components/TodoApp.cdz:**
```cadenza
component TodoApp(user_id: string) 
    uses [Network, LocalStorage, DOM] 
    state [todos, filter, loading, error_message]
    events [on_add_todo, on_filter_change, on_todo_action]
    -> UIComponent 
{
    declare_state todos: List<Todo> = []
    declare_state filter: TodoFilter = TodoFilter.All
    declare_state loading: bool = false
    declare_state error_message: Option<string> = None
    
    on_mount {
        load_todos()
    }
    
    event_handler load_todos() uses [Network] {
        set_state(loading, true)
        set_state(error_message, None)
        
        let result = TodoApi.get_todos(user_id)
        match result {
            Ok(fetched_todos) -> {
                set_state(todos, fetched_todos)
                set_state(loading, false)
            }
            Error(api_error) -> {
                set_state(loading, false)
                set_state(error_message, Some(api_error.message))
            }
        }
    }
    
    event_handler handle_add_todo(text: string) uses [Network] {
        let result = TodoApi.create_todo(user_id, text)
        match result {
            Ok(new_todo) -> {
                let updated_todos = todos.append(new_todo)
                set_state(todos, updated_todos)
            }
            Error(api_error) -> {
                set_state(error_message, Some($"Failed to add todo: {api_error.message}"))
            }
        }
    }
    
    render {
        todo_app_container(class: "todo-app") {
            app_header {
                heading(level: 1, text: "Cadenza Todo App")
                todo_filter(
                    current_filter: filter,
                    on_change: (new_filter) -> set_state(filter, new_filter)
                )
            }
            
            if error_message.is_some() {
                error_banner(
                    message: error_message.unwrap(),
                    on_dismiss: () -> set_state(error_message, None)
                )
            }
            
            todo_input_section {
                add_todo_form(
                    on_submit: handle_add_todo,
                    disabled: loading
                )
            }
            
            main_content {
                if loading {
                    loading_indicator(message: "Loading todos...")
                } else {
                    todo_list(
                        todos: filter_todos(todos, filter),
                        on_toggle: handle_toggle_todo,
                        on_delete: handle_delete_todo,
                        on_edit: handle_edit_todo
                    )
                }
            }
        }
    }
}
```

## CLI Commands

### Development Workflow

```bash
# Create new projects
cadenzac new --template ui my-ui-app
cadenzac new --template fullstack my-fullstack-app

# Compile to different targets
cadenzac compile --target csharp frontend/main.cdz # UI components (marked with -> UIComponent) will be compiled to Blazor-compatible C#
cadenzac compile --target csharp backend/main.cdz

# List available targets
cadenzac targets

# Development server (coming soon)
cadenzac dev --watch
```

### Project Templates

**UI Template:**
- Basic Blazor application structure (C#)
- Example components and state management
- Build configuration for Blazor

**Fullstack Template:**
- Backend services in C#/.NET
- Frontend components in C# (Blazor)
- Shared type definitions
- API client auto-generation

## Best Practices for LLM Development

### 1. **Always Declare Everything Explicitly**

```cadenza
// ✅ GOOD - LLM can see all aspects
component UserProfile(user_id: string) 
    uses [Network, LocalStorage] 
    state [user_data, loading, edit_mode]
    events [on_edit, on_save, on_cancel]
    -> UIComponent

// ❌ BAD - LLM must guess
component UserProfile(user_id: string) -> UIComponent
```

### 2. **Predictable Event Handler Naming**

```cadenza
// ✅ GOOD - Consistent patterns LLMs can predict
event_handler handle_user_login(credentials: LoginCredentials)
event_handler handle_form_submit(form_data: FormData)
event_handler handle_button_click(button_id: string)

// ❌ BAD - Inconsistent naming
event_handler userLogin(creds: LoginCredentials)
event_handler onSubmit(data: FormData)
event_handler clickBtn(id: string)
```

### 3. **Explicit Error Handling**

```cadenza
// ✅ GOOD - Every error case handled explicitly
let api_result = TodoApi.create_todo(text)
match api_result {
    Ok(new_todo) -> {
        set_state(todos, todos.append(new_todo))
        show_success_message("Todo added successfully")
    }
    Error(ValidationError(msg)) -> {
        set_state(form_errors, [msg])
    }
    Error(NetworkError(msg)) -> {
        show_error_message($"Network error: {msg}")
    }
    Error(ServerError(msg)) -> {
        show_error_message($"Server error: {msg}")
    }
}

// ❌ BAD - Generic error handling
let result = TodoApi.create_todo(text)
if result.is_error() {
    show_error_message("Something went wrong")
}
```

### 4. **State Updates Are Always Explicit**

```cadenza
// ✅ GOOD - Every state change is visible
event_handler handle_login_success(user: User) uses [LocalStorage] {
    set_state(current_user, Some(user))
    set_state(is_authenticated, true)
    set_state(login_error, None)
    storage_save("auth_token", user.auth_token)
    navigate_to("/dashboard")
}

// ❌ BAD - Hidden mutations
event_handler handle_login_success(user: User) {
    authenticateUser(user) // What does this do?
}
```

## Next Steps

1. Run the embedded host: see “Run the Counter sample in server mode (Blazor host)” above.
2. Explore examples in the examples/ folder, especially [`examples/counter.cdz`](examples/counter.cdz:1).
3. Read how components are mapped and routed in the host; routes are derived from component names.
4. Use browser DevTools Network and Console to verify the Blazor runtime script loads and that clicks dispatch without errors.
5. For deeper integration, transpile to C# and embed in your own Blazor project.

Cadenza UI components make frontend development predictable, type-safe, and LLM-friendly. Every aspect of your application is explicit and traceable, making it perfect for AI-assisted development! 🎉