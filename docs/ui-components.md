# Cadenza UI Components - LLM Developer Guide 🤖

**Cadenza UI components are designed specifically for LLM developers with maximum explicitness, predictability, and zero ambiguity.**

## Core Principles for LLM Development

### 🎯 **Explicit Everything**
Every UI interaction, state change, and side effect is explicitly declared. LLMs never need to guess or infer behavior.

### 🔒 **One Way To Do Things**  
Single patterns for components, state management, and event handling eliminate choice paralysis for LLMs.

### 📝 **Self-Documenting**
Code structure serves as complete documentation. LLMs can understand the entire component from its declaration.

### 🔍 **Predictable Patterns**
Consistent syntax across all components ensures LLMs can reliably generate correct code.

## Component Declaration Syntax

### Basic Component Structure

```cadenza
component ComponentName(parameters) 
    uses [effects] 
    state [state_variables]
    events [event_handlers]
    -> UIComponent 
{
    // Explicit state declarations
    declare_state variable_name: type = initial_value
    
    // Lifecycle methods (optional)
    on_mount {
        // Initialization logic with explicit effects
    }
    
    // Event handlers
    event_handler handler_name(parameters) uses [effects] {
        // Handler logic with explicit state updates
    }
    
    // Explicit render logic
    render {
        // UI element tree with explicit attributes and events
    }
}
```

### Complete Example - User Profile Component

```cadenza
// Every aspect is explicitly declared for maximum LLM clarity
component UserProfileCard(user_id: string) 
    uses [Network, LocalStorage, DOM] 
    state [loading, user_data, edit_mode, error_message]
    events [on_edit_click, on_save_click, on_cancel_click]
    -> UIComponent 
{
    // Explicit state declarations - LLMs see all state shape
    declare_state loading: bool = false
    declare_state user_data: Option<User> = None
    declare_state edit_mode: bool = false
    declare_state error_message: Option<string> = None
    
    // Explicit lifecycle - no hidden behavior
    on_mount {
        set_state(loading, true)
        let result = api_fetch_user(user_id)
        match result {
            Ok(user) -> {
                set_state(user_data, Some(user))
                set_state(loading, false)
            }
            Error(err) -> {
                set_state(error_message, Some(err))
                set_state(loading, false)
            }
        }
    }
    
    // Explicit event handlers with clear effects
    event_handler handle_edit_click() uses [DOM] {
        set_state(edit_mode, true)
        set_state(error_message, None)
    }
    
    event_handler handle_save_user(updated_user: User) uses [Network, DOM] {
        set_state(loading, true)
        let result = api_update_user(updated_user)
        match result {
            Ok(saved_user) -> {
                set_state(user_data, Some(saved_user))
                set_state(edit_mode, false)
                set_state(loading, false)
            }
            Error(err) -> {
                set_state(error_message, Some(err))
                set_state(loading, false)
            }
        }
    }
    
    event_handler handle_cancel_edit() uses [DOM] {
        set_state(edit_mode, false)
        set_state(error_message, None)
    }
    
    // Explicit render logic - conditional rendering is obvious
    render {
        container(class: "user-profile-card") {
            if loading {
                loading_spinner(text: "Loading user data...")
            } else if error_message.is_some() {
                error_display(
                    message: error_message.unwrap(),
                    on_retry: handle_edit_click
                )
            } else if user_data.is_some() {
                let user = user_data.unwrap()
                if edit_mode {
                    user_edit_form(
                        user: user,
                        on_save: handle_save_user,
                        on_cancel: handle_cancel_edit
                    )
                } else {
                    user_display_view(
                        user: user,
                        on_edit: handle_edit_click
                    )
                }
            } else {
                empty_state(message: "No user data available")
            }
        }
    }
}
```

## State Management

### App-Level State Declaration

