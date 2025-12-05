# BiddingService Technical Documentation

## Overview

BiddingService is a microservice built with .NET 10.0 that manages bidding functionality for vehicle auctions. It provides RESTful API endpoints for placing bids and retrieving bid history. The service uses MongoDB for data persistence, integrates with RabbitMQ for asynchronous messaging, and communicates with AuctionService via gRPC to validate auction information.

---

## Tech Stack

- **.NET 10.0** - Target framework
- **ASP.NET Core** - Web framework
- **MongoDB** - NoSQL database for bid storage
- **MongoDB.Entities** - MongoDB ORM library
- **gRPC** - High-performance RPC framework for inter-service communication
- **RabbitMQ** - Message broker for event-driven architecture
- **MassTransit** - Message bus abstraction layer
- **AutoMapper** - Object-to-object mapping
- **JWT Bearer Authentication** - Authentication mechanism

---

## NuGet Packages

| Package                                         | Version | Purpose                                                 |
| ----------------------------------------------- | ------- | ------------------------------------------------------- |
| `AutoMapper`                                    | 15.1.0  | Object mapping between entities and DTOs                |
| `Google.Protobuf`                               | 3.33.1  | Protocol Buffers support                                |
| `Grpc.Net.Client`                               | 2.71.0  | gRPC client implementation                              |
| `Grpc.Tools`                                    | 2.76.0  | gRPC code generation tools                              |
| `MassTransit.RabbitMQ`                          | 8.5.5   | MassTransit RabbitMQ transport                          |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.0  | JWT token authentication                                |
| `MongoDB.Entities`                              | 24.1.1  | MongoDB ORM library                                      |

**Project Reference:**

- `Contracts` - Shared contracts project for message definitions

---

## Models

### Bid

Represents a bid placed on an auction:

- `Id` (string) - Unique identifier (inherited from MongoDB.Entities.Entity)
- `AuctionId` (string) - Reference to the auction
- `Bidder` (string) - Username of the bidder
- `BidTime` (DateTime) - Timestamp when bid was placed (default: DateTime.UtcNow)
- `Amount` (int) - Bid amount
- `BidStatus` (BidStatus enum) - Current status of the bid

**Database Collection:** `Bids` (MongoDB)

### Auction

Represents auction information cached locally:

- `Id` (string) - Unique identifier (inherited from MongoDB.Entities.Entity)
- `AuctionEnd` (DateTime) - Scheduled end date/time
- `Seller` (string) - Username of the auction creator
- `ReservePrice` (int) - Minimum acceptable bid price
- `Finished` (bool) - Whether the auction has finished

**Database Collection:** `Auctions` (MongoDB)

### BidStatus (Enum)

Bid status values:

- `Accepted` - The bid was accepted and is the current highest bid
- `AcceptedBelowReserve` - The bid was accepted but is below the reserve price
- `TooLow` - The bid was not at least the current bid plus the increment
- `Finished` - The auction has finished

---

## DTOs (Data Transfer Objects)

### BidDto

Bid information returned to clients:

- `Id` (string) - Unique identifier
- `AuctionId` (string) - Reference to the auction
- `Bidder` (string) - Username of the bidder
- `BidTime` (DateTime) - Timestamp when bid was placed
- `Amount` (int) - Bid amount
- `BidStatus` (string) - String representation of bid status

---

## Controllers

### BidsController

**Base Route:** `/api/bids`

#### Endpoints:

1. **POST `/api/bids`**

   - Places a new bid on an auction
   - **Query Parameters:**
     - `auctionId` (string, required) - ID of the auction to bid on
     - `amount` (int, required) - Bid amount
   - **Returns:** `BidDto` with 200 OK or error response
   - **Authentication:** Required (JWT Bearer)
   - **Business Logic:**
     - Validates auction exists (checks MongoDB cache, falls back to gRPC call)
     - Prevents sellers from bidding on their own auctions
     - Determines bid status based on:
       - Auction end time (if finished, status is `Finished`)
       - Comparison with current high bid
       - Reserve price comparison
     - Publishes `BidPlaced` event to message bus
   - **Error Responses:**
     - 400 Bad Request - Auction not found or seller bidding on own auction
     - 401 Unauthorized - Missing or invalid JWT token

2. **GET `/api/bids/{auctionId}`**

   - Retrieves all bids for a specific auction
   - **Route Parameter:** `auctionId` (string) - ID of the auction
   - **Returns:** `List<BidDto>` sorted by bid time (descending)
   - **Authentication:** Not required

---

## Services

### GrpcAuctionClient

gRPC client for communicating with AuctionService.

**Methods:**

- `GetAuction(string id)` - Retrieves auction details by ID from AuctionService
  - **Request:** Auction ID (string)
  - **Response:** `Auction` model or null if not found
  - **Error Handling:** Logs errors and returns null on failure
  - **gRPC Endpoint:** Configured via `GrpcAuction` setting (default: `http://localhost:7777`)

### CheckAuctionFinished

Background service that periodically checks for finished auctions.

**Behavior:**

- Runs every 5 seconds
- Finds auctions where `AuctionEnd <= DateTime.UtcNow` and `Finished == false`
- For each finished auction:
  - Marks auction as finished (`Finished = true`)
  - Finds the winning bid (highest accepted bid)
  - Publishes `AuctionFinished` event to message bus with:
    - `ItemSold` - Whether a winning bid exists
    - `AuctionId` - ID of the finished auction
    - `Winner` - Username of winning bidder (if sold)
    - `Amount` - Winning bid amount (if sold)
    - `Seller` - Username of auction seller

