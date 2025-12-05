# Docker Compose Documentation

## Overview

This document explains the `docker-compose.yml` file in detail. Docker Compose is a tool that allows you to define and run multiple Docker containers as a single application. Instead of running each service separately, you can start all services with one command: `docker-compose up`.

---

## What is Docker Compose?

Docker Compose is like a recipe book for your application. It tells Docker:
- Which services (containers) to create
- How to configure each service
- How services connect to each other
- What ports to expose
- What data to persist

Think of it as a way to run multiple applications together, where each application runs in its own isolated container.

---

## File Structure

The `docker-compose.yml` file defines **7 services** (containers) that work together:

1. **postgres** - PostgreSQL database
2. **mongodb** - MongoDB database
3. **rabbitmq** - RabbitMQ message broker
4. **identity-svc** - Identity Service (authentication)
5. **auction-svc** - Auction Service
6. **search-svc** - Search Service
7. **bid-svc** - Bidding Service
8. **gateway-svc** - Gateway Service (API Gateway)

---

## Infrastructure Services

These are the supporting services that other services depend on.

### 1. PostgreSQL Database (`postgres`)

```yaml
postgres:
  image: postgres:15
  container_name: carsties_postgres
  environment:
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgresPwd
    POSTGRES_DB: Auctions
  ports:
    - "5432:5432"
  volumes:
    - pgdata:/var/lib/postgresql/data
  healthcheck:
    test: ["CMD", "pg_isready", "-U", "postgres"]
    interval: 5s
    timeout: 5s
    retries: 5
```

**What it does:**
- Runs a PostgreSQL database server (version 15)
- Stores relational data for AuctionService and IdentityService

**Configuration explained:**
- `image: postgres:15` - Uses the official PostgreSQL Docker image, version 15
- `container_name: carsties_postgres` - Gives the container a friendly name
- `environment:` - Sets environment variables inside the container:
  - `POSTGRES_USER: postgres` - Database admin username
  - `POSTGRES_PASSWORD: postgresPwd` - Database admin password
  - `POSTGRES_DB: Auctions` - Creates a database named "Auctions" on startup
- `ports: - "5432:5432"` - Maps port 5432 from container to host
  - Format: `"host_port:container_port"`
  - This allows you to connect to the database from your computer at `localhost:5432`
- `volumes: - pgdata:/var/lib/postgresql/data` - Persists database data
  - `pgdata` is a named volume (defined at bottom of file)
  - `/var/lib/postgresql/data` is where PostgreSQL stores data inside the container
  - **Why this matters:** Without this, all data would be lost when the container stops
- `healthcheck:` - Checks if the database is ready to accept connections
  - `test: ["CMD", "pg_isready", "-U", "postgres"]` - Runs a command to check database health
  - `interval: 5s` - Checks every 5 seconds
  - `timeout: 5s` - Waits 5 seconds for response
  - `retries: 5` - Tries 5 times before marking as unhealthy
  - **Why this matters:** Other services wait for this to be healthy before starting

---

### 2. MongoDB Database (`mongodb`)

```yaml
mongodb:
  image: mongo
  container_name: carsties_mongodb
  environment:
    - MONGO_INITDB_ROOT_USERNAME=root
    - MONGO_INITDB_ROOT_PASSWORD=mongoPwd
  ports:
    - 27017:27017
  volumes:
    - mongodata:/data/db
  healthcheck:
    test: ["CMD", "mongosh", "--eval", "db.adminCommand('ping')"]
    interval: 5s
    timeout: 5s
    retries: 5
```

**What it does:**
- Runs a MongoDB database server
- Stores document-based data for BiddingService and SearchService

**Configuration explained:**
- `image: mongo` - Uses the official MongoDB Docker image (latest version)
- `container_name: carsties_mongodb` - Friendly name for the container
- `environment:` - Sets MongoDB credentials:
  - `MONGO_INITDB_ROOT_USERNAME=root` - Root username
  - `MONGO_INITDB_ROOT_PASSWORD=mongoPwd` - Root password
  - These are set when MongoDB first initializes