```cadenza
// Global application state with explicit shape and mutations
app_state AppState uses [LocalStorage, Network] {
    // Explicit state shape - LLMs can see everything
    current_user: Option<User> = None
    shopping_cart: Cart = Cart.empty()
    ui_notifications: List<Notification> = []
    loading_states: Map<string, bool> = Map.empty()
    api_cache: Map<string, ApiResponse> = Map.empty()
    
    // Every state change is a named action with explicit effects
    action login_user(credentials: LoginCredentials) 
        uses [Network, LocalStorage] 
        updates [current_user, loading_states]
        -> Result<User, AuthError> 
    {
        // Set loading state explicitly
        set_loading("user_login", true)
        
        // Make API call with explicit error handling
        let auth_result = api_authenticate(credentials)
        match auth_result {
            Ok(user) -> {
                // Update state explicitly
                set_state(current_user, Some(user))
                // Persist to local storage explicitly
                storage_save("current_user", user)
                set_loading("user_login", false)
                return Ok(user)
            }
            Error(auth_error) -> {
                set_loading("user_login", false)
                return Error(auth_error)
            }
        }
    }
    
    action add_to_cart(product: Product, quantity: int)
        uses [LocalStorage]
        updates [shopping_cart]
        -> Result<Unit, CartError>
    {
        // Explicit validation
        guard quantity > 0 else {
            return Error(CartError.InvalidQuantity)
        }
        
        guard product.available else {
            return Error(CartError.ProductUnavailable)
        }
        
        // Explicit state update
        let updated_cart = shopping_cart.add_item(product, quantity)
        set_state(shopping_cart, updated_cart)
        
        // Explicit persistence
        storage_save("shopping_cart", updated_cart)
        
        return Ok(unit)
    }
    
    action add_notification(message: string, type: NotificationType)
        uses [LocalStorage]
        updates [ui_notifications]
        -> Result<Unit, Error>
    {
        let notification = Notification {
            id: generate_id(),
            message: message,
            type: type,
            timestamp: current_time()
        }
        
        let updated_notifications = ui_notifications.prepend(notification)
        set_state(ui_notifications, updated_notifications)
        
        // Auto-remove after 5 seconds
        schedule_removal(notification.id, 5000)
        
        return Ok(unit)
    }
}
```

## API Client Generation

### Backend Service Definition

```cadenza
// Backend service definition - will auto-generate frontend client
service UserService uses [Database, Logging] {
    endpoint get_user(id: string) -> Result<User, UserError>
    endpoint update_user(user: User) -> Result<User, UserError>  
    endpoint delete_user(id: string) -> Result<Unit, UserError>
    endpoint list_users(filter: UserFilter, pagination: Pagination) -> Result<UserList, UserError>
    endpoint search_users(query: string, limit: int) -> Result<List<User>, UserError>
}

service ProductService uses [Database, Search, Logging] {
    endpoint get_product(id: string) -> Result<Product, ProductError>
    endpoint search_products(query: SearchQuery) -> Result<ProductSearchResult, ProductError>
    endpoint get_categories() -> Result<List<Category>, ProductError>
}
```

### Auto-Generated Frontend API Client

```cadenza
// Auto-generated from backend services - completely predictable for LLMs
api_client UserApi from UserService {
    base_url: "https://api.myapp.com"
    auth_type: bearer_token
    timeout: 30s
    retry_policy: exponential_backoff(max_attempts: 3)
    cache_policy: cache_get_requests(ttl: 300s)
    
    // LLMs get exact function signatures with explicit effects
    function get_user(id: string) uses [Network] -> Result<User, ApiError>
    function update_user(user: User) uses [Network] -> Result<User, ApiError>
    function delete_user(id: string) uses [Network] -> Result<Unit, ApiError>
    function list_users(filter: UserFilter, pagination: Pagination) uses [Network] -> Result<UserList, ApiError>
    function search_users(query: string, limit: int) uses [Network] -> Result<List<User>, ApiError>
}

api_client ProductApi from ProductService {
    base_url: "https://api.myapp.com"
    auth_type: bearer_token
    timeout: 45s
    
    function get_product(id: string) uses [Network] -> Result<Product, ApiError>
    function search_products(query: SearchQuery) uses [Network] -> Result<ProductSearchResult, ApiError>
    function get_categories() uses [Network] -> Result<List<Category>, ApiError>
}
```

## UI Element Reference

### Container Elements

```cadenza
// Basic container with explicit styling
container(class: "my-container", id: "main") {
    // Child elements
}

// Grid layout container
grid_container(columns: 3, gap: "16px") {
    // Grid items
}

// Flex container  
flex_container(direction: "row", justify: "space-between") {
    // Flex items
}
```

### Form Elements

