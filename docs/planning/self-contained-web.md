# Cadenza Self-Contained Web Runtime Specification

## 1. Introduction and Vision

### 1.1 Core Philosophy

**LLM-Centric Web Development:** Cadenza's self-contained web runtime enables Large Language Models to create complete, deployable web applications using only Cadenza syntax, without requiring knowledge of HTML, CSS, JavaScript, web servers, build tools, or deployment infrastructure.

**Zero-Configuration Principle:** From a single Cadenza file, developers (including LLMs) should be able to generate a production-ready web application with a single command: `cadenzac --serve app.cdz`.

**Abstraction Without Lock-in:** While hiding web development complexity, the runtime should generate standard web technologies (HTML, CSS, JavaScript) that can be inspected, customized, or migrated if needed.

### 1.2 Current State vs. Target State

**Current State (Phase 2 Complete):**
- Cadenza components → Blazor C# classes
- Manual Blazor project setup required
- Requires web development knowledge for deployment

**Target State (Self-Contained Runtime):**
- Cadenza components → Complete web application
- Embedded web server with automatic routing
- Zero web development knowledge required
- One-command deployment to production

### 1.3 Success Criteria

1. **Single Command Deployment:** `cadenzac --serve myapp.cdz` produces a working web application
2. **LLM Friendly:** An LLM can create complex web applications knowing only Cadenza syntax
3. **Production Ready:** Generated applications can be deployed to production environments
4. **Progressive Enhancement:** Simple apps start simple, complex features available when needed
5. **Standard Output:** Generated applications use standard web technologies for inspection/migration

## 2. Architecture Overview

### 2.1 Runtime Components

**Cadenza Web Runtime Architecture:**

```
┌─────────────────────────────────────────────────────────────┐
│                    Cadenza Web Runtime                      │
├─────────────────────────────────────────────────────────────┤
│  CLI Interface (cadenzac --serve, --export, --deploy)      │
├─────────────────────────────────────────────────────────────┤
│  Project Generator (Auto-generates web app structure)      │
├─────────────────────────────────────────────────────────────┤
│  Routing Engine (File-system based, automatic)             │
├─────────────────────────────────────────────────────────────┤
│  Style System (Semantic CSS generation)                    │
├─────────────────────────────────────────────────────────────┤
│  Component Transpiler (Existing Blazor generator)          │
├─────────────────────────────────────────────────────────────┤
│  Embedded Web Server (Kestrel-based)                       │
├─────────────────────────────────────────────────────────────┤
│  Static Asset Handler (Images, fonts, etc.)                │
├─────────────────────────────────────────────────────────────┤
│  Development Tools (Hot reload, debugging)                 │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Technology Stack

**Core Runtime:**
- **Language:** C# (.NET 8+)
- **Web Server:** ASP.NET Core Kestrel (embedded)
- **UI Framework:** Blazor Server (hidden from user)
- **Routing:** Custom file-system based router
- **Styling:** CSS-in-Cadenza with semantic naming

**Output Technologies:**
- **Frontend:** HTML5 + CSS3 + SignalR (Blazor Server)
- **Backend:** ASP.NET Core web application
- **Deployment:** Self-contained executable or Docker container

### 2.3 File Structure Conventions

**Single File Applications:**
```
myapp.cdz → Complete web application
```

**Multi-File Applications:**
```
src/
  pages/
    index.cdz      → http://localhost:3000/
    about.cdz      → http://localhost:3000/about
    blog/
      post.cdz     → http://localhost:3000/blog/post
  components/
    Header.cdz     → Reusable components
    Footer.cdz
  assets/
    logo.png       → Static assets
    styles.css     → Optional custom CSS
  cadenza.config.json → Project configuration
```

## 3. Technical Specifications

### 3.1 CLI Interface

**Primary Commands:**

```bash
# Development server (hot reload enabled)
cadenzac --serve app.cdz
cadenzac --serve src/ --port 3000

# Production build
cadenzac --build src/ --output dist/

# Static site export
cadenzac --export src/ --output static/

# Docker container generation
cadenzac --containerize src/ --tag myapp:latest

# Cloud deployment
cadenzac --deploy src/ --provider vercel|netlify|azure|aws
```

**Command Options:**
- `--port N`: Specify port (default: 3000)
- `--hot-reload`: Enable/disable hot reload (default: true in dev)
- `--open`: Automatically open browser
- `--verbose`: Detailed logging
- `--config path`: Custom configuration file

### 3.2 Project Generator

**Auto-Generated Files (Hidden from User):**

```csharp
// Program.cs (auto-generated)
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using CadenzaWebRuntime;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddCadenzaComponents(); // Auto-discovers components

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapCadenzaRoutes(); // Auto-generated routing
app.Run();
```

```html
<!-- App.razor (auto-generated) -->
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>@ViewData["Title"]</title>
    <base href="~/" />
    <link href="~/cadenza-runtime.css" rel="stylesheet" />
