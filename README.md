# Modular Monolith E-Commerce Architecture

An e-commerce platform built on a **Modular Monolith** architecture using **.NET 9**. The project demonstrates how to achieve high cohesion and low coupling by isolating features into independent modules while maintaining the simplicity of a single deployment unit. Modular communication is handled via an **In-Memory Event Bus**, the **Outbox Pattern**, and the **Inbox Pattern**, ensuring strict data consistency and at-least-once delivery without requiring external message brokers.

---

## 🏗️ Architecture & Key Concepts

### 1. Modular Monolith
The application is deployed as a single ASP.NET Core process, but internally divided into strictly isolated modules (`Payment`, `Loyalty`, `Notification`).
* **Database per Module:** Each module has its own `DbContext` and PostgreSQL schema (e.g., `payment.Payments`, `loyalty.UserPoints`).
* **No Direct References:** Modules do not reference each other directly. They only communicate through shared events.

### 2. In-Memory Event Bus
Instead of a heavy message broker, this project utilizes `System.Threading.Channels` to build a high-performance, process-bound event bus.
* Built-in backpressure handling (Bounded Channels).
* Parallel processing for multiple event handlers using `Task.WhenAll`.
* Easy transition to RabbitMQ or Kafka by swapping the `IEventBus` implementation.

### 3. Outbox Pattern
To solve the dual-write problem on the **publisher side**, the **Outbox Pattern** is implemented.
1. The domain event is saved as an `OutboxMessage` in the **same transaction** as the business entity changes.
2. A background service (`OutboxProcessorService`) periodically polls the database and publishes pending messages to the In-Memory Event Bus.
3. Guarantees *At-Least-Once* delivery for domain events.

### 4. Inbox Pattern
To solve the dual-write problem on the **consumer side** (receiving an event and persisting its effects atomically), the **Inbox Pattern** is implemented.

#### How It Works
1. When the In-Memory Event Bus delivers an event to a subscriber module, `InboxSaverWrapper` intercepts it and persists the raw event as an `InboxMessage` in the **consuming module's own database schema**.
2. `InboxProcessorService` (a background service, one per module `DbContext`) polls pending `InboxMessage` rows and dispatches them to the real `IEventHandler` implementation.
3. Idempotency is guaranteed via a **UNIQUE constraint** on `InboxMessage.Id` (= `Event.Id`). Duplicate events are silently skipped.

#### Retry & Dead Letter
* Failed handlers are **retried up to 3 times** (configurable via `MaxRetries`).
* Messages that exceed `MaxRetries` are flagged as **dead letters** and logged at `CRITICAL` level for manual intervention.

#### OpenTelemetry Tracing
Each inbox dispatch creates a child span (`inbox.dispatch {EventType}`) linked to the original W3C `traceparent` stored in `InboxMessage.TraceContext`, preserving the full distributed trace across module boundaries.

#### Key Classes

| Class | Role |
|---|---|
| `InboxMessage` | Entity — persisted in the consuming module's schema. Holds payload, retry count, error, and W3C trace context. |
| `InboxSaverWrapper<TEvent, TDbContext>` | `IEventHandlerWrapper` — intercepts the event bus and writes to the inbox table. Handles concurrent deduplication. |
| `InboxProcessorService<TDbContext>` | `BackgroundService` — polls and dispatches inbox messages in batches of 20, every 5 seconds. |
| `InboxHandlerWrapper<TEvent>` | `IInboxHandlerWrapper` — wraps the real `IEventHandler` so it is only called by `InboxProcessorService`. |

#### Registration (per module)
```csharp
// In LoyaltyModule.cs
services.AddInboxEventHandler<PaymentSucceededEvent, PaymentSucceededEventHandler, LoyaltyDbContext>();
services.AddInbox<LoyaltyDbContext>();
```

---

## 📦 Project Structure & Modules

```
src/
├── ModularMonolith/                # Main Host Application (API & Composition Root)
│   └── Program.cs                  # Registers modules & Swagger
├── Modules/                        # Isolated Domain Modules
│   ├── Payment/                    # Handles payments (schema: payment) -> Publishes: PaymentSucceededEvent
│   ├── Loyalty/                    # Manages points (schema: loyalty) -> Listens: Payment; Publishes: PointsAdded
│   └── Notification/               # Sends emails -> Listens: Payment, Loyalty
└── Shared/                         # Shared Packages & Contracts
    ├── SharedBus/                  # Outbox/Inbox entities and event interfaces
    ├── SharedInfrastructure/       # EventBus, Outbox & Inbox background services
    └── SharedKernel/               # Common abstractions
```

### Module Responsibilities

