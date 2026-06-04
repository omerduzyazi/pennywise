# PennyWise — Personal Finance & Budget Tracking System

**Academic Documentation for Architectural & Design Decisions**
*Generated: 2026-06-04*

---

## 1. Architectural Design Decisions

### 1.1 Layered Architecture & Separation of Concerns
The PennyWise system adopts a layered (N-Tier) architecture decomposed into four discrete assemblies: `PennyWise.API`, `PennyWise.Domain`, `PennyWise.Infrastructure`, and `PennyWise.Tests`. This decomposition is grounded in the SOLID principles, particularly the Single Responsibility Principle (SRP) and the Dependency Inversion Principle (DIP). The Domain layer encapsulates all business entities and enumerations with zero external dependencies, ensuring that the core business logic remains agnostic of persistence concerns, web framework conventions, or third-party library implementations. The Infrastructure layer assumes responsibility for data access via Entity Framework Core 8.0, configured with the Npgsql provider for PostgreSQL connectivity. The API layer functions as the composition root, orchestrating dependency injection and HTTP pipeline configuration. This stratification mirrors the Clean Architecture paradigm, wherein the dependency flow is strictly unidirectional — outer layers depend on inner layers, never the reverse.

### 1.2 Technology Stack Justification
The selection of .NET 8.0 as the backend runtime is predicated upon its long-term support (LTS) classification, superior performance benchmarks, and native support for minimal APIs and dependency injection. PostgreSQL 16 was chosen over SQL Server due to its open-source licensing model, JSONB support for semi-structured data, and demonstrated scalability in production environments. The frontend employs Vanilla HTML, CSS, and JavaScript to eliminate framework-specific build tooling overhead, reducing deployment complexity while maintaining full control over the DOM and rendering lifecycle. Nginx 1.25 serves as the reverse proxy and static file server, chosen for its event-driven architecture and negligible memory footprint.

---

## 2. Containerization Strategy

### 2.1 Docker-First Development Paradigm
The project enforces a Docker-first development philosophy wherein all services — database, API, and frontend — are encapsulated within Docker containers and orchestrated via Docker Compose v3.9. This approach guarantees environment parity across development, testing, and production stages. The multi-stage Dockerfile for the API leverages the SDK image for compilation and the ASP.NET runtime image for execution, resulting in a production image size reduction of approximately 70% compared to single-stage builds.

### 2.2 Service Orchestration & Networking
The `docker-compose.yml` defines three services with explicit dependency chains. The PostgreSQL container exposes a health check, and the API container's startup is conditioned upon the database achieving a healthy state (`condition: service_healthy`). All containers communicate over a user-defined bridge network (`pennywise-network`), providing DNS-based service discovery. The Nginx container reverse-proxies API requests from the frontend (`/api/*`) to the backend container, enabling the frontend to interact with the API without cross-origin restrictions while maintaining a single-origin deployment model.

---

## 3. CI/CD Pipeline Architecture

### 3.1 Continuous Integration with GitHub Actions
The CI/CD pipeline is implemented via GitHub Actions and consists of two sequential jobs: `build-and-test` and `docker-build`. The first job restores NuGet dependencies, compiles the solution in Release configuration, and executes the xUnit test suite with code coverage collection. The second job, gated behind the successful completion of the first, validates the Docker images by building them and verifying the docker-compose configuration. This gating mechanism ensures that no Docker artifacts are produced from a codebase that fails its test suite, adhering to the fail-fast principle.

### 3.2 Testing Strategy
The test project employs xUnit as the test framework, selected for its extensibility model and parallel test execution capabilities. Integration tests utilize `WebApplicationFactory` to spin up an in-process test server, replacing the PostgreSQL DbContext with an EF Core InMemory provider. This substitution enables test execution in CI environments without requiring a live database instance. FluentAssertions provides a fluent, readable assertion syntax that improves test maintainability.

---

## 4. Authentication & Security

### 4.1 Authentication Strategy: JWT vs Session-Based
The PennyWise system employs JSON Web Token (JWT) based authentication as opposed to traditional server-side session management. First, JWT tokens are self-contained, aligning with the stateless constraint of RESTful architectural design, enabling horizontal scalability. Second, in the context of a containerized deployment, stateless authentication avoids the "sticky session" anti-pattern. Third, JWT tokens are natively supported by the `Microsoft.AspNetCore.Authentication.JwtBearer` middleware. The token is signed using HMAC-SHA256 with a 512-bit symmetric key.

### 4.2 Password Hashing: BCrypt Strategy
User passwords are hashed using the BCrypt adaptive hashing algorithm, implemented via the `BCrypt.Net-Next` NuGet package. BCrypt was selected for its inherent resistance to brute-force attacks through configurable work factors. The adaptive cost factor ensures that the computational cost of password verification scales with hardware advancements. This approach adheres to OWASP's Password Storage Cheat Sheet recommendations.

