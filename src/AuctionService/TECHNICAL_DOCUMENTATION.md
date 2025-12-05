# AuctionService Technical Documentation

## Overview

AuctionService is a microservice built with .NET 10.0 that manages vehicle auctions. It provides RESTful API endpoints and gRPC services for creating, reading, updating, and deleting auctions. The service integrates with RabbitMQ for asynchronous messaging and uses PostgreSQL for data persistence.

---

## Tech Stack

- **.NET 10.0** - Target framework
- **ASP.NET Core** - Web framework
- **Entity Framework Core** - ORM for database operations
- **PostgreSQL** - Relational database
- **gRPC** - High-performance RPC framework
- **RabbitMQ** - Message broker for event-driven architecture
- **MassTransit** - Message bus abstraction layer
- **AutoMapper** - Object-to-object mapping
- **JWT Bearer Authentication** - Authentication mechanism

---

## NuGet Packages

| Package                                         | Version | Purpose                                                 |
| ----------------------------------------------- | ------- | ------------------------------------------------------- |
| `AutoMapper`                                    | 15.1.0  | Object mapping between entities and DTOs                |
| `Grpc.AspNetCore`                               | 2.71.0  | gRPC server implementation                              |
| `MassTransit.EntityFrameworkCore`               | 8.5.5   | MassTransit integration with EF Core for outbox pattern |
| `MassTransit.RabbitMQ`                          | 8.5.5   | MassTransit RabbitMQ transport                          |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.0  | JWT token authentication                                |
| `Microsoft.EntityFrameworkCore.Design`          | 10.0.0  | EF Core design-time tools for migrations                |
| `Npgsql.EntityFrameworkCore.PostgreSQL`         | 10.0.0  | PostgreSQL database provider for EF Core                |

**Project Reference:**

- `Contracts` - Shared contracts project for message definitions

---

## Entities

### Auction

Represents an auction listing with the following properties:

- `Id` (Guid) - Unique identifier
- `ReservePrice` (int) - Minimum acceptable bid price (default: 0)
- `Seller` (string) - Username of the auction creator
- `Winner` (string) - Username of the winning bidder (set when auction finishes)
- `SoldAmount` (int) - Final sale price
- `CurrentHighBid` (int) - Current highest bid amount
- `CreatedAt` (DateTime) - Creation timestamp (UTC)
- `UpdatedAt` (DateTime) - Last update timestamp (UTC)
- `AuctionEnd` (DateTime) - Scheduled end date/time
- `Status` (Status enum) - Current auction status
- `Item` (Item) - Navigation property to the auctioned item

**Methods:**

- `HasReservePrice()` - Returns true if reserve price is greater than 0

**Database Table:** `Auctions`

### Item

Represents the vehicle/item being auctioned:

- `Id` (Guid) - Unique identifier
- `Make` (string) - Vehicle manufacturer (e.g., "Ford", "BMW")
- `Model` (string) - Vehicle model (e.g., "Mustang", "X1")
- `Year` (int) - Manufacturing year
- `Color` (string) - Vehicle color
- `Mileage` (int) - Vehicle mileage
- `ImageUrl` (string) - URL to vehicle image
- `AuctionId` (Guid) - Foreign key to Auction
- `Auction` (Auction) - Navigation property

**Database Table:** `Items`

### Status (Enum)

Auction status values:

- `Live` - Auction is currently active
- `Finished` - Auction has completed successfully
- `ReserveNotMet` - Auction ended but reserve price was not met

---

## DTOs (Data Transfer Objects)

### AuctionDto

Complete auction information including item details:

- `Id`, `ReservePrice`, `Seller`, `Winner`, `SoldAmount`, `CurrentHighBid`
- `CreatedAt`, `UpdatedAt`, `AuctionEnd`
- `Make`, `Model`, `Year`, `Color`, `ImageUrl` (from Item)
- `Status` (string representation)

### CreateAuctionDto

Required fields for creating a new auction:

- `Make` (required)
- `Model` (required)
- `Color` (required)
- `Mileage` (required)
- `Year` (required)
- `ReservePrice` (required)
- `ImageUrl` (required)
- `AuctionEnd` (required)

### UpdateAuctionDto

Optional fields for updating auction item details:

- `Make` (optional)
- `Model` (optional)
- `Color` (optional)
- `Mileage` (optional, nullable)
- `Year` (optional, nullable)

---

## Controllers

### AuctionsController

**Base Route:** `/api/auctions`