```cadenza
// Input field with explicit validation
text_input(
    value: form_data.username,
    placeholder: "Enter username",
    on_change: handle_username_change,
    validation: validate_username,
    error: form_errors.username
)

// Button with explicit click handler
button(
    text: "Save User",
    type: "primary",
    disabled: is_saving,
    on_click: handle_save_user
)

// Select dropdown with explicit options
select_dropdown(
    value: selected_category,
    options: category_options,
    on_change: handle_category_change,
    placeholder: "Select category"
)
```

### Display Elements

```cadenza
// Text display with explicit formatting
text(content: user.name, style: "heading-large")

// Image with explicit loading states
image(
    src: user.avatar_url,
    alt: $"Avatar for {user.name}",
    loading_placeholder: avatar_placeholder,
    error_placeholder: default_avatar
)

// List with explicit item rendering
list(items: user_list) { user ->
    user_list_item(
        user: user,
        on_click: () -> navigate_to_user(user.id)
    )
}
```

## Event Handling

### Explicit Event Declaration

```cadenza
// Every event handler must be explicitly declared
event_handler handle_form_submit(form_data: FormData) uses [Network, DOM] {
    // Explicit validation
    let validation_result = validate_form_data(form_data)
    match validation_result {
        Ok(valid_data) -> {
            // Explicit loading state
            set_state(is_submitting, true)
            
            // Explicit API call
            let submit_result = api_submit_form(valid_data)
            match submit_result {
                Ok(response) -> {
                    set_state(is_submitting, false)
                    set_state(submit_success, true)
                    navigate_to("/success")
                }
                Error(api_error) -> {
                    set_state(is_submitting, false)
                    set_state(form_errors, api_error.field_errors)
                }
            }
        }
        Error(validation_errors) -> {
            set_state(form_errors, validation_errors)
        }
    }
}

// Mouse events with explicit parameters
event_handler handle_item_hover(item: ListItem, mouse_position: Point) uses [DOM] {
    set_state(hovered_item, Some(item))
    show_tooltip(item.description, mouse_position)
}

// Keyboard events with explicit key handling
event_handler handle_key_press(key: KeyEvent) uses [DOM] {
    match key.code {
        "Enter" -> handle_form_submit()
        "Escape" -> handle_cancel_action()
        _ -> {} // Ignore other keys
    }
}
```

## Conditional Rendering

### Explicit Conditional Logic

```cadenza
render {
    container(class: "app-container") {
        // Explicit loading state
        if is_loading {
            loading_screen(message: "Loading application...")
        } else if current_user.is_some() {
            // User is logged in
            let user = current_user.unwrap()
            authenticated_app(user: user)
        } else if has_auth_error {
            // Authentication failed
            error_screen(
                message: "Authentication failed",
                on_retry: handle_login_retry
            )
        } else {
            // User not logged in
            login_screen(on_login: handle_user_login)
        }
    }
}
```

### List Rendering with Explicit Iteration

```cadenza
render {
    container(class: "product-grid") {
        if product_list.is_empty() {
            empty_state(
                message: "No products found",
                action_text: "Browse categories",
                on_action: navigate_to_categories
            )
        } else {
            // Explicit iteration with item rendering
            for product in product_list {
                product_card(
                    product: product,
                    on_add_to_cart: (quantity) -> handle_add_to_cart(product, quantity),
                    on_view_details: () -> navigate_to_product(product.id)
                )
            }
        }
    }
}
```

## Effect System Integration

### Frontend Effects

```cadenza
// All frontend effects are explicitly tracked
component ShoppingCart() uses [LocalStorage, Network, DOM] -> UIComponent {
    declare_state cart_items: List<CartItem> = []
    declare_state total_price: float = 0.0
    declare_state is_syncing: bool = false
    
    on_mount {
        // LocalStorage effect - explicit data loading
        let stored_cart = storage_load("shopping_cart")
        match stored_cart {
            Some(cart) -> set_state(cart_items, cart.items)
            None -> {} // No stored cart
        }
        
        // Network effect - sync with server
        sync_cart_with_server()
    }
    
    event_handler handle_add_item(product: Product, quantity: int) uses [LocalStorage, Network] {
        // DOM effect - update UI immediately
        let updated_items = cart_items.add_item(product, quantity)
        set_state(cart_items, updated_items)
        
        // LocalStorage effect - persist changes
        storage_save("shopping_cart", Cart { items: updated_items })
        
        // Network effect - sync with server
        api_update_cart(updated_items)
    }
    
    event_handler sync_cart_with_server() uses [Network] {
        set_state(is_syncing, true)
        
        let sync_result = api_get_cart()
        match sync_result {
            Ok(server_cart) -> {
                set_state(cart_items, server_cart.items)
                set_state(is_syncing, false)
            }
            Error(sync_error) -> {
                // Handle sync failure gracefully
                set_state(is_syncing, false)
                show_notification("Cart sync failed", NotificationType.Warning)
            }
        }
    }
}
```

