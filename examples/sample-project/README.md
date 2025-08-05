# Sample Multi-File Cadenza Project

This example demonstrates multi-file compilation in Cadenza, showing how to structure a project with multiple modules and dependencies.

## Project Structure

```
sample-project/
├── cadenzac.json                    # Project configuration
├── src/
│   ├── main.cdz                    # Application entry point
│   ├── controllers/
│   │   └── user_controller.cdz     # User controller logic
│   ├── services/
│   │   └── user_service.cdz        # Business logic layer
│   └── models/
│       └── user.cdz                # Data models
└── README.md                       # This file
```

## Features Demonstrated

- **Multi-file imports**: Cross-module dependencies using relative imports
- **Module system**: Each file declares its module namespace
- **Project configuration**: Using `cadenzac.json` for build settings
- **Effect tracking**: Functions declare their side effects (IO, Mutation)
- **Result types**: Error handling with Result<T, E>
- **Type safety**: Structured data types and null safety

## Module Dependencies

```
main.cdz
├── controllers/user_controller.cdz
│   ├── services/user_service.cdz
│   │   └── models/user.cdz
│   └── models/user.cdz
├── services/user_service.cdz
│   └── models/user.cdz
└── models/user.cdz
```

## Compilation

### Using Project Configuration

```bash
# Compile using cadenzac.json settings
cadenzac-core --project

# This will create: bin/SampleWebAPI.exe
```

### Auto-Discovery Mode

```bash
# Compile without configuration (auto-discover all .cdz files)
cd src/
cadenzac-core --project --output ../SampleApp.exe
```

### Library Generation

```bash
# Generate a library instead of executable
cadenzac-core --project --library --output bin/SampleLibrary.dll
```

## Running the Sample

After compilation:

```bash
# Run the generated executable
./bin/SampleWebAPI.exe
```

Expected output:
```
Starting Sample Web API...
UserController: Creating new user
Saving user: John Doe (john@example.com)
UserController: User created successfully with ID 1
User created with ID: 1
```

## Code Overview

### Entry Point (main.cdz)
- Imports all necessary modules
- Initializes services and controllers
- Demonstrates the full request flow
- Handles results with pattern matching

### Models (models/user.cdz)
- Defines the User data type
- Pure functions for user creation and validation
- Demonstrates nullable types and safety

### Services (services/user_service.cdz)
- Business logic for user operations
- Stateful service with mutation tracking
- Effect annotations for IO operations
- Result-based error handling

### Controllers (controllers/user_controller.cdz)
- HTTP controller layer (simplified)
- Delegates to service layer
- Comprehensive error handling and logging

## Key Cadenza Features

### 1. Import System
```cadenza
import "./models/user" as User
import "../services/user_service" as UserService
```

### 2. Module Declarations
```cadenza
module models.user
module services.user_service
```

### 3. Effect Tracking
```cadenza
function createUser(user: User) uses [Mutation, IO] -> Result<int, string>
```

### 4. Result Types
```cadenza
match result {
    Ok(value) -> // success case
    Error(message) -> // error case
}
```

### 5. Type Safety
```cadenza
type User {
    id: int?,
    name: string,
    email: string
}
```

This sample provides a foundation for understanding how to structure larger Cadenza applications with proper separation of concerns and type safety.