#### Endpoints:

1. **GET `/api/auctions`**

   - Retrieves all auctions
   - **Query Parameter:** `date` (optional) - Filter auctions updated after this date
   - **Returns:** `List<AuctionDto>`
   - **Authentication:** Not required

2. **GET `/api/auctions/{id}`**

   - Retrieves a specific auction by ID
   - **Returns:** `AuctionDto` or 404 Not Found
   - **Authentication:** Not required

3. **POST `/api/auctions`**

   - Creates a new auction
   - **Body:** `CreateAuctionDto`
   - **Returns:** `AuctionDto` with 201 Created
   - **Authentication:** Required (JWT Bearer)
   - **Side Effects:** Publishes `AuctionCreated` event to message bus

4. **PUT `/api/auctions/{id}`**

   - Updates an existing auction's item details
   - **Body:** `UpdateAuctionDto`
   - **Returns:** 200 OK or 404 Not Found or 403 Forbid
   - **Authentication:** Required (JWT Bearer)
   - **Authorization:** Only the seller can update their auction
   - **Side Effects:** Publishes `AuctionUpdated` event to message bus

5. **DELETE `/api/auctions/{id}`**
   - Deletes an auction
   - **Returns:** 200 OK or 404 Not Found or 403 Forbid
   - **Authentication:** Required (JWT Bearer)
   - **Authorization:** Only the seller can delete their auction
   - **Side Effects:** Publishes `AuctionDeleted` event to message bus

---

## Services

### GrpcAuctionService

Implements the gRPC service defined in `protos/auctions.proto`.

**Service:** `GrpcAuction`

**Methods:**

- `GetAuction(GetAuctionRequest)` - Retrieves auction details by ID
  - **Request:** `id` (string)
  - **Response:** `GrpcAuctionResponse` containing:
    - `id`, `seller`, `auctionEnd`, `reservePrice`
  - **Error:** Returns `NotFound` status if auction doesn't exist

**gRPC Endpoint:** `http://localhost:7777` (HTTP/2)

---

## Consumers (Message Bus)

### AuctionFinishedConsumer

Consumes `AuctionFinished` messages from RabbitMQ.

**Purpose:** Updates auction status when an auction finishes.

**Behavior:**

- Updates `Winner` and `SoldAmount` if item was sold
- Sets `Status` to `Finished` if sold amount meets reserve price
- Sets `Status` to `ReserveNotMet` if sold amount is below reserve price

### BidPlacedConsumer

Consumes `BidPlaced` messages from RabbitMQ.

**Purpose:** Updates the current high bid when a new bid is placed.

**Behavior:**

- Updates `CurrentHighBid` if:
  - No current high bid exists (value is 0), OR
  - New bid is accepted AND higher than current high bid
- Updates `UpdatedAt` timestamp

---

## Data Layer

### AuctionDbContext

Entity Framework Core database context.

**DbSets:**

- `Auctions` - Auction entities
- `Items` - Item entities

**Features:**

- Configures MassTransit outbox pattern entities:
  - `InboxStateEntity`
  - `OutboxMessageEntity`
  - `OutboxStateEntity`

### AuctionRepository

Repository pattern implementation for auction data access.

**Methods:**

- `GetAllAuctionsAsync(string date)` - Get all auctions, optionally filtered by update date
- `GetAuctionByIdAsync(Guid id)` - Get auction DTO by ID
- `GetAuctionEntityById(Guid id)` - Get auction entity with item included
- `AddAuction(Auction)` - Add new auction to context
- `RemoveAuction(Auction)` - Mark auction for deletion
- `SaveChangesAsync()` - Persist changes to database

**Features:**

- Uses AutoMapper for projection to DTOs
- Includes Item navigation property when needed

### DbInitializer

Database initialization and seeding.

**Behavior:**

- Runs database migrations on startup
- Seeds 10 sample auctions if database is empty
- Includes various vehicle makes/models (Ford, Bugatti, Mercedes, BMW, Ferrari, Audi)

---

## Configuration

### appsettings.json

Base configuration file (minimal):

- Logging configuration
- Allowed hosts: `*`

### appsettings.Development.json