## Full-Stack Type Safety

### Shared Type Definitions

```cadenza
// types/User.cdz - shared between backend and frontend
type User {
    id: string
    username: string
    email: string
    full_name: string
    avatar_url: Option<string>
    created_at: DateTime
    last_login: Option<DateTime>
    is_active: bool
}

type UserFilter {
    search_query: Option<string>
    is_active: Option<bool>
    created_after: Option<DateTime>
    sort_by: UserSortOption
}

enum UserSortOption {
    CreatedAt,
    LastLogin, 
    Username,
    Email
}

// Result types for API responses
type UserError {
    NotFound,
    InvalidInput(field: string, message: string),
    PermissionDenied,
    ServerError(message: string)
}
```

### Generated TypeScript Definitions

```typescript
// Auto-generated from Cadenza types - perfect for editor support
export interface User {
    id: string;
    username: string;
    email: string;
    full_name: string;
    avatar_url: string | null;
    created_at: Date;
    last_login: Date | null;
    is_active: boolean;
}

export interface UserFilter {
    search_query: string | null;
    is_active: boolean | null;
    created_after: Date | null;
    sort_by: UserSortOption;
}

export enum UserSortOption {
    CreatedAt = "CreatedAt",
    LastLogin = "LastLogin",
    Username = "Username", 
    Email = "Email"
}
```

## Development Workflow

### Project Structure

```
my-cadenza-app/
├── cadenzac.json                      # Single configuration file
├── types/                          # Shared types (backend + frontend)
│   ├── User.cdz
│   ├── Product.cdz
│   ├── ApiResponses.cdz
│   └── Errors.cdz
├── backend/
│   ├── services/                   # Business logic services
│   │   ├── UserService.cdz
│   │   ├── ProductService.cdz
│   │   └── AuthService.cdz
│   ├── data/                       # Database layer
│   │   ├── UserRepository.cdz
│   │   └── ProductRepository.cdz
│   └── main.cdz                   # Backend entry point
├── frontend/
│   ├── components/                 # UI components
│   │   ├── UserProfileCard.cdz
│   │   ├── ProductList.cdz
│   │   ├── ShoppingCart.cdz
│   │   └── LoginForm.cdz
│   ├── state/                      # State management
│   │   ├── AppState.cdz
│   │   ├── UserState.cdz
│   │   └── CartState.cdz
│   ├── api/                        # Auto-generated API clients
│   │   └── generated_clients.cdz
│   └── main.cdz                   # Frontend entry point
└── docs/
    └── generated_api_docs.md       # Auto-generated documentation
```

### CLI Commands for Full-Stack Development

```bash
# Create new full-stack project with LLM-optimized structure
cadenzac new --template fullstack-llm my-app

# Development mode with hot reload and effect tracking
cadenzac dev --watch --verbose-effects
# Starts:
# - Backend server (C#/.NET) on :8080 
# - Frontend dev server (Vite/React) on :3000
# - Type sync between backend/frontend
# - Real-time effect monitoring
# - Auto-restart on file changes

# Build for production with optimization
cadenzac build --target production --optimize
# Generates:
# - C# backend optimized for deployment
# - JavaScript frontend bundle
# - TypeScript definitions
# - API documentation

# Type checking across entire stack
cadenzac check --full-stack
# Validates:
# - Type consistency between backend and frontend
# - API contract compliance
# - Effect system integrity

# Generate API documentation
cadenzac docs --generate-api
# Creates:
# - API endpoint documentation
# - Type definitions
# - Usage examples
# - Integration guides
```

## LLM Development Best Practices

### 1. **Always Declare Everything Explicitly**

```cadenza
// ✅ GOOD - LLM can see all component aspects
component ProductCard(product: Product) 
    uses [Network, LocalStorage] 
    state [is_favorited, is_loading]
    events [on_favorite_click, on_buy_click]
    -> UIComponent

// ❌ BAD - LLM must guess what state/effects exist
component ProductCard(product: Product) -> UIComponent
```

### 2. **Use Predictable Naming Patterns**

