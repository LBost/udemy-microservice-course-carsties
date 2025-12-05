# SearchService Technical Documentation

## Overview

SearchService is a microservice built with .NET 10.0 that provides search functionality for vehicle auctions. It maintains a searchable index of auction items in MongoDB and provides RESTful API endpoints for querying auctions with various filters, sorting options, and full-text search capabilities. The service synchronizes with AuctionService via HTTP and RabbitMQ events to keep the search index up-to-date.

---

## Tech Stack

- **.NET 10.0** - Target framework
- **ASP.NET Core** - Web framework
- **MongoDB** - NoSQL database for search index
- **MongoDB.Entities** - MongoDB ORM library
- **RabbitMQ** - Message broker for event-driven architecture
- **MassTransit** - Message bus abstraction layer
- **AutoMapper** - Object-to-object mapping

---

## NuGet Packages

| Package                    | Version | Purpose                                                 |
| -------------------------- | ------- | ------------------------------------------------------- |
| `AutoMapper`               | 15.1.0  | Object mapping between contracts and models             |
| `MassTransit.RabbitMQ`     | 8.5.5   | MassTransit RabbitMQ transport                          |
| `MongoDB.Entities`         | 24.1.1  | MongoDB ORM library                                      |

**Project Reference:**

- `Contracts` - Shared contracts project for message definitions

---

## Models

### Item

Represents a searchable auction item in MongoDB:

- `Id` (string) - Unique identifier (inherited from MongoDB.Entities.Entity)
- `ReservePrice` (int) - Minimum acceptable bid price
- `Seller` (string) - Username of the auction creator
- `Winner` (string) - Username of the winning bidder (set when auction finishes)
- `SoldAmount` (int) - Final sale price
- `CurrentHighBid` (int) - Current highest bid amount
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime) - Last update timestamp
- `AuctionEnd` (DateTime) - Scheduled end date/time
- `Make` (string) - Vehicle manufacturer (indexed for full-text search)
- `Model` (string) - Vehicle model (indexed for full-text search)
- `Mileage` (int) - Vehicle mileage
- `Year` (int) - Manufacturing year
- `Color` (string) - Vehicle color (indexed for full-text search)
- `ImageUrl` (string) - URL to vehicle image
- `Status` (string) - Auction status ("Live", "Finished", "ReserveNotMet")

**Database Collection:** `Items` (MongoDB)

**Indexes:**

- Full-text search index on: `Make`, `Model`, `Color`

---

## DTOs (Data Transfer Objects)

The service returns `Item` models directly in search results. No separate DTOs are used.

---

## Controllers

### SearchController

**Base Route:** `/api/search`

#### Endpoints:

1. **GET `/api/search`**

   - Searches and filters auction items
   - **Query Parameters:**
     - `SearchTerm` (string, optional) - Full-text search query
     - `PageSize` (int, optional) - Results per page (default: 4)
     - `PageNumber` (int, optional) - Page number (default: 1)
     - `Seller` (string, optional) - Filter by seller username
     - `Winner` (string, optional) - Filter by winner username
     - `OrderBy` (string, optional) - Sort order:
       - `"make"` - Sort by make, then model (ascending)
       - `"new"` - Sort by creation date (descending)
       - Default: Sort by auction end date (ascending)
     - `FilterBy` (string, optional) - Filter by auction status:
       - `"finished"` - Show only finished auctions
       - `"endingSoon"` - Show auctions ending within 6 hours
       - Default: Show only live auctions (ending in future)
   - **Returns:** JSON object with:
     - `result` - Array of `Item` objects
     - `pageCount` - Total number of pages
     - `pageSize` - Total number of items (note: this appears to be total count, not page size)
   - **Authentication:** Not required

**Search Logic:**

- **Full-Text Search:** If `SearchTerm` is provided, searches across `Make`, `Model`, and `Color` fields, sorted by text score
- **Filtering:** Applies filters based on `FilterBy`, `Seller`, and `Winner` parameters
- **Sorting:** Applies sorting based on `OrderBy` parameter
- **Pagination:** Supports pagination with `PageNumber` and `PageSize`

---

## Services

### AuctionServiceHttpClient

HTTP client for synchronizing data with AuctionService.

**Methods:**

