# Modular Monolith E-Commerce Architecture

An e-commerce platform built on a **Modular Monolith** architecture using **.NET 9**. The project demonstrates how to achieve high cohesion and low coupling by isolating features into independent modules while maintaining the simplicity of a single deployment unit. Moduler communication is handled via an **In-Memory Event Bus** and the **Outbox Pattern**, ensuring strict data consistency without requiring external message brokers (like RabbitMQ or Kafka) initially.

---

## 🏗️ Architecture & Key Concepts

### 1. Modular Monolith
The application is deployed as a single ASP.NET Core process, but internally it's divided into strictly isolated modules (`Payment`, `Loyalty`, `Notification`). 
* **Database per Module:** Each module has its own `DbContext` and PostgreSQL schema (e.g., `payment.Payments`, `loyalty.UserPoints`).
* **No Direct References:** Modules do not reference each other directly. They only communicate through shared events.

### 2. In-Memory Event Bus
Instead of a heavy message broker, this project utilizes `System.Threading.Channels` to build a high-performance, process-inbound event bus.
* Built-in backpressure handling (Bounded Channels).
* Parallel processing for multiple event handlers using `Task.WhenAll`.
* Easy transition to RabbitMQ or Kafka in the future by simply swapping the `IEventBus` implementation.

### 3. Outbox Pattern
To solve the dual-write problem (saving to the database and publishing an event simultaneously), the **Outbox Pattern** is implemented.
1. The domain event is saved as an `OutboxMessage` in the same transaction as the business entity changes.
2. An elegant background service (`OutboxProcessorService`) periodically polls the database and publishes the pending messages to the In-Memory Event Bus.
3. This guarantees *At-Least-Once* delivery for domain events.

---

## 📦 Project Structure & Modules

```
src/
├── ModularMonolith/                # Main Host Application (API & Composition Root)
│   ├── Program.cs                  # Registers modules & Swagger
│
├── Modules/                        # Isolated Domain Modules
│   ├── Payment/                    # Handles payments (schema: payment) -> Publishes: PaymentSucceededEvent
│   ├── Loyalty/                    # Manages points (schema: loyalty) -> Listens: Payment; Publishes: PointsAdded
│   └── Notification/               # Sends emails -> Listens: Payment, Loyalty
│
└── Shared/                         # Shared Packages & Contracts
    ├── SharedBus/                  # Outbox entity and event interfaces
    ├── SharedInfrastructure/       # EventBus background services & Outbox processors
    └── SharedKernel/               # Common abstractions
```

### Module Responsibilities

| Module | Description | Listens To | Publishes | Database Schema |
|--------|-------------|------------|-----------|-----------------|
| **Payment** | Handles checkout and payment processes using MediatR & FluentValidation. | - | `PaymentSucceededEvent` | `payment` |
| **Loyalty** | Awards points to users based on successful payments (1 pt per 10 TL). | `PaymentSucceededEvent`| `PointsAddedEvent` | `loyalty` |
| **Notification** | Sends transactional emails (MailKit) upon successful payments and points earned. | `PaymentSucceededEvent`, `PointsAddedEvent` | `EmailSentEvent` | *None* |

---

## 🛠️ Technology Stack

* **Framework:** .NET 9 (ASP.NET Core Minimal APIs)
* **Database:** PostgreSQL 16 (Entity Framework Core 9)
* **Architecture Patterns:** Modular Monolith, CQRS, Outbox Pattern, Event-Driven
* **Libraries:** MediatR, FluentValidation, MailKit
* **Observability:** OpenTelemetry & Jaeger
* **Infrastructure:** Docker & Docker Compose

---

## 🚀 Getting Started

### Prerequisites
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Start Infrastructure (PostgreSQL, pgAdmin, Jaeger)
Run the provided `docker-compose.yml` to spin up the database and tracing tools:
```bash
docker-compose up -d
```
* **PostgreSQL:** `localhost:5492`
* **pgAdmin:** `http://localhost:8090` (admin@admin.com / admin)
* **Jaeger UI:** `http://localhost:16686`

### 2. Run the Application
You can run the application straight from Visual Studio or using the CLI. The application automatically applies all Entity Framework migrations on startup (`app.MigrateAllDatabasesAsync()`).

```bash
cd src/ModularMonolith
dotnet run
```

### 3. Explore the API 
Once running, open your browser and navigate to the Swagger UI:
* **Swagger:** `https://localhost:5001/swagger` (or matching port in `launchSettings.json`)

---

## 📝 How It Works (A Payment Flow Example)

1. A client sends a POST request to `/api/payment/process`.
2. The `Payment` module validates the request and saves a payment record to the `payment.Payments` table.
3. In the **same database transaction**, it writes a `PaymentSucceededEvent` to the `payment.OutboxMessages` table.
4. The `OutboxProcessorService` (polling in the background) picks up this message and puts it in the `InMemoryEventBus` channel.
5. The `EventBusBackgroundService` reads from the channel and triggers BOTH the `Loyalty` module and `Notification` module handlers in parallel.
6. `Loyalty` calculates and adds user points, saving it to `loyalty.UserPoints`, and generating a `PointsAddedEvent` via the Outbox... and the cycle continues natively!

---

## 📧 Email Configuration (Notification Module)
The Notification module expects SMTP configuration to successfully send out emails via MailKit. Ensure you configure valid credentials in `appsettings.json` under the `EmailSettings` node if you wish to see actual emails being dispatched.
