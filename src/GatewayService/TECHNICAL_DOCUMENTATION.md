# GatewayService Technical Documentation

## Overview

GatewayService is an API Gateway microservice built with .NET 10.0 using YARP (Yet Another Reverse Proxy). It acts as a single entry point for all client requests, routing them to appropriate backend services while handling authentication and authorization centrally. The gateway provides unified routing, request transformation, and JWT token validation.

---

## Tech Stack

- **.NET 10.0** - Target framework
- **ASP.NET Core** - Web framework
- **YARP (Yet Another Reverse Proxy)** - Reverse proxy library
- **JWT Bearer Authentication** - Authentication mechanism

---

## NuGet Packages

| Package                                         | Version | Purpose                     |
| ----------------------------------------------- | ------- | --------------------------- |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.0  | JWT token authentication    |
| `Yarp.ReverseProxy`                             | 2.3.0   | Reverse proxy functionality |

---

## Architecture

The GatewayService acts as a reverse proxy that:

1. **Routes Requests** - Forwards incoming requests to appropriate backend services
2. **Handles Authentication** - Validates JWT tokens for protected endpoints
3. **Transforms Paths** - Rewrites request paths to match backend service routes
4. **Centralizes Authorization** - Applies authorization policies before forwarding requests

---

## Routing Configuration

Routes are configured in `appsettings.json` under the `ReverseProxy` section.

### Routes

#### 1. **auctions-read**

- **Path:** `/auctions/{**catch-all}`
- **Methods:** `GET`
- **Cluster:** `auctions`
- **Authentication:** Not required
- **Path Transform:** `/api/auctions/{**catch-all}`
- **Backend:** `http://localhost:7001/`

#### 2. **auctions-write**

- **Path:** `/auctions/{**catch-all}`
- **Methods:** `POST`, `PUT`, `DELETE`
- **Cluster:** `auctions`
- **Authentication:** Required (`default` authorization policy)
- **Path Transform:** `/api/auctions/{**catch-all}`
- **Backend:** `http://localhost:7001/`

#### 3. **search**

- **Path:** `/search/{**catch-all}`
- **Methods:** `GET`
- **Cluster:** `search`
- **Authentication:** Not required
- **Path Transform:** `/api/search/{**catch-all}`
- **Backend:** `http://localhost:7002/`

#### 4. **bidsWrite**

- **Path:** `/bids`
- **Methods:** `POST`
- **Cluster:** `bids`
- **Authentication:** Required (`default` authorization policy)
- **Path Transform:** `/api/bids`
- **Backend:** `http://localhost:7003`

#### 5. **bidsRead**

- **Path:** `/bids/{**catch-all}`
- **Methods:** `GET`
- **Cluster:** `bids`
- **Authentication:** Not required
- **Path Transform:** `/api/bids/{**catch-all}`
- **Backend:** `http://localhost:7003`

---

## Clusters

Clusters define the backend services that handle requests:

### auctions

- **Destination:** `http://localhost:7001/`
- **Service:** AuctionService

### search

- **Destination:** `http://localhost:7002/`
- **Service:** SearchService

### bids

- **Destination:** `http://localhost:7003`
- **Service:** BiddingService

---

## Configuration

### appsettings.json

Main configuration file:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      // Route definitions (see Routing Configuration section)
    },
    "Clusters": {
      // Cluster definitions (see Clusters section)
    }
  }
}
```

### appsettings.Development.json

Development-specific configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ReverseProxy": {
    "Clusters": {
      "auctions": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:7001/"
          }
        }
      },
      "search": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:7002/"
          }
        }
      },
      "bids": {
        "Destinations": {
          "bidApi": {
            "Address": "http://localhost:7003"
          }
        }
      }
    }
  },
  "IdentityServiceUrl": "http://localhost:5000"
}
```

### Key Configuration Settings

1. **JWT Authentication**

   - Authority: `http://localhost:5000` (Identity Service)
   - HTTPS metadata: Disabled (development)
   - Audience validation: Disabled
   - Name claim type: `username`

2. **Reverse Proxy**

   - Routes loaded from configuration
   - Path transformations applied automatically
   - Authorization policies enforced before forwarding

---

## Program.cs

The service configuration:

1. **Adds Reverse Proxy** - Configures YARP from configuration
2. **Adds Authentication** - Configures JWT Bearer authentication
3. **Maps Reverse Proxy** - Sets up the reverse proxy endpoint
4. **Applies Middleware** - Authentication and authorization middleware

**Middleware Order:**

1. Reverse Proxy mapping
2. Authentication
3. Authorization

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
- Entry point: `dotnet GatewayService.dll`