- `GetItemsForSearchDb()` - Retrieves auction items from AuctionService
  - **Behavior:**
    - Checks for most recently updated item in MongoDB
    - If items exist, requests only items updated after the last update timestamp
    - If no items exist, requests all auctions
    - Returns list of `Item` objects
  - **Endpoint:** Configured via `AuctionServiceUrl` setting (default: `http://localhost:7001`)
  - **Query Parameter:** `date` - Last update timestamp (if applicable)

**Purpose:** Initial synchronization and periodic updates of search index.

---

## Consumers (Message Bus)

### AuctionCreatedConsumer

Consumes `AuctionCreated` messages from RabbitMQ.

**Purpose:** Adds new auction items to the search index when auctions are created.

**Behavior:**

- Maps `AuctionCreated` message to `Item` model using AutoMapper
- Saves item to MongoDB
- Throws `MessageException` on failure (triggers retry)

**Endpoint:** `search-auction-created`
**Retry Policy:** 5 retries with 5-second intervals

### AuctionUpdatedConsumer

Consumes `AuctionUpdated` messages from RabbitMQ.

**Purpose:** Updates auction item details in the search index when auctions are updated.

**Behavior:**

- Maps `AuctionUpdated` message to `Item` model using AutoMapper
- Updates only specific fields: `Make`, `Model`, `Color`, `Mileage`, `Year`
- Uses MongoDB partial update
- Throws `MessageException` on failure (triggers retry)

**Endpoint:** `search-auction-updated`
**Retry Policy:** 5 retries with 5-second intervals

### AuctionDeletedConsumer

Consumes `AuctionDeleted` messages from RabbitMQ.

**Purpose:** Removes auction items from the search index when auctions are deleted.

**Behavior:**

- Deletes item from MongoDB by ID
- Throws `MessageException` on failure (triggers retry)

**Endpoint:** `search-auction-deleted`
**Retry Policy:** 5 retries with 5-second intervals

### AuctionFinishedConsumer

Consumes `AuctionFinished` messages from RabbitMQ.

**Purpose:** Updates auction status and winner information when auctions finish.

**Behavior:**

- Finds auction item in MongoDB
- If item was sold:
  - Updates `Winner` field
  - Updates `SoldAmount` field
- Updates `Status` field:
  - `"Finished"` if sold amount meets reserve price
  - `"ReserveNotMet"` if sold amount is below reserve price
- Saves updated item

### BidPlacedConsumer

Consumes `BidPlaced` messages from RabbitMQ.

**Purpose:** Updates current high bid when bids are placed.

**Behavior:**

- Finds auction item in MongoDB
- If bid status contains "Accepted" and bid amount > current high bid:
  - Updates `CurrentHighBid` field
  - Saves updated item

---

## Data Layer

### MongoDB Configuration

- **Database Name:** `SearchDb`
- **Connection:** Configured via `MongoDbConnection` connection string
- **Collection:** `Items` - Auction item documents

**Initialization:**

- Database is initialized on application startup via `DbInitializer.InitDb()`
- Creates full-text search indexes on `Make`, `Model`, and `Color` fields
- Synchronizes initial data from AuctionService via HTTP

### DbInitializer

Database initialization and synchronization.

**Behavior:**

1. Initializes MongoDB connection
2. Creates full-text search indexes:
   - `Make` (text index)
   - `Model` (text index)
   - `Color` (text index)
3. Checks if database is empty
4. If empty or needs update:
   - Calls `AuctionServiceHttpClient.GetItemsForSearchDb()`
   - Saves retrieved items to MongoDB

**Note:** Commented-out code suggests previous support for seeding from JSON file.

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
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "MongoDbConnection": "mongodb://root:mongoPwd@localhost"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "user",
    "Password": "rabbitPwd"
  },
  "AuctionServiceUrl": "http://localhost:7001"
}
```

### Key Configuration Settings

1. **Database Connection**

   - Provider: MongoDB
   - Default connection string configured in `appsettings.Development.json`
   - Database name: `SearchDb`

2. **RabbitMQ**

   - Host: `localhost`
   - Username: `user` (default: `guest`)
   - Password: `rabbitPwd` (default: `guest`)
   - Endpoint naming: Kebab case with "search" prefix

3. **MassTransit**

   - Uses RabbitMQ transport
   - Custom receive endpoints with retry policies:
     - `search-auction-created` - 5 retries, 5-second intervals
     - `search-auction-updated` - 5 retries, 5-second intervals
     - `search-auction-deleted` - 5 retries, 5-second intervals
   - Endpoint naming: Kebab case with "search" prefix

4. **Auction Service**
   - HTTP endpoint: `http://localhost:7001`
   - Used for initial synchronization and incremental updates

