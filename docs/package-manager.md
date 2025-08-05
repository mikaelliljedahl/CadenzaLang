# Cadenza Package Manager

The Cadenza Package Manager provides seamless dependency management with .NET ecosystem integration, automatic effect inference, and comprehensive security scanning.

## Overview

Cadenza's enhanced package manager extends the basic `cadenzac.json` configuration to provide:

- **Semantic versioning** with conflict resolution
- **NuGet ecosystem integration** with automatic Cadenza bindings
- **Effect inference** for external dependencies
- **Security vulnerability scanning** and automated fixes
- **Workspace management** for multi-project solutions
- **Cadenza-specific package registry** for publishing and discovery

## Installation and Setup

The package manager is built into the Cadenza CLI. No additional installation is required.

```bash
# Check if package management is available
cadenzac --help

# Create a new project with package management
cadenzac new my-project
cd my-project
```

## Project Configuration

### Enhanced cadenzac.json

The enhanced `cadenzac.json` provides comprehensive package management configuration:

```json
{
  "name": "my-cadenza-app",
  "version": "1.2.0",
  "description": "A Cadenza application with package management",
  "dependencies": {
    "Cadenza.Core": "^2.1.0",
    "Cadenza.Database": "~1.5.2",
    "Newtonsoft.Json": "13.0.3"
  },
  "devDependencies": {
    "Cadenza.Testing": "^1.0.0"
  },
  "nugetSources": [
    "https://api.nuget.org/v3/index.json",
    "https://nuget.mycompany.com/v3/index.json"
  ],
  "cadenzaRegistry": "https://packages.cadenza.org",
  "effectMappings": {
    "System.Data.SqlClient": ["Database"],
    "System.Net.Http": ["Network"],
    "Microsoft.Extensions.Logging": ["Logging"]
  },
  "workspace": {
    "projects": ["./src", "./libs/utils"],
    "exclude": ["./examples", "./tests"]
  },
  "publishConfig": {
    "registry": "https://packages.cadenza.org",
    "access": "public"
  }
}
```

### Lock File (cadenzac.lock)

The lock file ensures reproducible builds across environments:

```json
{
  "lockfileVersion": 2,
  "resolved": {
    "Cadenza.Core@2.1.3": {
      "version": "2.1.3",
      "resolved": "https://packages.cadenza.org/Cadenza.Core/-/Cadenza.Core-2.1.3.tgz",
      "integrity": "sha512-abc123...",
      "dependencies": {
        "Newtonsoft.Json": "13.0.3"
      },
      "effects": ["Memory", "IO"]
    }
  },
  "generatedAt": "2024-01-15T10:30:00.000Z"
}
```

## Package Management Commands

### Adding Packages

```bash
# Add runtime dependency
cadenzac add Cadenza.Database

# Add specific version
cadenzac add Newtonsoft.Json@13.0.3

# Add with version constraint
cadenzac add Cadenza.Testing@^1.0.0 --dev

# Add from NuGet
cadenzac add Microsoft.Extensions.Logging
```

### Removing Packages

```bash
# Remove package
cadenzac remove Cadenza.Database

# Remove development dependency
cadenzac remove Cadenza.Testing
```

### Installing Dependencies

```bash
# Install all dependencies
cadenzac install

# Install only production dependencies
cadenzac install --production

# Install in workspace (all projects)
cadenzac workspace install
```

### Updating Packages

```bash
# Update all packages to latest compatible versions
cadenzac update

# Update specific package
cadenzac update Cadenza.Core

# Show outdated packages
cadenzac outdated
```

### Package Discovery

```bash
# Search for packages
cadenzac search http

# Get package information
cadenzac info Cadenza.Database

# List installed packages
cadenzac list
```

## Effect System Integration

### Automatic Effect Inference

The package manager automatically infers effects for external dependencies:

```cadenza
// NuGet package: EntityFramework
import EntityFramework.{DbContext, SaveChanges}

function save_user(context: DbContext, user: User) uses [Database] -> Result<Unit, DbError> {
    // Effect automatically inferred from EntityFramework usage
    return context.SaveChanges(user)
}
```

### Effect Mapping Configuration

Configure custom effect mappings in `cadenzac.json`:

```json
{
  "effectMappings": {
    "System.Data.SqlClient.*": ["Database"],
    "System.Net.Http.*": ["Network"],
    "Microsoft.Extensions.Logging.*": ["Logging"],
    "Azure.Storage.*": ["Network", "FileSystem"],
    "Redis.StackExchange.*": ["Network", "Memory"]
  }
}
```

## NuGet Integration

### Automatic Cadenza Bindings

The package manager automatically generates Cadenza bindings for .NET libraries:

```csharp
// C# library: System.Net.Http
namespace System.Net.Http {
    public class HttpClient { 
        public Task<string> GetStringAsync(string url); 
    }
}
```