- `ports: - 27017:27017` - Exposes MongoDB's default port
  - Connect from your computer at `localhost:27017`
- `volumes: - mongodata:/data/db` - Persists MongoDB data
  - `mongodata` is a named volume
  - `/data/db` is MongoDB's default data directory
- `healthcheck:` - Checks if MongoDB is responding
  - Uses `mongosh` (MongoDB shell) to ping the database

---

### 3. RabbitMQ Message Broker (`rabbitmq`)

```yaml
rabbitmq:
  image: rabbitmq:4-management-alpine
  container_name: carsties_rabbitmq
  environment:
    RABBITMQ_DEFAULT_USER: user
    RABBITMQ_DEFAULT_PASS: rabbitPwd
  ports:
    - 5672:5672
    - 15672:15672
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "check_port_connectivity"]
    interval: 5s
    timeout: 5s
    retries: 5
```

**What it does:**
- Runs RabbitMQ message broker
- Enables services to communicate asynchronously via messages/events

**Configuration explained:**
- `image: rabbitmq:4-management-alpine` - Uses RabbitMQ version 4 with management UI
  - `alpine` means a smaller, lightweight Linux distribution
- `container_name: carsties_rabbitmq` - Friendly name
- `environment:` - Sets RabbitMQ credentials:
  - `RABBITMQ_DEFAULT_USER: user` - Default username
  - `RABBITMQ_DEFAULT_PASS: rabbitPwd` - Default password
- `ports:` - Exposes two ports:
  - `5672:5672` - AMQP protocol port (for sending/receiving messages)
  - `15672:15672` - Management UI port (web interface)
    - Access management UI at `http://localhost:15672`
    - Login with `user` / `rabbitPwd`
- `healthcheck:` - Verifies RabbitMQ is accepting connections

---

## Application Services

These are the microservices that make up the Carsties application.

### 4. Identity Service (`identity-svc`)

```yaml
identity-svc:
  image: identity-svc:latest
  build:
    context: .
    dockerfile: src/IdentityService/Dockerfile
  environment:
    - ASPNETCORE_ENVIRONMENT=Docker
    - ASPNETCORE_URLS=http://+:80
    - ConnectionStrings__DefaultConnection=Server=postgres:5432;Database=Identity;User Id=postgres;Password=postgresPwd
  ports:
    - 5000:80
  depends_on:
    postgres:
      condition: service_healthy
```

**What it does:**
- Provides authentication and authorization (OAuth 2.0 / OpenID Connect)
- Issues JWT tokens for authenticated users
- Manages user accounts

**Configuration explained:**
- `image: identity-svc:latest` - Name for the built image
- `build:` - Tells Docker how to build this service:
  - `context: .` - Build context is the current directory (project root)
  - `dockerfile: src/IdentityService/Dockerfile` - Path to Dockerfile
  - **What happens:** Docker builds the image using the specified Dockerfile