---

## Consumers (Message Bus)

### AuctionCreatedConsumer

Consumes `AuctionCreated` messages from RabbitMQ.

**Purpose:** Caches auction information locally in MongoDB when a new auction is created.

**Behavior:**

- Creates a new `Auction` document in MongoDB
- Maps `AuctionCreated` message properties to `Auction` model
- Stores auction ID, seller, auction end date, and reserve price

---

## Data Layer

### MongoDB Configuration

- **Database Name:** `BidDb`
- **Connection:** Configured via `BidDbConnection` connection string
- **Collections:**
  - `Bids` - Bid documents
  - `Auctions` - Cached auction documents

**Initialization:**

- Database is initialized on application startup via `DB.InitAsync()`
- Uses MongoDB.Entities library for data access

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
    "BidDbConnection": "mongodb://root:mongoPwd@localhost"
  },
  "IdentityServiceUrl": "http://localhost:5000",
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "user",
    "Password": "rabbitPwd"
  },
  "GrpcAuction": "http://localhost:7777"
}
```

### Key Configuration Settings

1. **Database Connection**

   - Provider: MongoDB
   - Default connection string configured in `appsettings.Development.json`
   - Database name: `BidDb`

2. **RabbitMQ**

   - Host: `localhost`
   - Username: `user` (default: `guest`)
   - Password: `rabbitPwd` (default: `guest`)
   - Endpoint naming: Kebab case with "bids" prefix

3. **MassTransit**

   - Uses RabbitMQ transport
   - Message retry: 5 retries with 10-second intervals for connection exceptions
   - Endpoint naming: Kebab case with "bids" prefix

4. **JWT Authentication**

   - Authority: `http://localhost:5000` (Identity Service)
   - HTTPS metadata: Disabled (development)
   - Audience validation: Disabled
   - Name claim type: `username`

5. **gRPC Client**
   - Auction Service endpoint: `http://localhost:7777`

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
- Entry point: `dotnet BiddingService.dll`

**Note:** The Dockerfile references `carsties.sln` and expects the project structure:

```
.
├── carsties.sln
├── src/
│   ├── BiddingService/
│   │   └── BiddingService.csproj
│   └── Contracts/
│       └── Contracts.csproj
```

---

## Getting Started

### Prerequisites

1. **.NET 10.0 SDK**
2. **MongoDB** (running on localhost)
3. **RabbitMQ** (running on localhost)
4. **Identity Service** (running on http://localhost:5000) - for JWT authentication
5. **Auction Service** (running on http://localhost:7777) - for gRPC communication

### Setup Steps

1. **Configure Database**

   - Ensure MongoDB is running
   - Update connection string in `appsettings.Development.json` if needed
   - Database will be created automatically on first connection

2. **Configure RabbitMQ**

   - Ensure RabbitMQ is running
   - Update RabbitMQ settings in `appsettings.Development.json` if needed

3. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

4. **Run the Service**

   ```bash
   dotnet run
   ```

5. **Verify Endpoints**
   - Web API: `http://localhost:7003/api/bids`

### Docker Deployment

1. **Build Image**

   ```bash
   docker build -t bidding-service .
   ```

2. **Run Container**
   ```bash
   docker run -p 7003:80 bidding-service
   ```

**Note:** Ensure MongoDB, RabbitMQ, Identity Service, and Auction Service are accessible from the container (use Docker network or update connection strings).

---

## Architecture Patterns

1. **CQRS-like** - Separate read (DTOs) and write (Models) models
2. **Event-Driven** - Consumes and publishes events for auction lifecycle changes
3. **Microservice Communication** - gRPC for inter-service calls, RabbitMQ for events
4. **Background Processing** - Background service for periodic auction completion checks
5. **Caching** - Local MongoDB cache of auction information to reduce gRPC calls

---

## Message Contracts

The service publishes and consumes messages defined in the `Contracts` project:

**Published Events:**

- `BidPlaced` - When a new bid is placed on an auction
- `AuctionFinished` - When an auction completes (published by background service)

**Consumed Events:**

- `AuctionCreated` - When a new auction is created (caches auction info)

---

## AutoMapper Configuration

Mapping profiles defined in `RequestHelpers/MappingProfiles.cs`:

- `Bid` → `BidDto`
- `Bid` → `BidPlaced`

---

## Bid Status Logic

The service determines bid status using the following logic:

1. **If auction has ended:**
   - Status: `Finished`

2. **If auction is still active:**
   - **If no current high bid exists OR new bid > current high bid:**
     - If bid amount > reserve price: Status = `Accepted`
     - If bid amount <= reserve price: Status = `AcceptedBelowReserve`
   - **If new bid <= current high bid:**
     - Status: `TooLow`

---

## Notes

- The service uses MongoDB for fast, scalable bid storage
- Auction information is cached locally to reduce gRPC calls
- Background service ensures auctions are marked as finished even if no bids are placed
- JWT authentication is required for placing bids
- Read operations (GET bids) are publicly accessible
- The service falls back to gRPC calls if auction is not found in local cache