```cadenza
// ✅ GOOD - Consistent naming LLMs can predict
event_handler handle_user_login(credentials: LoginCredentials)
event_handler handle_form_submit(form_data: FormData)
event_handler handle_item_click(item: ListItem)

// ❌ BAD - Inconsistent naming confuses LLMs
event_handler userLogin(creds: LoginCredentials)
event_handler onSubmit(data: FormData)  
event_handler clickItem(item: ListItem)
```

### 3. **Explicit Error Handling**

```cadenza
// ✅ GOOD - Every error case is explicitly handled
let api_result = api_get_user(user_id)
match api_result {
    Ok(user) -> {
        set_state(user_data, Some(user))
        set_state(loading, false)
    }
    Error(NotFound) -> {
        set_state(error_message, Some("User not found"))
        set_state(loading, false)
    }
    Error(PermissionDenied) -> {
        set_state(error_message, Some("Access denied"))
        set_state(loading, false)
    }
    Error(ServerError(msg)) -> {
        set_state(error_message, Some($"Server error: {msg}"))
        set_state(loading, false)
    }
}

// ❌ BAD - Generic error handling hides important cases
let api_result = api_get_user(user_id)
if api_result.is_error() {
    set_state(error_message, Some("Something went wrong"))
}
```

### 4. **State Updates Are Always Explicit**

```cadenza
// ✅ GOOD - Every state change is visible and intentional
event_handler handle_login_success(user: User) uses [LocalStorage] {
    set_state(current_user, Some(user))
    set_state(is_authenticated, true)
    set_state(login_error, None)
    storage_save("auth_token", user.auth_token)
    navigate_to("/dashboard")
}

// ❌ BAD - Hidden state mutations confuse LLMs
event_handler handle_login_success(user: User) {
    authenticateUser(user) // What does this do? LLM can't tell
}
```

## Generated JavaScript Output

The Cadenza UI component above generates clean, predictable JavaScript:

```javascript
// Cadenza Generated JavaScript - LLM Optimized
import { createElement, useState, useEffect } from 'react';
import { CadenzaRuntime, Result } from '@cadenza/runtime';

// Cadenza Component: UserProfileCard
// Effects: [Network, LocalStorage, DOM]
// State: [loading, user_data, edit_mode, error_message]
// Events: [on_edit_click, on_save_click, on_cancel_click]
export function UserProfileCard({user_id}) {
    // State: loading (bool)
    const [loading, setLoading] = useState(false);
    // State: user_data (Option<User>)
    const [userData, setUserData] = useState(null);
    // State: edit_mode (bool)
    const [editMode, setEditMode] = useState(false);
    // State: error_message (Option<string>)
    const [errorMessage, setErrorMessage] = useState(null);

    // Component mount lifecycle
    useEffect(() => {
        // On mount logic
        setLoading(true);
        const result = api_fetch_user(user_id);
        if (result.isOk) {
            setUserData(result.value);
            setLoading(false);
        } else {
            setErrorMessage(result.error);
            setLoading(false);
        }
    }, []);

    // Event handler: handle_edit_click
    // Effects: [DOM]
    const handleEditClick = () => {
        setEditMode(true);
        setErrorMessage(null);
    };

    // Event handler: handle_save_user
    // Effects: [Network, DOM]
    const handleSaveUser = (updatedUser) => {
        setLoading(true);
        const result = api_update_user(updatedUser);
        if (result.isOk) {
            setUserData(result.value);
            setEditMode(false);
            setLoading(false);
        } else {
            setErrorMessage(result.error);
            setLoading(false);
        }
    };

    // Explicit render logic - LLM friendly
    return (
        <div className="user-profile-card">
            {loading ? (
                <LoadingSpinner text="Loading user data..." />
            ) : errorMessage ? (
                <ErrorDisplay message={errorMessage} onRetry={handleEditClick} />
            ) : userData ? (
                editMode ? (
                    <UserEditForm 
                        user={userData}
                        onSave={handleSaveUser}
                        onCancel={() => setEditMode(false)}
                    />
                ) : (
                    <UserDisplayView 
                        user={userData}
                        onEdit={handleEditClick}
                    />
                )
            ) : (
                <EmptyState message="No user data available" />
            )}
        </div>
    );
}
```

This documentation ensures LLMs can reliably understand and generate Cadenza UI components with complete predictability and explicit behavior at every level.