</head>
<body>
    <component type="typeof(App)" render-mode="ServerPrerendered" />
    <script src="_framework/blazor.server.js"></script>
</body>
</html>
```

### 3.3 Routing Engine

**File-System Based Routing:**

| Cadenza File | Generated Route | HTTP Endpoint |
|--------------|-----------------|---------------|
| `index.cdz` | `/` | `GET /` (UI Page) |
| `about.cdz` | `/about` | `GET /about` (UI Page) |
| `blog/post.cdz` | `/blog/post` | `GET /blog/post` (UI Page) |
| `api/users.cdz` | `/api/users` | `GET /api/users` (API Endpoint) |
| `api/users/[id].cdz` | `/api/users/{id}` | `GET /api/users/{id}` (API Endpoint) |

**Dynamic Routing:**
```cadenza
// pages/user/[id].cdz
page UserProfile(id: string) -> UIComponent {
    declare_state user: User? = null
    
    on_mount {
        user = fetch_user(id)
    }
    
    render {
        container {
            h1(text: user?.name ?? "Loading...")
            p(text: user?.email ?? "")
        }
    }
}
```

**Route Parameters:** Automatically extracted from URL and passed to component parameters.

### 3.4 API Endpoint System

**API-First Architecture:** The runtime serves both UI pages and API endpoints from the same codebase, enabling full-stack applications with a unified development experience.

**API Endpoint Syntax:**

```cadenza
// api/users.cdz - RESTful user management
api UserAPI() -> JSON {
    // GET /api/users - List all users
    endpoint get_users() uses [Database] -> Result<List<User>, APIError> {
        let users = db.query("SELECT * FROM users")
        return Ok(users)
    }
    
    // GET /api/users/{id} - Get specific user
    endpoint get_user(id: string) uses [Database] -> Result<User, APIError> {
        let user = db.query_single("SELECT * FROM users WHERE id = ?", id)
        if user == null {
            return Error(APIError.NotFound("User not found"))
        }
        return Ok(user)
    }
    
    // POST /api/users - Create new user
    endpoint create_user(request: CreateUserRequest) uses [Database] -> Result<User, APIError> {
        // Validate request
        let validation = validate_user(request)
        if !validation.is_valid {
            return Error(APIError.ValidationError(validation.errors))
        }
        
        // Create user
        let user = db.execute(
            "INSERT INTO users (name, email) VALUES (?, ?) RETURNING *",
            request.name, request.email
        )
        return Ok(user)
    }
    
    // PUT /api/users/{id} - Update user
    endpoint update_user(id: string, request: UpdateUserRequest) uses [Database] -> Result<User, APIError> {
        let existing = db.query_single("SELECT * FROM users WHERE id = ?", id)
        if existing == null {
            return Error(APIError.NotFound("User not found"))
        }
        
        let updated = db.execute(
            "UPDATE users SET name = ?, email = ? WHERE id = ? RETURNING *",
            request.name ?? existing.name,
            request.email ?? existing.email,
            id
        )
        return Ok(updated)
    }
    
    // DELETE /api/users/{id} - Delete user
    endpoint delete_user(id: string) uses [Database] -> Result<void, APIError> {
        let deleted = db.execute("DELETE FROM users WHERE id = ?", id)
        if deleted.rows_affected == 0 {
            return Error(APIError.NotFound("User not found"))
        }
        return Ok()
    }
}
```

**HTTP Method Detection:** Automatically inferred from endpoint name prefixes:
- `get_*` → GET
- `create_*` / `add_*` → POST  
- `update_*` / `edit_*` → PUT
- `delete_*` / `remove_*` → DELETE
- `*` (custom) → Can specify with `@method(POST)`

**Request/Response Types:**

```cadenza
// types/user.cdz - Shared between API and UI
type User {
    id: string
    name: string
    email: string
    created_at: DateTime
}

type CreateUserRequest {
    name: string
    email: string
}

type UpdateUserRequest {
    name: string?
    email: string?
}