| Module | Description | Listens To | Publishes | Database Schema |
|--------|-------------|------------|-----------|-----------------|
| **Payment** | Handles checkout and payment processes using MediatR & FluentValidation. | — | `PaymentSucceededEvent` | `payment` |
| **Loyalty** | Awards points based on successful payments (1 pt per 10 TL). Uses **Inbox Pattern** for idempotent processing. | `PaymentSucceededEvent` | `PointsAddedEvent` | `loyalty` |
| **Notification** | Sends transactional emails (MailKit) upon successful payments and points earned. | `PaymentSucceededEvent`, `PointsAddedEvent` | `EmailSentEvent` | *None* |

---

## 🛠️ Technology Stack

* **Framework:** .NET 9 (ASP.NET Core Minimal APIs)
* **Database:** PostgreSQL 16 (Entity Framework Core 9)
* **Architecture Patterns:** Modular Monolith, CQRS, Outbox Pattern, Inbox Pattern, Event-Driven
* **Libraries:** MediatR, FluentValidation, MailKit
* **Observability:** OpenTelemetry & Jaeger
* **Infrastructure:** Docker, Docker Compose, Kubernetes (Docker Desktop), ArgoCD (GitOps)

---

## 🚀 Getting Started

### Prerequisites
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Option A — Docker Compose (Local Development)

#### 1. Start Infrastructure
```bash
docker-compose up -d
```
* **PostgreSQL:** `localhost:5492`
* **pgAdmin:** `http://localhost:8090` (admin@admin.com / admin)
* **Jaeger UI:** `http://localhost:16686`

#### 2. Run the Application
```bash
cd src/ModularMonolith
dotnet run
```
The application automatically applies all EF Core migrations on startup.

#### 3. Explore the API
* **Swagger:** `https://localhost:5001/swagger`

---

### Option B — Kubernetes + ArgoCD (GitOps)

The project ships with a full GitOps pipeline using ArgoCD on Docker Desktop Kubernetes.

#### 1. Install ArgoCD
```bash
kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
kubectl wait --for=condition=available --timeout=180s deployment/argocd-server -n argocd
```

#### 2. Expose ArgoCD UI
```bash
kubectl patch svc argocd-server -n argocd -p '{"spec":{"type":"NodePort","ports":[{"name":"http","port":80,"targetPort":8080,"nodePort":31312}]}}'
```
* **ArgoCD UI:** `http://localhost:31312`  (user: `admin`)

#### 3. Build Docker Image
```bash
docker build -f src/ModularMonolith/Dockerfile -t modular-monolith:latest .
```

#### 4. Apply Manifests
```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/
kubectl apply -f k8s/argocd/
```

#### 5. Access the Application
* **API / Swagger:** `http://localhost:30080`
* **Jaeger:** `kubectl port-forward svc/modular-monolith-jaeger 16686:16686 -n modular-monolith-system`

> After initial setup, any `git push` to the `k8s/` directory automatically triggers ArgoCD sync (Automated + SelfHeal policy).

---

## 📝 Full Event Flow (Payment → Loyalty with Inbox Pattern)

```
Client  POST /api/payment/process
          │
          ▼
  [Payment Module]
    1. Save Payment record  ─┐
    2. Save OutboxMessage   ─┘  (same DB transaction → no dual-write)
          │
          ▼
  [OutboxProcessorService]   (polls every 5 s)
    3. Read OutboxMessage → publish to InMemoryEventBus
          │
          ▼
  [EventBusBackgroundService]
    4. Dispatch to all subscribers in parallel
          │
     ┌────┴──────────────────────┐
     ▼                           ▼
  [InboxSaverWrapper]       [Notification Handler]
   (Loyalty module)           sends email directly
    5. Write InboxMessage
       to loyalty schema
          │
          ▼
  [InboxProcessorService]   (polls every 5 s)
    6. Read InboxMessage → call PaymentSucceededEventHandler
    7. Save UserPoints + PointsAddedEvent OutboxMessage
    8. Mark InboxMessage.ProcessedAt = now
          │
          ▼
  (cycle continues: PointsAddedEvent → Notification inbox...)
```

**Key guarantees:**
* **At-Least-Once** delivery — Outbox ensures publish, Inbox ensures consume
* **Idempotency** — `InboxMessage.Id` UNIQUE constraint prevents duplicate processing
* **Retry** — up to 3 attempts per failed message
* **Dead Letter** — messages exceeding retry limit logged at `CRITICAL` level

---

## 📧 Email Configuration
Configure SMTP credentials in `appsettings.json` under the `EmailSettings` node to enable MailKit email dispatch.
