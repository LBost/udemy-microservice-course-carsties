# Carsties Microservices Architecture Guide

## Table of Contents

1. [What Are Microservices?](#what-are-microservices)
2. [Key Microservices Concepts](#key-microservices-concepts)
3. [Carsties Project Overview](#carsties-project-overview)
4. [System Architecture](#system-architecture)
5. [Service-by-Service Breakdown](#service-by-service-breakdown)
6. [How Services Communicate](#how-services-communicate)
7. [Data Flow Examples](#data-flow-examples)
8. [Key Patterns and Practices](#key-patterns-and-practices)
9. [Technology Choices Explained](#technology-choices-explained)
10. [Learning Path](#learning-path)

---

## What Are Microservices?

### Traditional Monolithic Architecture

In a **monolithic application**, everything runs as a single unit:

```
┌─────────────────────────────────────┐
│     Single Application               │
│  ┌─────────┐  ┌─────────┐          │
│  │ Users   │  │ Auctions │          │
│  └─────────┘  └─────────┘          │
│  ┌─────────┐  ┌─────────┐          │
│  │ Bids    │  │ Search  │          │
│  └─────────┘  └─────────┘          │
│                                     │
│  ┌─────────────────────────────┐    │
│  │     Single Database         │    │
│  └─────────────────────────────┘    │
└─────────────────────────────────────┘
```

**Problems:**
- All code deployed together
- One bug can bring down everything
- Hard to scale individual features
- Technology lock-in (one tech stack)
- Slow development (teams step on each other)

### Microservices Architecture

In a **microservices architecture**, the application is split into independent services:

```
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│ Identity │  │ Auction  │  │ Bidding  │  │  Search  │
│ Service  │  │ Service  │  │ Service  │  │ Service  │
└──────────┘  └──────────┘  └──────────┘  └──────────┘
     │             │              │              │
     │             │              │              │
┌────┴─────┐  ┌────┴─────┐  ┌────┴─────┐  ┌────┴─────┐
│PostgreSQL│  │PostgreSQL│  │ MongoDB  │  │ MongoDB  │
└──────────┘  └──────────┘  └──────────┘  └──────────┘
```

**Benefits:**
- Independent deployment
- Technology diversity (each service can use different tech)
- Scalability (scale only what you need)
- Fault isolation (one service failure doesn't kill everything)
- Team autonomy (teams work independently)

---

## Key Microservices Concepts

### 1. Service Independence

Each microservice:
- **Owns its data** - Has its own database
- **Can be deployed independently** - No need to deploy everything together
- **Can fail independently** - Other services continue working
- **Can be scaled independently** - Scale only the services that need it

**In Carsties:**
- AuctionService uses PostgreSQL
- BiddingService uses MongoDB
- SearchService uses MongoDB
- Each can be updated without affecting others

### 2. Service Communication

Services need to talk to each other. Two main approaches:

#### Synchronous Communication (Request/Response)
- **HTTP/REST** - Like a phone call, you wait for an answer
- **gRPC** - Faster, more efficient than REST
- **When to use:** When you need an immediate response

**Example in Carsties:**
- BiddingService calls AuctionService via gRPC to check if auction exists
- Gateway routes requests to backend services via HTTP

#### Asynchronous Communication (Events/Messages)
- **Message Broker (RabbitMQ)** - Like a mailbox, you send a message and continue
- **When to use:** When you don't need an immediate response, or for decoupling

**Example in Carsties:**
- AuctionService publishes "AuctionCreated" event
- SearchService and BiddingService consume it later
- Services don't need to know about each other

### 3. API Gateway Pattern

**Problem:** Clients would need to know about all services and their endpoints

**Solution:** Single entry point that routes requests

```
Client → Gateway → Backend Services
```

**In Carsties:**
- All requests go through GatewayService (port 6001)
- Gateway routes to appropriate service
- Gateway handles authentication centrally

### 4. Database per Service

**Rule:** Each service has its own database

**Why?**
- Services can use different database types (SQL vs NoSQL)
- No shared database = no tight coupling
- Services can change their schema without affecting others

**In Carsties:**
- AuctionService → PostgreSQL (relational data)
- IdentityService → PostgreSQL (user data)
- BiddingService → MongoDB (document data)
- SearchService → MongoDB (search index)

### 5. Event-Driven Architecture

Services communicate via events (messages):

```
Service A publishes event → Message Broker → Service B consumes event
```

**Benefits:**
- **Loose coupling** - Services don't directly depend on each other
- **Scalability** - Can add more consumers easily
- **Resilience** - If one service is down, messages wait in queue

**In Carsties:**
- AuctionService publishes: `AuctionCreated`, `AuctionUpdated`, `AuctionDeleted`
- BiddingService publishes: `BidPlaced`, `AuctionFinished`
- SearchService consumes all auction events to keep search index updated

### 6. Distributed Data Management

**Challenge:** Data is spread across services

**Solutions:**
- **Eventual Consistency** - Data syncs eventually (not immediately)
- **CQRS** - Separate read and write models
- **Event Sourcing** - Rebuild state from events

**In Carsties:**
- SearchService maintains its own copy of auction data (eventual consistency)
- When auction updates, event is published and SearchService updates its copy
- This allows fast searches without querying AuctionService

---

## Carsties Project Overview

### What is Carsties?

Carsties is a **vehicle auction platform** built as a microservices application. Users can:
- Create auctions for vehicles
- Search for auctions
- Place bids on auctions
- View auction details and bid history

### High-Level Architecture

```
                    ┌──────────────┐
                    │   Client     │
                    │  (Browser)   │
                    └──────┬───────┘
                           │
                           │ HTTP
                           ▼
              ┌────────────────────────┐
              │   Gateway Service     │
              │   (Port 6001)         │
              │   - Routes requests    │
              │   - Handles auth       │
              └──────┬─────────────────┘
                     │
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
┌──────────┐  ┌──────────┐  ┌──────────┐
│ Identity │  │ Auction  │  │ Bidding  │
│ Service  │  │ Service  │  │ Service  │
│ (5000)   │  │ (7001)   │  │ (7003)   │
└──────────┘  └──────────┘  └──────────┘
                     │
                     ▼
              ┌──────────┐
              │  Search  │
              │ Service  │
              │ (7002)   │
              └──────────┘

Infrastructure:
  - PostgreSQL (port 5432)
  - MongoDB (port 27017)
  - RabbitMQ (port 5672)
```

### Services Overview

| Service | Purpose | Database | Port |
|---------|---------|----------|------|
| **GatewayService** | API Gateway, routes requests | None | 6001 |
| **IdentityService** | Authentication & Authorization | PostgreSQL | 5000 |
| **AuctionService** | Manage auctions (CRUD) | PostgreSQL | 7001, 7777 |
| **BiddingService** | Handle bids | MongoDB | 7003 |
| **SearchService** | Search auctions | MongoDB | 7002 |

---

## System Architecture

### Complete System Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        Client Layer                          │
│                    (Web Application)                         │
└────────────────────────┬────────────────────────────────────┘
                         │
                         │ HTTP Requests
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    Gateway Service                           │
│  - Single entry point                                        │
│  - Request routing                                           │
│  - JWT authentication                                        │
└─────┬──────────┬──────────┬──────────┬──────────────────────┘
      │          │          │          │
      │          │          │          │
      ▼          ▼          ▼          ▼
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│ Identity │ │ Auction  │ │ Bidding  │ │  Search  │
│ Service  │ │ Service  │ │ Service  │ │ Service  │
└────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘
     │            │             │             │
     │            │             │             │
     ▼            ▼             ▼             ▼
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│PostgreSQL│ │PostgreSQL│ │ MongoDB  │ │ MongoDB  │
│(Identity)│ │(Auctions)│ │  (Bids)  │ │ (Search) │
└──────────┘ └──────────┘ └──────────┘ └──────────┘

                    ┌──────────────┐
                    │   RabbitMQ   │
                    │ Message Bus  │
                    └──────┬───────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
        ┌──────────┐ ┌──────────┐ ┌──────────┐
        │ Auction  │ │ Bidding  │ │  Search  │
        │ Service  │ │ Service  │ │ Service  │
        │(Publishes│ │(Publishes│ │(Consumes)│
        │  Events) │ │  Events) │ │  Events) │
        └──────────┘ └──────────┘ └──────────┘
```

### Communication Patterns

#### 1. Synchronous (HTTP/gRPC)
- **Client → Gateway → Service** - Request/response
- **BiddingService → AuctionService** - gRPC call to validate auction
- **SearchService → AuctionService** - HTTP call for initial sync

#### 2. Asynchronous (RabbitMQ)
- **AuctionService → RabbitMQ → SearchService** - Auction events
- **AuctionService → RabbitMQ → BiddingService** - Auction events
- **BiddingService → RabbitMQ → AuctionService** - Bid events

---

## Service-by-Service Breakdown

### 1. Gateway Service

**Role:** API Gateway - Single entry point

**Responsibilities:**
- Routes incoming requests to appropriate backend service
- Validates JWT tokens for protected endpoints
- Transforms request paths (e.g., `/auctions` → `/api/auctions`)

**Key Features:**
- Uses YARP (Yet Another Reverse Proxy)
- Centralized authentication
- No business logic (pure routing)

**Example Flow:**
```
Client: GET /auctions
  ↓
Gateway: Validates request (no auth needed for GET)
  ↓
Gateway: Routes to AuctionService at /api/auctions
  ↓
AuctionService: Returns auctions
  ↓
Gateway: Returns response to client
```

**Why It Exists:**
- Clients don't need to know about multiple services
- Centralized authentication
- Can add rate limiting, logging, etc. in one place

---

### 2. Identity Service

**Role:** Authentication and Authorization

**Responsibilities:**
- User registration and login
- Issues JWT tokens
- Validates tokens for other services

**Technology:**
- Duende IdentityServer (OAuth 2.0 / OpenID Connect)
- ASP.NET Core Identity (user management)
- PostgreSQL (user data)

**Key Concepts:**

#### OAuth 2.0 / OpenID Connect
- **OAuth 2.0** - Authorization framework (who can do what)
- **OpenID Connect** - Authentication layer (who you are)
- **JWT Tokens** - Secure way to pass user identity

#### Token Flow:
```
1. User logs in → IdentityService
2. IdentityService validates credentials
3. IdentityService issues JWT token
4. Client includes token in requests
5. Gateway validates token with IdentityService
6. Request proceeds if token is valid
```

**Seeded Users:**
- `alice` / `Pass123$`
- `bob` / `Pass123$`

**Why It Exists:**
- Centralized authentication (single source of truth)
- Other services don't need to handle user management
- Industry-standard protocols (OAuth 2.0/OIDC)

---

### 3. Auction Service

**Role:** Core business logic - Auction management

**Responsibilities:**
- Create, read, update, delete auctions
- Manage auction items (vehicles)
- Track auction status (Live, Finished, ReserveNotMet)
- Publish events when auctions change

**Technology:**
- ASP.NET Core Web API (REST)
- gRPC (for inter-service calls)
- PostgreSQL (relational database)
- Entity Framework Core (ORM)
- RabbitMQ (publish events)

**Key Features:**

#### REST API Endpoints:
- `GET /api/auctions` - List all auctions
- `GET /api/auctions/{id}` - Get auction by ID
- `POST /api/auctions` - Create auction (requires auth)
- `PUT /api/auctions/{id}` - Update auction (requires auth)
- `DELETE /api/auctions/{id}` - Delete auction (requires auth)

#### gRPC Service:
- `GetAuction(id)` - Used by BiddingService to validate auctions

#### Events Published:
- `AuctionCreated` - When new auction is created
- `AuctionUpdated` - When auction details change
- `AuctionDeleted` - When auction is deleted

#### Events Consumed:
- `AuctionFinished` - Updates auction status
- `BidPlaced` - Updates current high bid

**Data Model:**
```
Auction
  ├── Id (Guid)
  ├── ReservePrice (int)
  ├── Seller (string)
  ├── Winner (string)
  ├── SoldAmount (int)
  ├── CurrentHighBid (int)
  ├── Status (enum: Live, Finished, ReserveNotMet)
  ├── AuctionEnd (DateTime)
  └── Item (navigation property)
      ├── Make (string)
      ├── Model (string)
      ├── Year (int)
      ├── Color (string)
      ├── Mileage (int)
      └── ImageUrl (string)
```

**Why PostgreSQL?**
- Relational data (auctions have items)
- ACID transactions
- Complex queries with joins

**Why It Exists:**
- Single source of truth for auctions
- Manages auction lifecycle
- Other services depend on it for auction data

---

### 4. Bidding Service

**Role:** Handle bid placement and management

**Responsibilities:**
- Accept bids on auctions
- Validate bids (amount, auction status, etc.)
- Store bid history
- Determine bid status (Accepted, TooLow, etc.)
- Check for finished auctions and publish events

**Technology:**
- ASP.NET Core Web API
- MongoDB (document database)
- gRPC client (calls AuctionService)
- RabbitMQ (publish/consume events)

**Key Features:**

#### Endpoints:
- `POST /api/bids?auctionId={id}&amount={amount}` - Place bid (requires auth)
- `GET /api/bids/{auctionId}` - Get bids for auction

#### Bid Validation Logic:
1. Check if auction exists (gRPC call to AuctionService)
2. Prevent seller from bidding on own auction
3. Check if auction has ended
4. Compare with current high bid
5. Check against reserve price
6. Set bid status accordingly

#### Bid Statuses:
- `Accepted` - Bid is highest and above reserve
- `AcceptedBelowReserve` - Bid is highest but below reserve
- `TooLow` - Bid is lower than current high bid
- `Finished` - Auction has ended

#### Background Service:
- `CheckAuctionFinished` - Runs every 5 seconds
- Finds auctions that have ended
- Publishes `AuctionFinished` event

#### Events Published:
- `BidPlaced` - When a bid is placed
- `AuctionFinished` - When auction ends (from background service)

#### Events Consumed:
- `AuctionCreated` - Caches auction info locally

**Why MongoDB?**
- Document-based (bids are simple documents)
- Fast writes (many bids per auction)
- Easy to query bid history
- Scales horizontally

**Why It Exists:**
- Separates bidding logic from auction management
- Can scale independently (many bids during peak times)
- Owns bid data (no shared database)

---

### 5. Search Service

**Role:** Provide search functionality for auctions

**Responsibilities:**
- Maintain searchable index of auctions
- Provide search API with filters and sorting
- Keep index synchronized with auction data

**Technology:**
- ASP.NET Core Web API
- MongoDB (search index)
- RabbitMQ (consume events)
- Full-text search indexes

**Key Features:**

#### Endpoint:
- `GET /api/search` - Search auctions
  - Query params: `searchTerm`, `pageSize`, `pageNumber`, `seller`, `winner`, `orderBy`, `filterBy`

#### Search Capabilities:
- **Full-text search** - Search across Make, Model, Color
- **Filtering** - By status, seller, winner
- **Sorting** - By make, date, auction end
- **Pagination** - Page-based results

#### Data Synchronization:

**Initial Sync:**
- On startup, calls AuctionService HTTP API
- Retrieves all auctions or auctions updated since last sync
- Stores in MongoDB

**Real-time Updates (via RabbitMQ):**
- `AuctionCreated` → Add to index
- `AuctionUpdated` → Update in index
- `AuctionDeleted` → Remove from index
- `AuctionFinished` → Update status
- `BidPlaced` → Update current high bid

**Why MongoDB?**
- Full-text search indexes
- Fast read queries
- Document structure matches auction data
- Scales for large search volumes

**Why It Exists:**
- Separates search concerns from auction management
- Optimized for read-heavy workloads
- Can scale independently
- Eventual consistency (doesn't need real-time data)

---

## How Services Communicate

### Communication Matrix

| From Service | To Service | Method | Purpose |
|-------------|-----------|--------|---------|
| Client | Gateway | HTTP | All requests |
| Gateway | Identity | HTTP | Validate JWT |
| Gateway | Auction | HTTP | Auction operations |
| Gateway | Bidding | HTTP | Bid operations |
| Gateway | Search | HTTP | Search operations |
| Bidding | Auction | gRPC | Validate auction exists |
| Search | Auction | HTTP | Initial sync |
| Auction | RabbitMQ | Publish | Auction events |
| Bidding | RabbitMQ | Publish | Bid events |
| RabbitMQ | Search | Consume | Auction events |
| RabbitMQ | Bidding | Consume | Auction events |
| RabbitMQ | Auction | Consume | Bid/Finish events |

### Synchronous Communication Examples

#### Example 1: Place Bid (Synchronous Chain)

```
1. Client → Gateway: POST /bids?auctionId=123&amount=5000
   Headers: Authorization: Bearer <JWT>
   
2. Gateway → IdentityService: Validate JWT token
   Response: Token valid, user="alice"
   
3. Gateway → BiddingService: POST /api/bids?auctionId=123&amount=5000
   
4. BiddingService → AuctionService (gRPC): GetAuction(id=123)
   Response: Auction exists, ends in 2 days, reserve=4000
   
5. BiddingService: Validates bid
   - Check: User is not seller ✓
   - Check: Auction not ended ✓
   - Check: Amount > current high bid ✓
   - Check: Amount > reserve price ✓
   - Status: Accepted
   
6. BiddingService → MongoDB: Save bid
   
7. BiddingService → RabbitMQ: Publish BidPlaced event
   
8. BiddingService → Gateway: Return BidDto
   
9. Gateway → Client: Return response
```

#### Example 2: Search Auctions

```
1. Client → Gateway: GET /search?searchTerm=ford&pageSize=10
   
2. Gateway → SearchService: GET /api/search?searchTerm=ford&pageSize=10
   
3. SearchService → MongoDB: Query with full-text search
   - Search "ford" in Make, Model, Color fields
   - Filter: AuctionEnd > now (live auctions)
   - Sort: By auction end date
   - Paginate: Page 1, 10 items
   
4. SearchService → Gateway: Return results
   
5. Gateway → Client: Return results
```

### Asynchronous Communication Examples

#### Example 1: Create Auction (Event Flow)

```
1. User creates auction via Gateway → AuctionService
   
2. AuctionService:
   - Saves auction to PostgreSQL
   - Publishes AuctionCreated event to RabbitMQ
   - Returns response to user
   
3. RabbitMQ delivers event to consumers:
   
   a) SearchService receives AuctionCreated:
      - Adds auction to MongoDB search index
      
   b) BiddingService receives AuctionCreated:
      - Caches auction info in MongoDB
      - Stores: ID, Seller, AuctionEnd, ReservePrice
```

**Key Point:** AuctionService doesn't wait for SearchService or BiddingService. It publishes the event and continues.

#### Example 2: Auction Finishes (Event Flow)

```
1. BiddingService background job runs (every 5 seconds):
   - Finds auctions where AuctionEnd <= now and Finished = false
   - For each finished auction:
     a) Finds winning bid (highest accepted bid)
     b) Publishes AuctionFinished event
   
2. RabbitMQ delivers AuctionFinished to consumers:
   
   a) AuctionService receives AuctionFinished:
      - Updates auction status
      - Sets Winner and SoldAmount
      - Updates CurrentHighBid
      
   b) SearchService receives AuctionFinished:
      - Updates auction status in search index
      - Updates Winner and SoldAmount
```

---

## Data Flow Examples

### Complete Flow: User Creates Auction and Places Bid

```
┌─────────┐
│  User   │
└────┬────┘
     │
     │ 1. POST /auctions (with JWT)
     ▼
┌─────────────┐
│   Gateway   │
└──────┬──────┘
       │
       │ 2. Validate JWT
       ▼
┌─────────────┐
│  Identity   │ Returns: Token valid
└─────────────┘
       │
       │ 3. Forward request
       ▼
┌─────────────┐
│   Auction   │
│   Service   │
└──────┬──────┘
       │
       │ 4. Save to PostgreSQL
       ▼
┌─────────────┐
│ PostgreSQL  │ Auction saved
└─────────────┘
       │
       │ 5. Publish AuctionCreated event
       ▼
┌─────────────┐
│  RabbitMQ   │
└──────┬──────┘
       │
   ┌───┴───┐
   │       │
   ▼       ▼
┌──────┐ ┌──────┐
│Search│ │Bidding│
│Service│ │Service│
└──────┘ └──────┘
   │       │
   │       │ 6. Cache auction info
   │       ▼
   │   ┌──────┐
   │   │MongoDB│
   │   └──────┘
   │
   │ 7. Add to search index
   ▼
┌──────┐
│MongoDB│
└──────┘

Later...

┌─────────┐
│  User   │
└────┬────┘
     │
     │ 8. POST /bids?auctionId=123&amount=5000
     ▼
┌─────────────┐
│   Gateway   │
└──────┬──────┘
       │
       │ 9. Forward to BiddingService
       ▼
┌─────────────┐
│  Bidding   │
│  Service    │
└──────┬──────┘
       │
       │ 10. gRPC call: GetAuction(123)
       ▼
┌─────────────┐
│   Auction   │ Returns auction details
│   Service   │
└─────────────┘
       │
       │ 11. Validate bid
       │ 12. Save bid to MongoDB
       │ 13. Publish BidPlaced event
       ▼
┌─────────────┐
│  RabbitMQ   │
└──────┬──────┘
       │
   ┌───┴───┐
   │       │
   ▼       ▼
┌──────┐ ┌──────┐
│Auction│ │Search│
│Service│ │Service│
└──────┘ └──────┘
   │       │
   │ 14. Update CurrentHighBid
   │       │ 15. Update CurrentHighBid in index
   ▼       ▼
┌──────┐ ┌──────┐
│PostgreSQL│ │MongoDB│
└──────┘ └──────┘
```

---

## Key Patterns and Practices

### 1. API Gateway Pattern

**What:** Single entry point for all client requests

**Why:**
- Clients don't need to know about multiple services
- Centralized authentication
- Can add cross-cutting concerns (logging, rate limiting) in one place

**In Carsties:**
- GatewayService routes all requests
- Validates JWT tokens
- Transforms paths

### 2. Database per Service

**What:** Each service has its own database

**Why:**
- Services are independent
- Can use different database types
- No shared database = loose coupling

**In Carsties:**
- AuctionService → PostgreSQL
- BiddingService → MongoDB
- SearchService → MongoDB
- IdentityService → PostgreSQL

### 3. Event-Driven Architecture

**What:** Services communicate via events/messages

**Why:**
- Loose coupling (services don't directly depend on each other)
- Scalability (can add more consumers)
- Resilience (messages wait if service is down)

**In Carsties:**
- RabbitMQ as message broker
- Services publish events when something happens
- Other services consume events they care about

### 4. CQRS (Command Query Responsibility Segregation)

**What:** Separate read and write models

**In Carsties:**
- SearchService has read-optimized index (separate from AuctionService)
- Write happens in AuctionService
- Read happens in SearchService
- Synchronized via events

### 5. Outbox Pattern

**What:** Ensures reliable message delivery

**How:**
- Save to database and message queue in same transaction
- Background process reads from outbox and publishes messages
- If service crashes, messages aren't lost

**In Carsties:**
- AuctionService uses MassTransit outbox pattern
- Ensures events are published even if service crashes

### 6. Saga Pattern (Simplified)

**What:** Managing distributed transactions across services

**In Carsties:**
- When auction finishes, multiple services need to update:
  - AuctionService updates status
  - SearchService updates index
  - BiddingService marks auction as finished
- Coordinated via events (not transactions)

### 7. Service Discovery (Implicit)

**What:** Services find each other

**In Carsties:**
- Docker Compose provides DNS
- Services use service names as hostnames
- No explicit service registry needed

---

## Technology Choices Explained

### Why PostgreSQL for AuctionService?

- **Relational data** - Auctions have items (one-to-one relationship)
- **ACID transactions** - Need consistency when creating auctions
- **Complex queries** - Joins between auctions and items
- **Mature ecosystem** - Entity Framework Core support

### Why MongoDB for BiddingService?

- **Document-based** - Bids are simple documents
- **High write volume** - Many bids per auction
- **Simple queries** - Mostly "get bids for auction"
- **Horizontal scaling** - Can scale for high bid volumes

### Why MongoDB for SearchService?

- **Full-text search** - Built-in text indexes
- **Read-optimized** - Fast search queries
- **Document structure** - Matches auction data structure
- **Scalability** - Can handle large search volumes

### Why RabbitMQ?

- **Message broker** - Reliable message delivery
- **Multiple consumers** - One event can go to multiple services
- **Durability** - Messages survive service restarts
- **MassTransit** - .NET abstraction makes it easy to use

### Why gRPC?

- **Performance** - Faster than HTTP/REST
- **Type safety** - Strongly typed contracts
- **Streaming** - Can stream data
- **Inter-service calls** - Perfect for service-to-service communication

### Why YARP for Gateway?

- **Reverse proxy** - Routes requests transparently
- **Configuration-based** - Easy to configure routes
- **Performance** - High throughput
- **.NET native** - Built by Microsoft for .NET

### Why Duende IdentityServer?

- **Industry standard** - OAuth 2.0 / OpenID Connect
- **.NET integration** - Works seamlessly with ASP.NET Core
- **Flexibility** - Supports multiple grant types
- **Security** - Battle-tested authentication framework

---

## Learning Path

### For Beginners

1. **Start with GatewayService**
   - Simplest service (just routing)
   - Understand how requests flow

2. **Then IdentityService**
   - Learn about authentication
   - Understand JWT tokens

3. **Then AuctionService**
   - Core business logic
   - REST API and gRPC
   - Database operations

4. **Then BiddingService**
   - Event consumption
   - gRPC client usage
   - Background services

5. **Finally SearchService**
   - Event-driven architecture
   - Data synchronization
   - Full-text search

### Key Concepts to Master

1. **Microservices Fundamentals**
   - Service independence
   - Database per service
   - Service communication

2. **Communication Patterns**
   - Synchronous (HTTP/gRPC)
   - Asynchronous (Events/Messages)

3. **Architecture Patterns**
   - API Gateway
   - Event-Driven
   - CQRS
   - Outbox Pattern

4. **Technologies**
   - Docker & Docker Compose
   - Message Brokers (RabbitMQ)
   - Databases (PostgreSQL, MongoDB)
   - Authentication (OAuth 2.0/OIDC)

### Practice Exercises

1. **Add a new endpoint**
   - Add GET /api/auctions/seller/{username} to AuctionService
   - Route it through Gateway

2. **Add a new event**
   - Create BidAccepted event
   - Publish from BiddingService
   - Consume in AuctionService

3. **Add a new service**
   - Create NotificationService
   - Consume AuctionFinished events
   - Send notifications (simulate)

4. **Debug a flow**
   - Trace a request from client to database
   - Understand all the steps

---

## Summary

### What You've Learned

1. **Microservices Architecture**
   - Independent, deployable services
   - Each service owns its data
   - Services communicate via APIs and events

2. **Carsties Architecture**
   - 5 application services
   - 3 infrastructure services
   - Gateway as single entry point
   - Event-driven communication

3. **Key Patterns**
   - API Gateway
   - Database per Service
   - Event-Driven Architecture
   - CQRS
   - Outbox Pattern

4. **Communication**
   - Synchronous: HTTP/gRPC
   - Asynchronous: RabbitMQ events

5. **Technologies**
   - .NET 10.0
   - PostgreSQL & MongoDB
   - RabbitMQ
   - Docker Compose

### Next Steps

1. **Run the application**
   - `docker-compose up`
   - Explore the services
   - Make requests via Gateway

2. **Read the code**
   - Start with one service
   - Understand the flow
   - Trace a request end-to-end

3. **Experiment**
   - Add features
   - Modify existing code
   - Break things and fix them

4. **Learn more**
   - Microservices patterns
   - Distributed systems
   - Event-driven architecture

---

## Additional Resources

### Documentation Files

- `src/AuctionService/TECHNICAL_DOCUMENTATION.md` - Detailed AuctionService docs
- `src/BiddingService/TECHNICAL_DOCUMENTATION.md` - Detailed BiddingService docs
- `src/SearchService/TECHNICAL_DOCUMENTATION.md` - Detailed SearchService docs
- `src/IdentityService/TECHNICAL_DOCUMENTATION.md` - Detailed IdentityService docs
- `src/GatewayService/TECHNICAL_DOCUMENTATION.md` - Detailed GatewayService docs
- `DOCKER_COMPOSE_DOCUMENTATION.md` - Docker Compose explained

### Key Files to Explore

- `docker-compose.yml` - Service orchestration
- `src/Contracts/` - Message contracts (events)
- `src/*/Program.cs` - Service startup code
- `src/*/Controllers/` - API endpoints

---

**Remember:** Microservices is a journey. Start simple, understand the basics, and gradually explore more advanced concepts. The Carsties project is an excellent learning platform because it demonstrates real-world patterns in a manageable size.