Generated Cadenza binding:
```cadenza
module System_Net_Http {
    function http_get(url: string) uses [Network] -> Result<string, HttpError> {
        // Implementation bridges to System.Net.Http.HttpClient
    }
    
    export { http_get }
}
```

### Supported .NET Frameworks

- .NET 10.0+ (primary target)
- .NET Standard 2.1
- .NET Framework 4.8 (compatibility mode)

## Security and Vulnerability Management

### Security Scanning

```bash
# Scan for vulnerabilities
cadenzac audit

# Scan with detailed output
cadenzac audit --verbose

# Automatically fix vulnerabilities
cadenzac audit fix

# Scan specific categories
cadenzac audit --effects --security
```

### Vulnerability Report Example

```bash
$ cadenzac audit
Security audit completed for 15 packages
Vulnerabilities found: 3
  Critical: 1
  High: 1
  Medium: 1
  Low: 0

Vulnerable packages:
  System.Net.Http@4.3.0: CVE-2017-0247 (Critical)
    Fixed in: 4.3.1
  
Run 'cadenzac audit fix' to automatically fix compatible issues.
```

### Security Databases

The scanner integrates with multiple vulnerability databases:
- **GitHub Advisory Database** (NuGet packages)
- **OSV (Open Source Vulnerabilities)** database
- **Cadenza Security Database** (Cadenza-specific packages)

## Workspace Management

### Multi-Project Setup

```json
{
  "name": "my-workspace",
  "workspace": {
    "projects": ["./apps/*", "./libs/*"],
    "exclude": ["./examples"]
  }
}
```

### Workspace Commands

```bash
# List workspace projects
cadenzac workspace list

# Install dependencies for all projects
cadenzac workspace install

# Run command across workspace
cadenzac workspace run build
cadenzac workspace run test
```

### Workspace Structure Example

```
my-workspace/
├── cadenzac.json (workspace root)
├── apps/
│   ├── web-app/
│   │   ├── cadenzac.json
│   │   └── src/main.cdz
│   └── api-service/
│       ├── cadenzac.json
│       └── src/main.cdz
├── libs/
│   ├── utils/
│   │   ├── cadenzac.json
│   │   └── src/helpers.cdz
│   └── data/
│       ├── cadenzac.json
│       └── src/models.cdz
└── cadenzac.lock (unified dependencies)
```

## Package Publishing

### Creating a Package

```bash
# Create package archive
cadenzac pack

# Create and publish
cadenzac publish

# Dry run (test without publishing)
cadenzac publish --dry-run

# Private package
cadenzac publish --private
```

### Package Structure

A Cadenza package includes:

```
sample-package-1.0.0.tgz
├── cadenzac.json
├── src/
│   └── main.cdz
├── README.md
└── LICENSE
```

### Publishing Requirements

- Valid `cadenzac.json` with name, version, and description
- Semantic versioning (e.g., 1.2.3)
- At least one `.cdz` source file
- README.md (recommended)
- LICENSE file (recommended)

## Version Management

### Semantic Versioning

