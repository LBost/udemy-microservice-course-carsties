# IdentityService Technical Documentation

## Overview

IdentityService is an authentication and authorization microservice built with .NET 10.0 using Duende IdentityServer. It provides OAuth 2.0 and OpenID Connect (OIDC) authentication services for the Carsties auction platform. The service manages user identities, issues JWT tokens, and handles user authentication flows including resource owner password credentials and authorization code flows.

---

## Tech Stack

- **.NET 10.0** - Target framework
- **ASP.NET Core** - Web framework
- **Duende IdentityServer** - Identity and access control framework
- **ASP.NET Core Identity** - User management framework
- **Entity Framework Core** - ORM for database operations
- **PostgreSQL** - Relational database
- **Serilog** - Structured logging framework

---

## NuGet Packages

| Package                                                | Version | Purpose                                               |
| ------------------------------------------------------ | ------- | ----------------------------------------------------- |
| `Duende.IdentityServer.AspNetIdentity`                 | 7.3.0   | IdentityServer integration with ASP.NET Core Identity |
| `Npgsql.EntityFrameworkCore.PostgreSQL`                | 8.0.11  | PostgreSQL database provider for EF Core              |
| `Serilog.AspNetCore`                                   | 8.0.3   | Serilog logging integration                           |
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` | 8.0.11  | EF Core diagnostics tools                             |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore`    | 8.0.11  | Identity EF Core integration                          |
| `Microsoft.AspNetCore.Identity.UI`                     | 8.0.11  | Identity UI components                                |
| `Microsoft.EntityFrameworkCore.Tools`                  | 8.0.11  | EF Core design-time tools for migrations              |

---

## Models

### ApplicationUser

Extends ASP.NET Core Identity's `IdentityUser`:

- Inherits standard Identity properties:
  - `Id` (string) - Unique identifier
  - `UserName` (string) - Username
  - `Email` (string) - Email address
  - `EmailConfirmed` (bool) - Email confirmation status
  - Additional Identity properties

**Database Table:** `AspNetUsers` (via ASP.NET Core Identity)

---

## Data Layer

### ApplicationDbContext

Entity Framework Core database context extending `IdentityDbContext<ApplicationUser>`.

**Features:**

- Manages user identity tables:
  - `AspNetUsers` - User accounts
  - `AspNetRoles` - Roles
  - `AspNetUserRoles` - User-role mappings
  - `AspNetUserClaims` - User claims
  - `AspNetRoleClaims` - Role claims
  - Additional Identity tables

**Database:** PostgreSQL

---

## Configuration

### Identity Resources

Defined in `Config.cs`:

1. **OpenId** - Standard OpenID Connect identity resource
2. **Profile** - User profile information

### API Scopes

Defined in `Config.cs`:

- `auctionApp` - Full access to auction application APIs
  - Description: "Auction app full access"

### Clients

Defined in `Config.cs`:

#### 1. **postman**

- **Client ID:** `postman`
- **Client Name:** Postman
- **Allowed Scopes:** `auctionApp`, `openid`, `profile`
- **Redirect URIs:** `https://oauth.pstmn.io/v1/callback`
- **Client Secret:** `NotASecret` (SHA256 hashed)
- **Grant Types:** Resource Owner Password Credentials
- **Use Case:** API testing with Postman

#### 2. **nextApp**

- **Client ID:** `nextApp`
- **Client Name:** Next.js Application
- **Client Secret:** `secret` (SHA256 hashed)
- **Allowed Grant Types:** Authorization Code + Client Credentials
- **Require PKCE:** false
- **Redirect URIs:** `http://localhost:3000/api/auth/callback/id-server`
- **Allow Offline Access:** true (refresh tokens enabled)
- **Allowed Scopes:** `auctionApp`, `openid`, `profile`
- **Access Token Lifetime:** 30 days (3600 _ 24 _ 30 seconds)
- **Always Include User Claims In Id Token:** true
- **Use Case:** Next.js frontend application

---

## Services

### CustomProfileService

Implements `IProfileService` to customize claims included in tokens.

**Methods:**

- `GetProfileDataAsync(ProfileDataRequestContext)` - Adds custom claims to tokens:

  - `username` - User's username
  - `email` - User's email address
  - `name` - User's display name (from existing claims)

- `IsActiveAsync(IsActiveContext)` - Determines if user is active (always returns true)

**Purpose:** Ensures `username` claim is available in JWT tokens for backend services.

---

## Seed Data

### SeedData

Initializes the database with default users on first startup.

**Seeded Users:**

1. **alice**

   - Username: `alice`
   - Email: `AliceSmith@example.com`
   - Password: `Pass123$`
   - Email Confirmed: `true`
   - Claims: `name` = "Alice Smith"

2. **bob**
   - Username: `bob`
   - Email: `BobSmith@example.com`
   - Password: `Pass123$`
   - Email Confirmed: `true`
   - Claims: `name` = "Bob Smith"

**Behavior:**

- Runs database migrations automatically
- Only creates users if database is empty
- Logs user creation status

---

## Configuration Files

### appsettings.json