**Note:** The Dockerfile references `carsties.sln` and expects the project structure:

```
.
├── carsties.sln
├── src/
│   └── GatewayService/
│       └── GatewayService.csproj
```

---

## Getting Started

### Prerequisites

1. **.NET 10.0 SDK**
2. **Identity Service** (running on http://localhost:5000) - for JWT token validation
3. **Backend Services:**
   - AuctionService (running on http://localhost:7001)
   - SearchService (running on http://localhost:7002)
   - BiddingService (running on http://localhost:7003)

### Setup Steps

1. **Configure Backend Services**

   - Ensure all backend services are running
   - Update cluster addresses in `appsettings.Development.json` if needed

2. **Configure Identity Service**

   - Ensure Identity Service is running
   - Update `IdentityServiceUrl` in `appsettings.Development.json` if needed

3. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

4. **Run the Service**

   ```bash
   dotnet run
   ```

5. **Verify Endpoints**
   - Gateway: `http://localhost:5001` (or configured port)
   - Test routes:
     - `GET http://localhost:5001/auctions` → Routes to AuctionService
     - `GET http://localhost:5001/search` → Routes to SearchService
     - `GET http://localhost:5001/bids/{auctionId}` → Routes to BiddingService

### Docker Deployment

1. **Build Image**

   ```bash
   docker build -t gateway-service .
   ```

2. **Run Container**
   ```bash
   docker run -p 5001:80 gateway-service
   ```

**Note:** Ensure all backend services and Identity Service are accessible from the container (use Docker network or update connection strings).

---

## Request Flow

### Example: GET /auctions

1. Client sends request to Gateway: `GET http://localhost:5001/auctions`
2. Gateway receives request at `/auctions`
3. Gateway matches route `auctions-read` (GET method, no auth required)
4. Gateway transforms path: `/auctions` → `/api/auctions`
5. Gateway forwards request to: `http://localhost:7001/api/auctions`
6. AuctionService processes request and returns response
7. Gateway returns response to client

### Example: POST /auctions (with authentication)

1. Client sends request with JWT token: `POST http://localhost:5001/auctions`
2. Gateway receives request at `/auctions`
3. Gateway matches route `auctions-write` (POST method, auth required)
4. Gateway validates JWT token with Identity Service
5. If valid, Gateway transforms path: `/auctions` → `/api/auctions`
6. Gateway forwards request to: `http://localhost:7001/api/auctions`
7. AuctionService processes request and returns response
8. Gateway returns response to client

---

## Path Transformations

The gateway rewrites request paths to match backend service routes:

| Gateway Route       | Transformed Path        | Backend Service Route   |
| ------------------- | ----------------------- | ----------------------- |
| `/auctions`         | `/api/auctions`         | `/api/auctions`         |
| `/auctions/{id}`    | `/api/auctions/{id}`    | `/api/auctions/{id}`    |
| `/search`           | `/api/search`           | `/api/search`           |
| `/search?term=ford` | `/api/search?term=ford` | `/api/search?term=ford` |
| `/bids`             | `/api/bids`             | `/api/bids`             |
| `/bids/{auctionId}` | `/api/bids/{auctionId}` | `/api/bids/{auctionId}` |

---

## Authentication & Authorization

### Public Endpoints (No Authentication Required)

- `GET /auctions` - List all auctions
- `GET /auctions/{id}` - Get auction by ID
- `GET /search` - Search auctions
- `GET /bids/{auctionId}` - Get bids for auction

### Protected Endpoints (Authentication Required)

- `POST /auctions` - Create auction
- `PUT /auctions/{id}` - Update auction
- `DELETE /auctions/{id}` - Delete auction
- `POST /bids` - Place bid

### JWT Token Validation

- Tokens are validated against Identity Service at `http://localhost:5000`
- Token must include `username` claim
- Invalid or missing tokens result in 401 Unauthorized

---

## Architecture Patterns

1. **API Gateway Pattern** - Single entry point for all client requests
2. **Reverse Proxy** - Transparent request forwarding to backend services
3. **Centralized Authentication** - JWT validation at gateway level
4. **Path Transformation** - URL rewriting for backend service compatibility
5. **Route-Based Authorization** - Different authorization policies per route

---

## Notes

- The gateway does not store any application data
- All business logic resides in backend services
- The gateway acts as a pure routing and authentication layer
- Path transformations ensure backend services receive correctly formatted routes
- Authorization policies are enforced before requests are forwarded
- The gateway supports both HTTP/1.1 and HTTP/2 protocols