Cadenza follows [Semantic Versioning](https://semver.org/):

- **Major** (X.y.z): Breaking changes
- **Minor** (x.Y.z): New features, backward compatible
- **Patch** (x.y.Z): Bug fixes, backward compatible

### Version Constraints

```json
{
  "dependencies": {
    "exact": "1.2.3",
    "caret": "^1.2.3",  // >=1.2.3 <2.0.0
    "tilde": "~1.2.3",  // >=1.2.3 <1.3.0
    "range": ">=1.2.0 <2.0.0",
    "wildcard": "*"     // Any version
  }
}
```

### Version Commands

```bash
# Show current version
cadenzac version

# Bump patch version (1.0.0 -> 1.0.1)
cadenzac version patch

# Bump minor version (1.0.1 -> 1.1.0)
cadenzac version minor

# Bump major version (1.1.0 -> 2.0.0)
cadenzac version major

# Set specific version
cadenzac version 2.1.0-beta.1
```

## Advanced Features

### Private Package Registries

Configure private registries for enterprise use:

```json
{
  "nugetSources": [
    "https://api.nuget.org/v3/index.json",
    "https://nuget.mycompany.com/v3/index.json"
  ],
  "cadenzaRegistry": "https://packages.mycompany.com"
}
```

### Dependency Resolution

The resolver handles complex scenarios:

- **Version conflicts**: Automatically resolves compatible versions
- **Circular dependencies**: Detects and reports circular references
- **Transitive dependencies**: Resolves entire dependency graph
- **Effect propagation**: Tracks effects through dependency chain

### Performance Optimization

- **Local caching**: Downloaded packages cached locally
- **Parallel downloads**: Multiple packages downloaded concurrently
- **Lock file validation**: Fast dependency checking
- **Incremental resolution**: Only re-resolve changed dependencies

## CI/CD Integration

### Continuous Integration

```yaml
# GitHub Actions example
- name: Install Cadenza dependencies
  run: cadenzac install --production

- name: Security audit
  run: cadenzac audit

- name: Build project
  run: cadenzac build
```

### Lock File Best Practices

- **Commit lock files** to version control
- **Validate lock files** in CI pipelines
- **Use `--production`** in production builds
- **Audit regularly** for security vulnerabilities

## Troubleshooting

### Common Issues

**Package not found:**
```bash
# Check spelling and availability
cadenzac search package-name
cadenzac info package-name
```

**Version conflicts:**
```bash
# View dependency tree
cadenzac list --tree

# Force resolution
cadenzac install --force-resolve
```

**Lock file issues:**
```bash
# Regenerate lock file
rm cadenzac.lock
cadenzac install
```

**Network issues:**
```bash
# Clear cache
cadenzac clean

# Use different registry
cadenzac add package-name --registry=https://alternate-registry.com
```

### Debug Mode

Enable verbose logging for troubleshooting:

```bash
# Verbose output
cadenzac install --verbose

# Debug dependency resolution
cadenzac install --debug

# Show package resolution steps
cadenzac install --trace
```

## Migration Guide

### From Basic cadenzac.json

Existing Cadenza projects automatically upgrade to the enhanced package manager:

```json
// Old format
{
  "name": "my-project",
  "version": "1.0.0",
  "dependencies": {
    "some-package": "1.0.0"
  }
}

// New format (backward compatible)
{
  "name": "my-project",
  "version": "1.0.0",
  "dependencies": {
    "some-package": "1.0.0"
  },
  "devDependencies": {},
  "nugetSources": ["https://api.nuget.org/v3/index.json"],
  "effectMappings": {}
}
```

### From Other Package Managers

**From npm:**
```bash
# Convert package.json dependencies
cadenzac migrate --from=npm

# Manual conversion
# npm: "lodash": "^4.17.21"
# Cadenza: "Lodash.Net": "^1.0.0"
```

**From NuGet packages.config:**
```bash
# Convert packages.config
cadenzac migrate --from=nuget-packages-config
```

## Best Practices

### Project Organization

1. **Use semantic versioning** for all packages
2. **Pin exact versions** for critical dependencies
3. **Use version ranges** for libraries to allow updates
4. **Separate dev dependencies** from runtime dependencies
5. **Configure effect mappings** for better static analysis

### Security

1. **Run security audits** regularly (`cadenzac audit`)
2. **Keep dependencies updated** (`cadenzac update`)
3. **Review dependency licenses** before adding
4. **Use private registries** for proprietary packages
5. **Validate package signatures** in production

### Performance

1. **Use lock files** for reproducible builds
2. **Clean cache** periodically (`cadenzac clean`)
3. **Use workspace commands** for monorepos
4. **Minimize dependency count** to reduce build time
5. **Use `--production`** in production builds

## Examples

### Basic Package Setup

```cadenza
// math_utils.cdz
module MathUtils {
    pure function add(a: int, b: int) -> int {
        return a + b
    }
    
    pure function multiply(a: int, b: int) -> int {
        return a * b
    }
    
    export { add, multiply }
}
```

```json
// cadenzac.json
{
  "name": "math-utils",
  "version": "1.0.0",
  "description": "Mathematical utility functions",
  "dependencies": {},
  "publishConfig": {
    "access": "public"
  }
}
```

### Using External Dependencies

```cadenza
// web_service.cdz
import HttpClient.{get, post}
import Database.{connect, query}

function fetch_user_data(user_id: string) uses [Database, Network] -> Result<User, Error> {
    let db = connect("postgresql://localhost/mydb")?
    let user = query(db, "SELECT * FROM users WHERE id = $1", [user_id])?
    return Ok(user)
}

function notify_user(user: User, message: string) uses [Network] -> Result<Unit, HttpError> {
    let response = post("https://api.notifications.com/send", {
        user_id: user.id,
        message: message
    })?
    return Ok(())
}
```

### Workspace Configuration

```json
// Root cadenzac.json
{
  "name": "my-company-platform",
  "workspace": {
    "projects": [
      "./services/*",
      "./libs/*",
      "./tools/*"
    ],
    "exclude": [
      "./services/legacy",
      "./tools/experimental"
    ]
  },
  "effectMappings": {
    "Company.Database.*": ["Database"],
    "Company.Messaging.*": ["Network"],
    "Company.Logging.*": ["Logging"]
  }
}
```

This comprehensive package management system provides Cadenza developers with powerful dependency management capabilities while maintaining the language's core principles of explicitness, safety, and effect tracking.