Base configuration:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.AspNetCore.Authentication": "Debug",
        "System": "Warning"
      }
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost:5432; User Id=postgres;Password=postgresPwd;Database=Identity;"
  }
}
```

### Key Configuration Settings

1. **Database Connection**

   - Provider: PostgreSQL
   - Default connection string configured in `appsettings.json`
   - Database name: `Identity`

2. **Serilog Logging**

   - Console logging with structured format
   - Development mode: Diagnostic logs written to `./diagnostics/diagnostic.log`
   - Log rotation: Daily, 10 MB file size limit
   - Filtered diagnostic summaries in development

3. **IdentityServer Configuration**

   - Events enabled: Error, Information, Failure, Success
   - Development mode: 10 MB chunk size for diagnostic data
   - Docker environment: Issuer URI set to `http://localhost:5000`
   - Uses in-memory configuration for resources, scopes, and clients
   - Integrated with ASP.NET Core Identity
   - Custom profile service for claim customization

4. **Application Cookie**

   - SameSite mode: Lax
   - Standard ASP.NET Core Identity cookie settings

---

## Hosting Extensions

### ConfigureLogging

Sets up Serilog logging:

- Console output with formatted timestamps
- Development mode: File logging for diagnostic summaries
- Filters diagnostic summaries from console in development

### ConfigureServices

Configures all services:

1. **Razor Pages** - For Identity UI pages
2. **Entity Framework** - PostgreSQL database context
3. **ASP.NET Core Identity** - User management
4. **Duende IdentityServer** - Authentication server
5. **Custom Profile Service** - Claim customization

### ConfigurePipeline

Configures HTTP request pipeline:

1. Serilog request logging
2. Developer exception page (development only)
3. Static files
4. Routing
5. IdentityServer middleware
6. Authorization middleware
7. Razor Pages (requires authorization)

---

## Docker Settings

### Dockerfile

Multi-stage build configuration:

**Build Stage:**

- Base image: `mcr.microsoft.com/dotnet/sdk:10.0`
- Copies solution and project files
- Restores dependencies
- Publishes application in Release configuration

**Runtime Stage:**

- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0`
- Copies published output from build stage
- Exposes port 80
- Entry point: `dotnet IdentityService.dll`

**Note:** The Dockerfile references `carsties.sln` and expects the project structure:

```
.
├── carsties.sln
├── src/
│   └── IdentityService/
│       └── IdentityService.csproj
```

---

## Getting Started

### Prerequisites

1. **.NET 10.0 SDK**
2. **PostgreSQL** (running on localhost:5432)
3. **Duende IdentityServer License** (for production use)

### Setup Steps

1. **Configure Database**

   - Ensure PostgreSQL is running
   - Update connection string in `appsettings.json` if needed
   - Database will be created automatically via migrations

2. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

3. **Run Migrations**

   - Migrations run automatically on startup via `SeedData`
   - Or manually: `dotnet ef database update`

4. **Run the Service**

   ```bash
   dotnet run
   ```

5. **Verify Endpoints**
   - Identity Server: `http://localhost:5000`
   - Discovery Document: `http://localhost:5000/.well-known/openid-configuration`
   - User Interface: `http://localhost:5000` (Razor Pages)

### Docker Deployment

1. **Build Image**

   ```bash
   docker build -t identity-service .
   ```

2. **Run Container**
   ```bash
   docker run -p 5000:80 identity-service
   ```

**Note:** Ensure PostgreSQL is accessible from the container (use Docker network or update connection string).

---

## Authentication Flows

### Resource Owner Password Credentials (Postman Client)

1. Client sends POST request to `/connect/token`:

   ```
   grant_type=password
   username=alice
   password=Pass123$
   client_id=postman
   client_secret=NotASecret
   scope=auctionApp
   ```

2. IdentityServer validates credentials
3. Returns access token and optional refresh token

### Authorization Code Flow (Next.js Client)

1. User redirected to `/connect/authorize`:

   ```
   client_id=nextApp
   redirect_uri=http://localhost:3000/api/auth/callback/id-server
   response_type=code
   scope=auctionApp openid profile
   ```

2. User authenticates via Identity UI
3. IdentityServer redirects to callback with authorization code
4. Client exchanges code for tokens at `/connect/token`

---

## JWT Token Claims

Tokens issued by IdentityServer include:

- `sub` - Subject (user ID)
- `username` - Username (custom claim via CustomProfileService)
- `email` - Email address (custom claim via CustomProfileService)
- `name` - Display name (from user claims)
- `aud` - Audience (`auctionApp`)
- `iss` - Issuer (IdentityServer URL)
- `exp` - Expiration time
- `iat` - Issued at time
- `scope` - Granted scopes

---

## Architecture Patterns

1. **Identity Provider** - Centralized authentication and authorization
2. **OAuth 2.0 / OIDC** - Industry-standard authentication protocols
3. **Token-Based Authentication** - JWT tokens for stateless authentication
4. **User Management** - ASP.NET Core Identity for user storage
5. **Claim-Based Authorization** - Custom claims for fine-grained access control

---

## Notes

- The service uses Entity Framework Core migrations for database schema management
- Default users are seeded automatically on first startup
- Diagnostic logging is enabled in development mode
- The service supports both interactive (authorization code) and non-interactive (password) flows
- Custom profile service ensures `username` claim is available for backend services
- Refresh tokens are supported for the Next.js client
- The service uses Razor Pages for user interface (login, registration, etc.)