- `environment:` - Sets .NET environment variables:
  - `ASPNETCORE_ENVIRONMENT=Docker` - Sets environment to "Docker"
  - `ASPNETCORE_URLS=http://+:80` - Listens on port 80 inside container
  - `ConnectionStrings__DefaultConnection=...` - Database connection string
    - `Server=postgres:5432` - Uses service name "postgres" (Docker's internal DNS)
    - `Database=Identity` - Connects to Identity database
    - **Note:** `postgres` is the service name, not `localhost` (containers can talk to each other by service name)
- `ports: - 5000:80` - Maps container port 80 to host port 5000
  - Access service at `http://localhost:5000`
- `depends_on:` - Defines startup order:
  - `postgres: condition: service_healthy` - Waits for PostgreSQL to be healthy
  - **Why this matters:** Service won't start until database is ready

---

### 5. Auction Service (`auction-svc`)

```yaml
auction-svc:
  image: auction-svc:latest
  build:
    context: .
    dockerfile: src/AuctionService/Dockerfile
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ASPNETCORE_URLS=http://+:80
    - ASPNETCORE_URLS=http://+:7777
    - ConnectionStrings__DefaultConnection=Server=postgres:5432;Database=Auctions;User Id=postgres;Password=postgresPwd
    - RabbitMQ__Host=rabbitmq
    - RabbitMQ__UserName=user
    - RabbitMQ__Password=rabbitPwd
    - IdentityServiceUrl=http://identity-svc
    - Kestrel__Endpoints__Grpc__Protocols=Http2
    - Kestrel__Endpoints__Grpc__Url=http://+:7777
    - Kestrel__Endpoints__WebApi__Protocols=Http1
    - Kestrel__Endpoints__WebApi__Url=http://+80
  ports:
    - 7001:80
    - 7777:7777
  depends_on:
    postgres:
      condition: service_healthy
    rabbitmq:
      condition: service_healthy
```

**What it does:**
- Manages vehicle auctions (create, read, update, delete)
- Provides REST API and gRPC endpoints
- Publishes events when auctions change

**Configuration explained:**
- `build:` - Builds from `src/AuctionService/Dockerfile`
- `environment:` - Multiple configuration settings:
  - `ASPNETCORE_ENVIRONMENT=Development` - Development mode
  - `ASPNETCORE_URLS=http://+:80` and `http://+:7777` - Two endpoints:
    - Port 80: REST API
    - Port 7777: gRPC service
  - `ConnectionStrings__DefaultConnection=...` - PostgreSQL connection
    - `Server=postgres:5432` - Uses service name "postgres"
    - `Database=Auctions` - Connects to Auctions database
  - `RabbitMQ__Host=rabbitmq` - RabbitMQ service name (not localhost!)
  - `RabbitMQ__UserName=user` and `RabbitMQ__Password=rabbitPwd` - RabbitMQ credentials
  - `IdentityServiceUrl=http://identity-svc` - Identity service URL (service name)
  - `Kestrel__Endpoints__...` - Configures Kestrel web server:
    - gRPC endpoint on port 7777 using HTTP/2
    - Web API endpoint on port 80 using HTTP/1
- `ports:` - Exposes two ports:
  - `7001:80` - REST API accessible at `http://localhost:7001`
  - `7777:7777` - gRPC accessible at `http://localhost:7777`
- `depends_on:` - Waits for both PostgreSQL and RabbitMQ to be healthy

---

### 6. Search Service (`search-svc`)

```yaml
search-svc:
  image: search-svc:latest
  build:
    context: .
    dockerfile: src/SearchService/Dockerfile
  environment:
    - ASPNETCORE_ENVIRONMENT=Docker
    - ASPNETCORE_URLS=http://+:80
    - ConnectionStrings__MongoDbConnection=mongodb://root:mongoPwd@mongodb
    - RabbitMQ__Host=rabbitmq
    - RabbitMQ__UserName=user
    - RabbitMQ__Password=rabbitPwd
    - AuctionServiceUrl=http://auction-svc
  ports:
    - 7002:80
  depends_on:
    mongodb:
      condition: service_healthy
    rabbitmq:
      condition: service_healthy
```

**What it does:**
- Provides search functionality for auctions
- Maintains a searchable index in MongoDB
- Listens to auction events to keep index updated

**Configuration explained:**
- `build:` - Builds from `src/SearchService/Dockerfile`
- `environment:` - Configuration:
  - `ASPNETCORE_ENVIRONMENT=Docker` - Docker environment
  - `ASPNETCORE_URLS=http://+:80` - Listens on port 80
  - `ConnectionStrings__MongoDbConnection=mongodb://root:mongoPwd@mongodb`
    - MongoDB connection string
    - `mongodb://` - Protocol
    - `root:mongoPwd` - Username:password
    - `@mongodb` - Hostname (service name)
  - `RabbitMQ__Host=rabbitmq` - RabbitMQ service name
  - `AuctionServiceUrl=http://auction-svc` - Auction service URL (for initial sync)
- `ports: - 7002:80` - Accessible at `http://localhost:7002`
- `depends_on:` - Waits for MongoDB and RabbitMQ

---

### 7. Bidding Service (`bid-svc`)

```yaml
bid-svc:
  image: bid-svc:latest
  build: 
    context: .
    dockerfile: src/BiddingService/Dockerfile
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ASPNETCORE_URLS=http://+:80
    - RabbitMQ__Host=rabbitmq
    - RabbitMQ__UserName=user
    - RabbitMQ__Password=rabbitPwd
    - ConnectionStrings__BidDbConnection=mongodb://root:mongoPwd@mongodb
    - IdentityServiceUrl=http://identity-svc
    - GrpcAuction=http://auction-svc:7777
  ports:
    - 7003:80
  depends_on:
    rabbitmq:
      condition: service_started
    mongodb:
      condition: service_healthy
```

**What it does:**
- Handles bid placement on auctions
- Stores bids in MongoDB
- Communicates with AuctionService via gRPC

**Configuration explained:**
- `build:` - Builds from `src/BiddingService/Dockerfile`
- `environment:` - Configuration:
  - `ASPNETCORE_ENVIRONMENT=Development` - Development mode
  - `ASPNETCORE_URLS=http://+:80` - Port 80
  - `RabbitMQ__Host=rabbitmq` - RabbitMQ service
  - `ConnectionStrings__BidDbConnection=mongodb://root:mongoPwd@mongodb` - MongoDB connection
  - `IdentityServiceUrl=http://identity-svc` - Identity service for JWT validation
  - `GrpcAuction=http://auction-svc:7777` - AuctionService gRPC endpoint
- `ports: - 7003:80` - Accessible at `http://localhost:7003`
- `depends_on:` - Note the difference:
  - `rabbitmq: condition: service_started` - Only waits for service to start (not healthy)
  - `mongodb: condition: service_healthy` - Waits for MongoDB to be healthy

---

### 8. Gateway Service (`gateway-svc`)

```yaml
gateway-svc:
  image: gateway-svc:latest
  build:
    context: .
    dockerfile: src/GatewayService/Dockerfile
  environment:
    - ASPNETCORE_ENVIRONMENT=Docker
    - ASPNETCORE_URLS=http://+:80
  ports:
    - 6001:80
```

**What it does:**
- Acts as API Gateway (single entry point)
- Routes requests to appropriate backend services
- Handles authentication

**Configuration explained:**
- `build:` - Builds from `src/GatewayService/Dockerfile`
- `environment:` - Minimal configuration:
  - `ASPNETCORE_ENVIRONMENT=Docker` - Docker environment
  - `ASPNETCORE_URLS=http://+:80` - Port 80
- `ports: - 6001:80` - Accessible at `http://localhost:6001`
- **Note:** No `depends_on` - Gateway can start independently (it connects to services at runtime)

---

## Volumes

```yaml
volumes:
  pgdata:
  mongodata:
```

**What it does:**
- Defines named volumes for data persistence

**Explanation:**
- `pgdata` - Volume for PostgreSQL data
- `mongodata` - Volume for MongoDB data
- **Why volumes matter:**
  - Data persists even if containers are stopped/removed
  - Data is stored on your host machine (managed by Docker)
  - Can be backed up, shared, or migrated
  - Without volumes, data would be lost when containers are removed

**Where is the data stored?**
- On Linux/Mac: `/var/lib/docker/volumes/`
- On Windows: `\\wsl$\docker-desktop-data\data\docker\volumes\`
- You can inspect volumes with: `docker volume ls` and `docker volume inspect <volume_name>`

---

## Key Concepts Explained

### Service Names as Hostnames

In Docker Compose, services can communicate using their service names as hostnames:

- `postgres` → `http://postgres:5432` (not `localhost`)
- `mongodb` → `mongodb://mongodb:27017`
- `rabbitmq` → `rabbitmq:5672`
- `identity-svc` → `http://identity-svc:80`

**Why?** Each container has its own network namespace. Docker Compose creates a virtual network where containers can resolve each other by service name.

### Port Mapping Format

`"host_port:container_port"`

- **Host port** - Port on your computer (what you use to access)
- **Container port** - Port inside the container (what the application listens on)

Example: `7001:80` means:
- Application listens on port 80 inside container
- You access it via port 7001 on your computer
- URL: `http://localhost:7001`

### Environment Variables

Environment variables configure applications at runtime. In .NET, nested configuration uses double underscores:

- `ConnectionStrings__DefaultConnection` → `ConnectionStrings.DefaultConnection` in appsettings.json
- `RabbitMQ__Host` → `RabbitMQ.Host` in appsettings.json

### Health Checks

Health checks ensure services are ready before dependent services start:

- `condition: service_healthy` - Waits for health check to pass
- `condition: service_started` - Only waits for container to start (faster, but less reliable)

### Dependencies (`depends_on`)

Defines startup order:

```yaml
depends_on:
  postgres:
    condition: service_healthy
```

This means: "Don't start this service until PostgreSQL is healthy"

---

## How to Use Docker Compose

### Starting All Services

```bash
docker-compose up
```

**What happens:**
1. Creates a virtual network for all services
2. Creates volumes if they don't exist
3. Builds images for services with `build:` directive
4. Starts services in dependency order
5. Waits for health checks to pass

### Starting in Background (Detached Mode)

```bash
docker-compose up -d
```

The `-d` flag runs containers in the background.

### Stopping Services

```bash
docker-compose down
```

**What happens:**
- Stops all containers
- Removes containers (but keeps volumes and images)

### Stopping and Removing Volumes

```bash
docker-compose down -v
```

**Warning:** This removes volumes, deleting all database data!

### Viewing Logs

```bash
# All services
docker-compose logs

# Specific service
docker-compose logs identity-svc

# Follow logs (like tail -f)
docker-compose logs -f auction-svc
```

### Rebuilding Services

```bash
# Rebuild all services
docker-compose build

# Rebuild specific service
docker-compose build auction-svc

# Rebuild and restart
docker-compose up --build
```

---

## Service Communication Flow

Here's how services communicate:

```
Client Request
    ↓
Gateway Service (port 6001)
    ↓
    ├─→ Identity Service (port 5000) - for authentication
    ├─→ Auction Service (port 7001) - for auction operations
    ├─→ Search Service (port 7002) - for search
    └─→ Bidding Service (port 7003) - for bids

Backend Services
    ├─→ PostgreSQL (port 5432) - for relational data
    ├─→ MongoDB (port 27017) - for document data
    └─→ RabbitMQ (port 5672) - for messaging

Services publish/consume events via RabbitMQ:
    AuctionService → RabbitMQ → SearchService
    AuctionService → RabbitMQ → BiddingService
    BiddingService → RabbitMQ → AuctionService
```

---

## Common Issues and Solutions

### Port Already in Use

**Error:** `Bind for 0.0.0.0:5432 failed: port is already allocated`

**Solution:** 
- Stop the service using that port
- Or change the port mapping: `"5433:5432"` (use 5433 instead of 5432)

### Service Won't Start

**Check logs:**
```bash
docker-compose logs <service-name>
```

**Common causes:**
- Database not ready (check health checks)
- Connection string incorrect
- Missing environment variables

### Can't Connect to Database

**Problem:** Service can't reach PostgreSQL/MongoDB

**Check:**
- Service name in connection string (use `postgres`, not `localhost`)
- Health checks are passing: `docker-compose ps`
- Services are on same network: `docker network ls`

### Data Lost After Restart

**Problem:** Data disappears when containers restart

**Solution:** Ensure volumes are defined and mounted correctly

---

## Summary

The `docker-compose.yml` file orchestrates 8 services:

1. **3 Infrastructure Services:**
   - PostgreSQL (relational database)
   - MongoDB (document database)
   - RabbitMQ (message broker)

2. **5 Application Services:**
   - Identity Service (authentication)
   - Auction Service (auction management)
   - Search Service (search functionality)
   - Bidding Service (bid management)
   - Gateway Service (API gateway)

**Key Points:**
- Services communicate using service names (not localhost)
- Ports are mapped from container to host
- Volumes persist data across container restarts
- Health checks ensure services start in correct order
- Environment variables configure each service

This setup allows you to run the entire Carsties microservices application with a single command: `docker-compose up`