5. **AutoMapper**
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
- Entry point: `dotnet SearchService.dll`

**Note:** The Dockerfile references `carsties.sln` and expects the project structure:

```
.
├── carsties.sln
├── src/
│   ├── SearchService/
│   │   └── SearchService.csproj
│   └── Contracts/
│       └── Contracts.csproj
```

---

## Getting Started

### Prerequisites

1. **.NET 10.0 SDK**
2. **MongoDB** (running on localhost)
3. **RabbitMQ** (running on localhost)
4. **Auction Service** (running on http://localhost:7001) - for initial synchronization

### Setup Steps

1. **Configure Database**

   - Ensure MongoDB is running
   - Update connection string in `appsettings.Development.json` if needed
   - Database will be created automatically on first connection

2. **Configure RabbitMQ**

   - Ensure RabbitMQ is running
   - Update RabbitMQ settings in `appsettings.Development.json` if needed

3. **Configure Auction Service**

   - Ensure AuctionService is running
   - Update `AuctionServiceUrl` in `appsettings.Development.json` if needed

4. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

5. **Run the Service**

   ```bash
   dotnet run
   ```

6. **Verify Endpoints**
   - Web API: `http://localhost:7002/api/search`

### Docker Deployment

1. **Build Image**

   ```bash
   docker build -t search-service .
   ```

2. **Run Container**
   ```bash
   docker run -p 7002:80 search-service
   ```

**Note:** Ensure MongoDB, RabbitMQ, and AuctionService are accessible from the container (use Docker network or update connection strings).

---

## Search Features

### Full-Text Search

- Searches across `Make`, `Model`, and `Color` fields
- Results sorted by text relevance score
- Case-insensitive matching

### Filtering Options

1. **By Status:**
   - `"finished"` - Completed auctions
   - `"endingSoon"` - Auctions ending within 6 hours
   - Default: Live auctions (ending in future)

2. **By Seller:** Filter by seller username
3. **By Winner:** Filter by winner username

### Sorting Options

1. **`"make"`** - Sort by make (ascending), then model (ascending)
2. **`"new"`** - Sort by creation date (descending)
3. **Default** - Sort by auction end date (ascending)

### Pagination

- Configurable page size (default: 4 items per page)
- Page number-based navigation
- Returns total page count and total item count

---

## Architecture Patterns

1. **CQRS-like** - Separate read-optimized search index
2. **Event-Driven** - Consumes events for real-time index updates
3. **Event Sourcing** - Maintains search index as projection of auction events
4. **Microservice Communication** - HTTP for initial sync, RabbitMQ for events
5. **Search Optimization** - Full-text search indexes for fast queries

---

## Message Contracts

The service consumes messages defined in the `Contracts` project:

**Consumed Events:**

- `AuctionCreated` - When a new auction is created
- `AuctionUpdated` - When auction item details are updated
- `AuctionDeleted` - When an auction is deleted
- `AuctionFinished` - When an auction completes
- `BidPlaced` - When a bid is placed on an auction

---

## AutoMapper Configuration

Mapping profiles defined in `RequestHelpers/MappingProfiles.cs`:

- `AuctionCreated` → `Item`
- `AuctionUpdated` → `Item`

---

## Data Synchronization

### Initial Synchronization

On startup, the service:

1. Checks MongoDB for existing items
2. If empty or needs update:
   - Calls AuctionService HTTP API
   - Retrieves all auctions or auctions updated since last sync
   - Saves items to MongoDB

### Real-Time Updates

After initial sync, the service stays synchronized via RabbitMQ events:

- **AuctionCreated** → Adds new item
- **AuctionUpdated** → Updates item details
- **AuctionDeleted** → Removes item
- **AuctionFinished** → Updates status and winner
- **BidPlaced** → Updates current high bid

---

## Notes

- The service uses MongoDB for fast, scalable search operations
- Full-text search indexes enable efficient querying across vehicle attributes
- The service maintains eventual consistency with AuctionService via events
- Initial synchronization ensures search index is populated on first startup
- All search operations are publicly accessible (no authentication required)
- The service is optimized for read-heavy workloads
- Retry policies ensure reliable message processing