Development-specific configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost:5432; User Id=postgres;Password=postgresPwd;Database=Auctions;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "user",
    "Password": "rabbitPwd"
  },
  "IdentityServiceUrl": "http://localhost:5000",
  "AutoMapper": {
    "LicenseKey": ""
  },
  "Kestrel": {
    "Endpoints": {
      "Grpc": {
        "Protocols": "Http2",
        "Url": "http://localhost:7777"
      },
      "WebApi": {
        "Protocols": "Http1",
        "Url": "http://localhost:7001"
      }
    }
  }
}
```

### Key Configuration Settings

1. **Database Connection**

   - Provider: PostgreSQL
   - Default connection string configured in `appsettings.Development.json`
   - Database name: `Auctions`

2. **RabbitMQ**

   - Host: `localhost`
   - Username: `user` (default: `guest`)
   - Password: `rabbitPwd` (default: `guest`)

3. **MassTransit**

   - Uses Entity Framework outbox pattern
   - Query delay: 10 seconds
   - Uses PostgreSQL for outbox persistence
   - Endpoint naming: Kebab case with "auction" prefix

4. **JWT Authentication**

   - Authority: `http://localhost:5000` (Identity Service)
   - HTTPS metadata: Disabled (development)
   - Audience validation: Disabled
   - Name claim type: `username`

5. **Kestrel Endpoints**

   - **gRPC:** `http://localhost:7777` (HTTP/2)
   - **Web API:** `http://localhost:7001` (HTTP/1)

6. **AutoMapper**
   - License key configured (can be empty for development)

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
- Entry point: `dotnet AuctionService.dll`

**Note:** The Dockerfile references `carsties.sln` and expects the project structure:

```
.
├── carsties.sln
├── src/
│   ├── AuctionService/
│   │   └── AuctionService.csproj
│   └── Contracts/
│       └── Contracts.csproj
```

---

## Getting Started

### Prerequisites

1. **.NET 10.0 SDK**
2. **PostgreSQL** (running on localhost:5432)
3. **RabbitMQ** (running on localhost)
4. **Identity Service** (running on http://localhost:5000) - for JWT authentication

### Setup Steps

1. **Configure Database**

   - Ensure PostgreSQL is running
   - Update connection string in `appsettings.Development.json` if needed
   - Database will be created automatically via migrations

2. **Configure RabbitMQ**

   - Ensure RabbitMQ is running
   - Update RabbitMQ settings in `appsettings.Development.json` if needed

3. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

4. **Run Migrations**

   - Migrations run automatically on startup via `DbInitializer`
   - Or manually: `dotnet ef database update`

5. **Run the Service**

   ```bash
   dotnet run
   ```

6. **Verify Endpoints**
   - Web API: `http://localhost:7001/api/auctions`
   - gRPC: `http://localhost:7777`

### Docker Deployment

1. **Build Image**

   ```bash
   docker build -t auction-service .
   ```

2. **Run Container**
   ```bash
   docker run -p 7001:80 -p 7777:7777 auction-service
   ```

**Note:** Ensure PostgreSQL, RabbitMQ, and Identity Service are accessible from the container (use Docker network or update connection strings).

---

## Architecture Patterns

1. **Repository Pattern** - Data access abstraction via `IAuctionRepository`
2. **Outbox Pattern** - MassTransit outbox ensures reliable message delivery
3. **CQRS-like** - Separate read (DTOs) and write (Entities) models
4. **Event-Driven** - Publishes events for auction lifecycle changes
5. **Microservice Communication** - gRPC for inter-service calls, RabbitMQ for events

---

## Message Contracts

The service publishes and consumes messages defined in the `Contracts` project:

**Published Events:**

- `AuctionCreated` - When a new auction is created
- `AuctionUpdated` - When auction item details are updated
- `AuctionDeleted` - When an auction is deleted

**Consumed Events:**

- `AuctionFinished` - When an auction completes
- `BidPlaced` - When a bid is placed on an auction

---

## AutoMapper Configuration

Mapping profiles defined in `RequestHelpers/MappingProfiles.cs`:

- `Auction` → `AuctionDto` (includes Item properties)
- `Auction` → `AuctionUpdated` (includes Item properties)
- `CreateAuctionDto` → `Auction` (maps to Auction and Item)
- `CreateAuctionDto` → `Item`
- `AuctionDto` → `AuctionCreated`
- `Item` → `AuctionDto`, `AuctionUpdated`, `AuctionDeleted`

---

## Notes

- The service uses Entity Framework Core migrations for database schema management
- MassTransit outbox pattern ensures at-least-once message delivery
- JWT authentication is required for write operations (POST, PUT, DELETE)
- Read operations (GET) are publicly accessible
- Database seeding occurs automatically on first startup if database is empty