---

## 5. Data Access & Domain Model

### 5.1 Repository Pattern Justification
The generic `IRepository<T>` interface, defined in the Domain layer, abstracts all data access operations. The concrete implementation, `Repository<T>`, resides in the Infrastructure layer and leverages Entity Framework Core's `DbSet<T>`. This indirection serves multiple purposes:
- **Testability:** Controllers can be tested using mock implementations.
- **Separation of Concerns:** The Domain layer remains entirely free of infrastructure dependencies.
- **Open/Closed Principle (OCP):** New entity types automatically receive full CRUD capabilities through the generic registration without modifying existing code.

### 5.2 Entity Hierarchy
All domain entities inherit from `BaseEntity`, which provides a Guid-based primary key (`Id`) and audit timestamps (`CreatedAt`, `UpdatedAt`). The use of GUIDs as primary keys, rather than auto-incrementing integers, is motivated by their suitability for distributed systems and their ability to be generated client-side without database round-trips.

### 5.3 Database Migration Strategy
The initial database migration establishes the complete relational schema, including all five domain tables (Users, Transactions, Budgets, Portfolios, Holdings), their associated foreign key constraints, and performance-critical indexes (notably a unique index on `Users.Email` for O(1) lookup during authentication). The migration is executed automatically during application startup via `PennyWiseDbContext.Database.Migrate()`.

---

## 6. API Design & Data Ownership

### 6.1 CRUD Design Patterns & RESTful Conventions
The implementation of Transactions, Budgets, and Portfolios adheres strictly to RESTful architectural conventions. Each controller targets a specific resource collection and utilizes standard HTTP methods (GET, POST, PUT, DELETE). Data Transfer Objects (DTOs), implemented using C# 9.0 record types, establish a clear contract between the client and API, ensuring that internal domain entities are never exposed directly. This decoupling prevents over-posting vulnerabilities.

### 6.2 Data Ownership & Multi-Tenant Query Scoping
Since PennyWise serves multiple users simultaneously, data isolation is a critical security requirement. Every API request that interacts with the database enforces "tenant scoping" at the repository level. The current user's ID is extracted directly from the validated JWT claims and prepended as a mandatory predicate to every database query (e.g., `_repo.FindAsync(t => t.UserId == userId)`). This design guarantees that an authenticated user can only retrieve, mutate, or delete records that belong to them, preventing Horizontal Privilege Escalation.

### 6.3 Pagination and Filtering Strategy
To ensure scalable performance as the transaction volume grows, the `GET /api/transactions` endpoint implements server-side pagination and filtering. Instead of returning the entire dataset, the API accepts `page` and `pageSize` query parameters, utilizing LINQ's `Skip()` and `Take()` operators to translate the request into efficient SQL `OFFSET` and `FETCH NEXT` clauses. Dynamic filtering capabilities are applied directly to the `IQueryable` interface, minimizing database I/O.

### 6.4 Budget Tracking Logic & Aggregation
The `GET /api/budgets/status` endpoint exemplifies cross-entity data aggregation. Rather than requiring the frontend to download all budgets and all transactions to compute the remaining balance, the API performs this aggregation server-side. For a given month and year, the system fetches all budgets and all expense transactions belonging to the user. It then groups the expenses by category and calculates the sum, returning pre-computed, display-ready metrics to the client.

---

## 7. Advanced Analytics: Portfolio Performance

### 7.1 Service Layer Pattern for Analytics
The project utilizes the Service Layer pattern via the `IPortfolioAnalyticsService`. Unlike standard CRUD operations which are handled adequately by the Repository pattern inside the controllers, complex financial calculations (such as TWR) represent pure domain business logic. By extracting this logic into a dedicated service (`PortfolioAnalyticsService`) and registering it via Dependency Injection, we adhere to the Single Responsibility Principle (SRP). The controller is solely responsible for HTTP request/response handling and authorization, delegating the mathematical computations to the service layer.

### 7.2 Time-Weighted Return (TWR) Calculation Model
The objective of the Time-Weighted Return (TWR) metric is to evaluate the compound rate of growth of a portfolio while eliminating the distorting effects of cash inflows and outflows. 

For the initial phase of PennyWise, a simplified approximation model is implemented. Since the system tracks individual holdings with an average `PurchasePrice` and a `CurrentPrice` (rather than timestamped ledger entries for every cash flow), the TWR calculation is simplified to:
`TWR = (Sum(CurrentValue) / Sum(CostBasis)) - 1`

This represents the absolute percentage return over the holding period. As the application matures in future steps to include cash ledger accounting and daily mark-to-market valuations, the `IPortfolioAnalyticsService` can be swapped out via Dependency Injection with a more sophisticated implementation (e.g., modified Dietz method) without requiring any changes to the API controllers or frontend.