type APIError {
    NotFound(message: string)
    ValidationError(errors: List<string>)
    InternalError(message: string)
    Unauthorized(message: string)
}
```

**Authentication & Authorization:**

```cadenza
// api/protected.cdz
api ProtectedAPI() -> JSON {
    // Require authentication for all endpoints
    @auth(required: true)
    
    // Role-based authorization
    @authorize(roles: ["admin"])
    endpoint get_admin_data() -> Result<AdminData, APIError> {
        return Ok(get_sensitive_data())
    }
    
    // User-specific authorization
    endpoint get_user_profile(user_id: string) -> Result<UserProfile, APIError> {
        // Automatic user context from JWT/session
        if current_user.id != user_id && !current_user.has_role("admin") {
            return Error(APIError.Unauthorized("Access denied"))
        }
        
        return Ok(get_user_profile(user_id))
    }
}
```

**Database Integration:**

```cadenza
// api/blog.cdz
api BlogAPI() uses [Database] -> JSON {
    endpoint get_posts(
        page: int = 1, 
        limit: int = 10,
        category: string? = null
    ) -> Result<PagedResult<BlogPost>, APIError> {
        let offset = (page - 1) * limit
        
        let query = "SELECT * FROM blog_posts WHERE published = true"
        let params = []
        
        if category != null {
            query += " AND category = ?"
            params.add(category)
        }
        
        query += " ORDER BY created_at DESC LIMIT ? OFFSET ?"
        params.add(limit, offset)
        
        let posts = db.query<BlogPost>(query, params)
        let total = db.query_scalar<int>(
            "SELECT COUNT(*) FROM blog_posts WHERE published = true"
        )
        
        return Ok(PagedResult {
            items: posts,
            total: total,
            page: page,
            limit: limit
        })
    }
}
```

**Generated API Documentation (Automatic):**

```json
// Auto-generated OpenAPI 3.0 specification
{
  "openapi": "3.0.0",
  "info": {
    "title": "My App API",
    "version": "1.0.0"
  },
  "paths": {
    "/api/users": {
      "get": {
        "summary": "List all users",
        "responses": {
          "200": {
            "description": "Success",
            "content": {
              "application/json": {
                "schema": {
                  "type": "array",
                  "items": { "$ref": "#/components/schemas/User" }
                }
              }
            }
          }
        }
      },
      "post": {
        "summary": "Create new user",
        "requestBody": {
          "content": {
            "application/json": {
              "schema": { "$ref": "#/components/schemas/CreateUserRequest" }
            }
          }
        }
      }
    }
  }
}
```

**Frontend Integration:**

```cadenza
// pages/users.cdz - UI component using API
page UsersPage() -> UIComponent {
    declare_state users: List<User> = []
    declare_state loading: bool = true
    declare_state error: string? = null
    
    // Type-safe API client (auto-generated)
    on_mount {
        try {
            let response = api.users.get_users()
            set_state(users, response)
        } catch (e) {
            set_state(error, e.message)
        } finally {
            set_state(loading, false)
        }
    }
    
    event_handler create_user(name: string, email: string) {
        try {
            let new_user = api.users.create_user(CreateUserRequest {
                name: name,
                email: email
            })
            set_state(users, users.append(new_user))
        } catch (e) {
            set_state(error, e.message)
        }
    }
    
    render {
        container {
            h1(text: "Users")
            
            if loading {
                loading_spinner()
            } else if error != null {
                error_message(text: error)
            } else {
                user_table {
                    for user in users {
                        user_row(key: user.id) {
                            cell(text: user.name)
                            cell(text: user.email)
                            cell {
                                Button(
                                    text: "Edit",
                                    on_click: () => edit_user(user.id)
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}
```

### 3.5 Semantic Styling System

**Built-in Design System:**

```cadenza
component Button(text: string, variant: string, size: string) -> UIComponent {
    styles {
        // Semantic styling - no CSS knowledge required
        base: {
            padding: "comfortable",     // → 12px 24px
            border_radius: "rounded",   // → 8px  
            font_weight: "medium",      // → 500
            transition: "smooth"        // → all 0.2s ease
        }
        
        variants: {
            primary: {
                background: "brand",        // → var(--brand-600)
                color: "brand_contrast",    // → white
                hover_background: "brand_dark" // → var(--brand-700)
            },
            secondary: {
                background: "surface",
                color: "text_primary",
                border: "subtle"
            }
        }
        
        sizes: {
            small: { padding: "compact", font_size: "small" },
            large: { padding: "spacious", font_size: "large" }
        }
    }
    
    render {
        button(
            text: text, 
            class: variant + " " + size,
            style: "button"
        )
    }
}
```

**Generated CSS (Automatic):**
```css
/* Auto-generated from semantic styling */
.button.primary.small {
    padding: 8px 16px;
    border-radius: 8px;
    font-weight: 500;
    background-color: var(--brand-600);
    color: white;
    font-size: 0.875rem;
    transition: all 0.2s ease;
}

.button.primary.small:hover {
    background-color: var(--brand-700);
}
```

**CSS Custom Properties (Auto-Generated):**
```css
:root {
    /* Brand Colors */
    --brand-50: #eff6ff;
    --brand-600: #2563eb;
    --brand-700: #1d4ed8;
    
    /* Semantic Tokens */
    --color-text-primary: var(--gray-900);
    --color-surface: var(--gray-50);
    --spacing-comfortable: 12px 24px;
    --radius-rounded: 8px;
}

@media (prefers-color-scheme: dark) {
    :root {
        --color-text-primary: var(--gray-100);
        --color-surface: var(--gray-800);
    }
}
```

### 3.6 Layout System

**Built-in Layout Components:**

```cadenza
// Automatic layout detection
layout AppLayout() -> UIComponent {
    render {
        page_structure {
            header(sticky: true) {
                nav() {
                    brand(text: "My App")
                    menu {
                        link(href: "/", text: "Home")
                        link(href: "/about", text: "About")
                    }
                }
            }
            
            main(padding: "comfortable") {
                // Page content inserted here automatically
                @RenderBody()
            }
            
            footer {
                text(text: "© 2025 My App")
            }
        }
    }
}

// pages/index.cdz - Automatically uses AppLayout
page HomePage() -> UIComponent {
    render {
        container {
            hero_section {
                h1(text: "Welcome to My App")
                p(text: "Built with Cadenza")
                Button(text: "Get Started", variant: "primary")
            }
        }
    }
}
```

**Generated HTML Structure:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>My App</title>
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <link rel="stylesheet" href="/cadenza-runtime.css">
</head>
<body>
    <div class="app-layout">
        <header class="header sticky">
            <nav class="nav">
                <div class="brand">My App</div>
                <ul class="menu">
                    <li><a href="/">Home</a></li>
                    <li><a href="/about">About</a></li>
                </ul>
            </nav>
        </header>
        
        <main class="main comfortable-padding">
            <div class="container">
                <section class="hero-section">
                    <h1>Welcome to My App</h1>
                    <p>Built with Cadenza</p>
                    <button class="button primary">Get Started</button>
                </section>
            </div>
        </main>
        
        <footer class="footer">
            <p>© 2025 My App</p>
        </footer>
    </div>
</body>
</html>
```

## 4. User Experience Flows

### 4.1 Complete Beginner Flow (LLM Perspective)

**Goal:** LLM creates a todo application

**Step 1: Single File Application**
```cadenza
// todo.cdz
page TodoApp() -> UIComponent {
    declare_state todos: List<string> = ["Learn Cadenza", "Build app"]
    declare_state new_todo: string = ""
    
    event_handler add_todo() {
        if new_todo.length > 0 {
            set_state(todos, todos.append(new_todo))
            set_state(new_todo, "")
        }
    }
    
    event_handler remove_todo(index: int) {
        set_state(todos, todos.remove_at(index))
    }
    
    render {
        container {
            header {
                h1(text: "My Todo App")
            }
            
            add_form {
                input(
                    value: new_todo,
                    placeholder: "Add a new todo...",
                    on_input: (event) => set_state(new_todo, event.value)
                )
                Button(
                    text: "Add Todo", 
                    variant: "primary",
                    on_click: add_todo
                )
            }
            
            todo_list {
                for (todo, index) in todos.enumerate() {
                    todo_item(key: index) {
                        text(text: todo)
                        Button(
                            text: "Remove", 
                            variant: "secondary",
                            size: "small",
                            on_click: () => remove_todo(index)
                        )
                    }
                }
            }
        }
    }
}
```

**Step 2: Single Command Deployment**
```bash
$ cadenzac --serve todo.cdz
✓ Compiling Cadenza components...
✓ Generating web application...
✓ Starting development server...

🚀 Todo app running at http://localhost:3000
```

**Result:** Fully functional todo application accessible in web browser, with no web development knowledge required.

### 4.2 Full-Stack Application Flow (API + UI)

**Goal:** LLM creates a complete blog application with API and UI

**Project Structure:**
```
blog-app/
  src/
    types/
      blog.cdz           # Shared types
    api/
      posts.cdz          # Blog API endpoints
      comments.cdz       # Comments API
    pages/
      index.cdz          # Blog home page
      post/[slug].cdz    # Individual blog post
      admin/
        posts.cdz        # Admin interface
    components/
      BlogPost.cdz       # UI components
      CommentForm.cdz
    cadenza.config.json
```

**Shared Types:**
```cadenza
// types/blog.cdz
type BlogPost {
    id: string
    title: string
    slug: string
    content: string
    excerpt: string
    published: bool
    created_at: DateTime
    updated_at: DateTime
    author: User
}

type Comment {
    id: string
    post_id: string
    author_name: string
    author_email: string
    content: string
    created_at: DateTime
}

type CreatePostRequest {
    title: string
    content: string
    published: bool
}
```

**API Implementation:**
```cadenza
// api/posts.cdz
api BlogAPI() uses [Database] -> JSON {
    // Public endpoints
    endpoint get_posts(
        published_only: bool = true,
        limit: int = 10,
        offset: int = 0
    ) -> Result<List<BlogPost>, APIError> {
        let query = "SELECT * FROM blog_posts"
        let params = []
        
        if published_only {
            query += " WHERE published = true"
        }
        
        query += " ORDER BY created_at DESC LIMIT ? OFFSET ?"
        params.add(limit, offset)
        
        let posts = db.query<BlogPost>(query, params)
        return Ok(posts)
    }
    
    endpoint get_post(slug: string) -> Result<BlogPost, APIError> {
        let post = db.query_single<BlogPost>(
            "SELECT * FROM blog_posts WHERE slug = ? AND published = true", 
            slug
        )
        if post == null {
            return Error(APIError.NotFound("Post not found"))
        }
        return Ok(post)
    }
    
    // Admin endpoints (require authentication)
    @auth(required: true)
    @authorize(roles: ["admin", "editor"])
    endpoint create_post(request: CreatePostRequest) -> Result<BlogPost, APIError> {
        // Generate slug from title
        let slug = generate_slug(request.title)
        
        // Check if slug already exists
        let existing = db.query_single("SELECT id FROM blog_posts WHERE slug = ?", slug)
        if existing != null {
            return Error(APIError.ValidationError(["Slug already exists"]))
        }
        
        let post = db.execute(
            """INSERT INTO blog_posts (title, slug, content, published, author_id, created_at)
               VALUES (?, ?, ?, ?, ?, NOW()) RETURNING *""",
            request.title, slug, request.content, request.published, current_user.id
        )
        
        return Ok(post)
    }
    
    @auth(required: true)
    @authorize(roles: ["admin", "editor"])
    endpoint update_post(id: string, request: UpdatePostRequest) -> Result<BlogPost, APIError> {
        let post = db.execute(
            "UPDATE blog_posts SET title = ?, content = ?, published = ?, updated_at = NOW() WHERE id = ? RETURNING *",
            request.title, request.content, request.published, id
        )
        return Ok(post)
    }
}
```

**Frontend Pages:**
```cadenza
// pages/index.cdz - Blog home page
page BlogHome() -> UIComponent {
    declare_state posts: List<BlogPost> = []
    declare_state loading: bool = true
    declare_state error: string? = null
    
    on_mount {
        load_posts()
    }
    
    event_handler load_posts() {
        try {
            set_state(loading, true)
            let response = api.blog.get_posts(limit: 6)
            set_state(posts, response)
        } catch (e) {
            set_state(error, e.message)
        } finally {
            set_state(loading, false)
        }
    }
    
    render {
        layout {
            hero_section {
                h1(text: "My Blog")
                p(text: "Thoughts on technology and life")
            }
            
            posts_section {
                if loading {
                    loading_grid {
                        for i in range(6) {
                            post_skeleton()
                        }
                    }
                } else if error != null {
                    error_message(text: error)
                } else {
                    posts_grid {
                        for post in posts {
                            BlogPostCard(
                                title: post.title,
                                excerpt: post.excerpt,
                                slug: post.slug,
                                date: post.created_at,
                                author: post.author.name
                            )
                        }
                    }
                }
            }
        }
    }
}

// pages/post/[slug].cdz - Individual blog post
page BlogPostPage(slug: string) -> UIComponent {
    declare_state post: BlogPost? = null
    declare_state comments: List<Comment> = []
    declare_state loading: bool = true
    declare_state error: string? = null
    
    on_mount {
        load_post_and_comments()
    }
    
    event_handler load_post_and_comments() {
        try {
            set_state(loading, true)
            
            // Load post and comments in parallel
            let [post_result, comments_result] = Promise.all([
                api.blog.get_post(slug),
                api.comments.get_comments(post_slug: slug)
            ])
            
            set_state(post, post_result)
            set_state(comments, comments_result)
        } catch (e) {
            set_state(error, e.message)
        } finally {
            set_state(loading, false)
        }
    }
    
    event_handler add_comment(comment_data: CreateCommentRequest) {
        try {
            let new_comment = api.comments.create_comment(comment_data)
            set_state(comments, comments.append(new_comment))
        } catch (e) {
            // Handle error
        }
    }
    
    render {
        layout {
            if loading {
                article_skeleton()
            } else if error != null {
                error_page(message: error)
            } else if post != null {
                article {
                    header {
                        h1(text: post.title)
                        meta_info {
                            author_info(name: post.author.name)
                            publish_date(date: post.created_at)
                        }
                    }
                    
                    content(markdown: post.content)
                    
                    comments_section {
                        h3(text: "Comments (" + comments.length.toString() + ")")
                        
                        CommentForm(
                            post_id: post.id,
                            on_submit: add_comment
                        )
                        
                        comments_list {
                            for comment in comments {
                                CommentItem(comment: comment)
                            }
                        }
                    }
                }
            } else {
                not_found_page()
            }
        }
    }
}

// pages/admin/posts.cdz - Admin interface
@auth(required: true)
@authorize(roles: ["admin", "editor"])
page AdminPosts() -> UIComponent {
    declare_state posts: List<BlogPost> = []
    declare_state loading: bool = true
    declare_state show_create_form: bool = false
    
    on_mount {
        load_all_posts()
    }
    
    event_handler load_all_posts() {
        try {
            set_state(loading, true)
            let all_posts = api.blog.get_posts(published_only: false)
            set_state(posts, all_posts)
        } catch (e) {
            // Handle error
        } finally {
            set_state(loading, false)
        }
    }
    
    event_handler create_post(post_data: CreatePostRequest) {
        try {
            let new_post = api.blog.create_post(post_data)
            set_state(posts, posts.prepend(new_post))
            set_state(show_create_form, false)
        } catch (e) {
            // Handle error
        }
    }
    
    render {
        admin_layout {
            admin_header {
                h1(text: "Manage Posts")
                Button(
                    text: "New Post",
                    variant: "primary",
                    on_click: () => set_state(show_create_form, true)
                )
            }
            
            if show_create_form {
                CreatePostForm(
                    on_submit: create_post,
                    on_cancel: () => set_state(show_create_form, false)
                )
            }
            
            if loading {
                loading_table()
            } else {
                posts_table {
                    table_header {
                        column(text: "Title")
                        column(text: "Status")
                        column(text: "Created")
                        column(text: "Actions")
                    }
                    
                    for post in posts {
                        table_row(key: post.id) {
                            cell(text: post.title)
                            cell {
                                status_badge(
                                    text: post.published ? "Published" : "Draft",
                                    variant: post.published ? "success" : "warning"
                                )
                            }
                            cell(text: post.created_at.format("MMM d, yyyy"))
                            cell {
                                action_buttons {
                                    Button(text: "Edit", size: "small")
                                    Button(text: "Delete", variant: "danger", size: "small")
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
```

**Single Command Deployment:**
```bash
$ cadenzac --serve src/
✓ Discovered 3 pages, 2 components, 2 API endpoints
✓ Generated database migrations
✓ Compiled all components and APIs
✓ Generated OpenAPI documentation at /api/docs
✓ Started development server with hot reload

🚀 Blog running at http://localhost:3000
📚 API docs at http://localhost:3000/api/docs
💾 Database: SQLite (auto-created)

Available routes:
  GET  /                    → Blog home page
  GET  /post/[slug]         → Individual blog post  
  GET  /admin/posts         → Admin interface
  GET  /api/posts           → Blog posts API
  POST /api/posts           → Create post API
  GET  /api/comments        → Comments API
```

**Result:** Complete full-stack blog application with:
- Public blog pages with server-side rendering
- RESTful API with authentication and authorization
- Admin interface for content management
- Automatic database schema generation
- Type-safe frontend-backend integration
- Auto-generated API documentation

### 4.3 Multi-Page Application Flow

**Goal:** LLM creates a blog website

**Project Structure:**
```
blog-app/
  src/
    pages/
      index.cdz          # Home page
      about.cdz          # About page  
      blog/
        index.cdz        # Blog listing
        [slug].cdz       # Individual blog post
    components/
      BlogPost.cdz       # Reusable blog post component
      Navigation.cdz     # Site navigation
    assets/
      logo.png
    cadenza.config.json
```

**Configuration:**
```json
// cadenza.config.json
{
  "name": "My Blog",
  "description": "A blog built with Cadenza",
  "theme": {
    "brand_color": "#2563eb",
    "font_family": "Inter",
    "layout": "centered"
  },
  "routing": {
    "trailing_slash": false,
    "case_sensitive": false
  },
  "build": {
    "target": "static",
    "optimize": true
  }
}
```

**Component Examples:**
```cadenza
// components/Navigation.cdz
component Navigation() -> UIComponent {
    render {
        nav {
            brand(href: "/", text: "My Blog")
            menu {
                link(href: "/", text: "Home")
                link(href: "/blog", text: "Blog") 
                link(href: "/about", text: "About")
            }
        }
    }
}

// pages/index.cdz  
page HomePage() -> UIComponent {
    declare_state recent_posts: List<BlogPost> = []
    
    on_mount {
        recent_posts = fetch_recent_posts(limit: 3)
    }
    
    render {
        layout {
            Navigation()
            
            hero_section {
                h1(text: "Welcome to My Blog")
                p(text: "Thoughts on technology and life")
            }
            
            recent_posts_section {
                h2(text: "Recent Posts")
                post_grid {
                    for post in recent_posts {
                        BlogPost(
                            title: post.title,
                            excerpt: post.excerpt,
                            date: post.date,
                            slug: post.slug
                        )
                    }
                }
            }
        }
    }
}

// pages/blog/[slug].cdz
page BlogPostPage(slug: string) -> UIComponent {
    declare_state post: BlogPost? = null
    declare_state loading: bool = true
    
    on_mount {
        post = fetch_post_by_slug(slug)
        set_state(loading, false)
    }
    
    render {
        layout {
            Navigation()
            
            if loading {
                loading_spinner()
            } else if post != null {
                article {
                    header {
                        h1(text: post.title)
                        meta_info {
                            text(text: "Published on " + post.date.format("MMMM d, yyyy"))
                        }
                    }
                    
                    content(html: post.content_html)
                }
            } else {
                error_message(text: "Post not found")
            }
        }
    }
}
```

**Deployment:**
```bash
$ cadenzac --serve src/
✓ Discovered 4 pages, 2 components
✓ Generated routing for /blog/[slug]
✓ Compiled all components
✓ Started development server with hot reload

🚀 Blog running at http://localhost:3000

$ cadenzac --export src/ --output dist/
✓ Generated static site in dist/
✓ Ready for deployment to any static host
```

### 4.4 API Integration Flow

**Goal:** LLM creates an app that consumes external APIs

```cadenza
// pages/weather.cdz
page WeatherApp() -> UIComponent {
    declare_state city: string = "London"
    declare_state weather: WeatherData? = null
    declare_state loading: bool = false
    declare_state error: string? = null
    
    event_handler fetch_weather() {
        set_state(loading, true)
        set_state(error, null)
        
        try {
            let response = api_call(
                url: "https://api.openweathermap.org/data/2.5/weather",
                params: {
                    q: city,
                    appid: env("WEATHER_API_KEY"),
                    units: "metric"
                }
            )
            set_state(weather, response.parse<WeatherData>())
        } catch (e) {
            set_state(error, e.message)
        } finally {
            set_state(loading, false)
        }
    }
    
    on_mount {
        fetch_weather()
    }
    
    render {
        container {
            h1(text: "Weather App")
            
            search_form {
                input(
                    value: city,
                    placeholder: "Enter city name...",
                    on_input: (e) => set_state(city, e.value)
                )
                Button(
                    text: "Get Weather",
                    on_click: fetch_weather,
                    disabled: loading
                )
            }
            
            if loading {
                loading_indicator(text: "Fetching weather...")
            } else if error != null {
                error_card(message: error)
            } else if weather != null {
                weather_card {
                    city_name(text: weather.name)
                    temperature(text: weather.main.temp + "°C")
                    description(text: weather.weather[0].description)
                    humidity(text: "Humidity: " + weather.main.humidity + "%")
                }
            }
        }
    }
}

// Environment variables (auto-loaded from .env)
// WEATHER_API_KEY=your_api_key_here
```

**Generated API Client (Automatic):**
```csharp
// Auto-generated from api_call usage
public class ApiClient 
{
    private readonly HttpClient _http;
    
    public async Task<ApiResponse> CallAsync(string url, Dictionary<string, string> parameters)
    {
        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        var response = await _http.GetAsync($"{url}?{query}");
        return new ApiResponse(await response.Content.ReadAsStringAsync());
    }
}
```

## 5. Implementation Phases

### 5.1 Phase 1: Embedded Web Server (Foundation)

**Duration:** 2-3 days
**Goal:** `cadenzac --serve` produces working web application

**Components:**
1. **CLI Enhancement:**
   - Add `--serve`, `--port`, `--open` flags to cadenzac
   - Project detection (single file vs. multi-file)
   - Development vs. production modes

2. **Project Generator:**
   - Auto-generate Program.cs, App.razor, _Host.cshtml
   - Embed default CSS framework
   - Component discovery and registration

3. **Embedded Kestrel Server:**
   - Self-hosting ASP.NET Core application
   - Static file serving
   - Hot reload infrastructure

4. **Integration Testing:**
   - Test with existing Counter example
   - Verify end-to-end compilation and serving
   - Browser functionality verification

**Success Criteria:**
- `cadenzac --serve counter.cdz` opens working counter in browser
- Hot reload works for component changes
- Generated application is self-contained

### 5.2 Phase 2: File-System Routing

**Duration:** 1-2 days  
**Goal:** Multi-page applications with automatic routing

**Components:**
1. **Route Discovery:**
   - Scan src/pages/ directory structure
   - Generate routing table from file names
   - Support for dynamic routes [param].cdz

2. **Layout System:**
   - Default layout component
   - Nested layouts
   - Layout inheritance

3. **Navigation Generation:**
   - Automatic menu generation from routes
   - Active route highlighting
   - Breadcrumb support

**Success Criteria:**
- Multi-page blog example works
- Routes automatically generated from file structure
- Navigation between pages functional

### 5.3 Phase 3: Semantic Styling System

**Duration:** 2-3 days
**Goal:** CSS-in-Cadenza with semantic naming

**Components:**
1. **Style Parser:**
   - Parse `styles { }` blocks in components
   - Semantic token resolution
   - CSS generation

2. **Design System:**
   - Built-in semantic tokens (colors, spacing, typography)
   - Responsive breakpoints
   - Dark/light theme support

3. **CSS Optimization:**
   - Unused style elimination
   - CSS bundling and minification
   - Critical CSS extraction

**Success Criteria:**
- Components can define styles semantically
- Generated CSS is optimized and minimal
- Responsive design works automatically

### 5.4 Phase 4: Production Features

**Duration:** 1-2 days
**Goal:** Production deployment capabilities

**Components:**
1. **Static Export:**
   - `cadenzac --export` generates static site
   - Pre-rendering for SEO
   - Asset optimization

2. **Container Support:**
   - Docker image generation
   - Multi-stage builds
   - Health checks

3. **Cloud Integration:**
   - Deployment scripts for major providers
   - Environment variable management
   - CDN configuration

**Success Criteria:**
- Applications can be deployed to production
- Performance optimizations in place
- Multiple deployment targets supported

## 6. Technical Decisions and Trade-offs

### 6.1 Blazor Server vs. Blazor WebAssembly

**Decision:** Blazor Server for initial implementation

**Rationale:**
- **Simpler Deployment:** Single server, no client-side complexity
- **Better Performance:** No large WASM download
- **Easier Debugging:** Server-side debugging tools
- **Incremental Migration:** Can add WASM export later

**Trade-offs:**
- Requires constant server connection
- Higher server resource usage
- Not suitable for offline applications

**Future Consideration:** Add `--target wasm` for client-side deployment

### 6.2 File-System vs. Configuration-Based Routing

**Decision:** File-system based routing

**Rationale:**
- **Zero Configuration:** Routes automatically inferred
- **Predictable:** Clear mapping between files and routes
- **LLM Friendly:** No routing configuration to learn

**Trade-offs:**
- Less flexible than configuration-based routing
- File naming conventions must be learned
- Complex routing patterns harder to express

### 6.3 Semantic vs. Traditional CSS

**Decision:** Semantic styling with traditional CSS output

**Rationale:**
- **LLM Friendly:** Meaningful names instead of CSS properties
- **Design System:** Built-in consistency
- **Progressive Enhancement:** Can add custom CSS when needed

**Trade-offs:**
- Learning curve for semantic tokens
- Potential limitation for complex designs
- Abstraction may hide underlying CSS

### 6.4 Component vs. HTML-First

**Decision:** Component-first architecture

**Rationale:**
- **Consistency:** Matches existing Cadenza component model
- **Reusability:** Components can be shared and composed
- **Type Safety:** Compile-time checking of component usage

**Trade-offs:**
- Higher abstraction than direct HTML
- May require more components for simple layouts
- Learning curve for component composition

## 7. Performance Considerations

### 7.1 Development Performance

**Hot Reload:**
- File watching for .cdz files
- Incremental compilation of changed components
- Browser refresh/update via SignalR

**Build Performance:**
- Parallel compilation of independent components
- Caching of compiled components
- Incremental builds for unchanged files

### 7.2 Runtime Performance

**Server-Side Rendering:**
- Pre-rendering for faster initial page load
- Component caching for repeated renders
- Efficient state management

**Client-Side Performance:**
- Minimal JavaScript footprint
- CSS optimization and bundling
- Image optimization and lazy loading

### 7.3 Scalability

**Development Scalability:**
- Support for large projects (100+ components)
- Efficient routing for many pages
- Module system for component organization

**Runtime Scalability:**
- Connection pooling for multiple users
- Efficient state synchronization
- Resource optimization

## 8. Security Considerations

### 8.1 Server-Side Security

**Input Validation:**
- Automatic sanitization of user inputs
- XSS prevention in rendered content
- SQL injection prevention in API calls

**Authentication & Authorization:**
- Built-in authentication providers
- Role-based authorization
- Secure session management

### 8.2 Client-Side Security

**Content Security Policy:**
- Automatic CSP header generation
- Inline script prevention
- Trusted source enforcement

**Data Protection:**
- Automatic HTTPS enforcement
- Secure cookie configuration
- CSRF protection

## 9. Migration and Compatibility

### 9.1 From Current Blazor Implementation

**Migration Path:**
1. Existing Blazor-generated components work unchanged
2. Add web runtime CLI commands
3. Gradually adopt file-system routing
4. Migrate to semantic styling system

**Compatibility:**
- Existing .cdz files compile unchanged
- Generated C# code remains compatible
- Can mix manual Blazor components with Cadenza components

### 9.2 Export Options

**Standard Web Technologies:**
- Export to static HTML/CSS/JS
- Export to Blazor project for customization
- Export to Docker container for deployment

**Integration Options:**
- Embed in existing ASP.NET Core applications
- Use as micro-frontend components
- Deploy as standalone web services

## 10. Future Enhancements

### 10.1 Advanced Features

**Real-time Capabilities:**
- Built-in WebSocket support
- Real-time data synchronization
- Live collaboration features

**Progressive Web App:**
- Service worker generation
- Offline support
- Push notification integration

**Advanced Styling:**
- Animation system
- Advanced responsive features
- Custom design system support

### 10.2 Ecosystem Integration

**State Management:**
- Integration with state management libraries
- Global state across components
- Persistence and hydration

**Testing:**
- Automated testing framework
- Component testing utilities
- E2E testing integration

**Development Tools:**
- Visual component editor
- Debug tools and profiling
- Performance monitoring

## 11. Conclusion

The Cadenza Self-Contained Web Runtime represents a fundamental shift from traditional web development to LLM-centric application creation. By abstracting away web development complexity while maintaining the power and flexibility of modern web technologies, it enables rapid creation of production-ready web applications with minimal learning curve.

**Key Success Factors:**
1. **Simplicity:** Single command to working web application
2. **Flexibility:** Scales from simple to complex applications
3. **Standards:** Generates standard web technologies
4. **Performance:** Production-ready output
5. **Ecosystem:** Integrates with existing tools and services

**Implementation Priority:**
- Phase 1 (Embedded Web Server) provides immediate value
- Phase 2 (File-System Routing) enables multi-page applications  
- Phase 3 (Semantic Styling) completes the developer experience
- Phase 4 (Production Features) enables real-world deployment

This specification provides the roadmap for transforming Cadenza from a transpilation tool into a complete web development platform optimized for LLM-driven development.