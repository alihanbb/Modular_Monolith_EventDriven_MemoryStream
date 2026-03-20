# Modular Monolith Architecture

## Overview

This project transforms a **microservice**-designed e-commerce platform into a **modular monolith** architecture. It features a single ASP.NET Core application with strictly isolated modules (`Payment`, `Loyalty`, `Notification`). Each module has its own `DbContext` and PostgreSQL schema (e.g., `payment.Payments`, `loyalty.UserPoints`). Modules do not reference each other directly; they communicate through shared events.

---

## Proje Yapısı

```
src/ModularMonolith/
│
├── Infrastructure/
│   └── EventBus/                     # Event bus altyapısı
│       ├── IEvent.cs                 # Tüm event'lerin temel arayüzü
│       ├── IEventBus.cs              # Publish arayüzü
│       ├── IEventHandler.cs          # Handler arayüzü
│       ├── InMemoryEventBus.cs       # Channel<T> tabanlı implementasyon
│       ├── EventBusBackgroundService.cs  # Dispatcher arka plan servisi
│       └── EventBusServiceExtensions.cs  # DI kayıt extension'ları
│
├── Shared/
│   └── Events/                       # Modüller arası paylaşılan event'ler
│       ├── PaymentSucceededEvent.cs
│       ├── PointsAddedEvent.cs
│       ├── EmailSentEvent.cs
│       └── PaymentStatus.cs
│
├── Modules/
│   ├── Payment/                      # Ödeme modülü
│   │   ├── Domain/Entities/          # Payment entity
│   │   ├── Application/Commands/     # ProcessPaymentCommand (MediatR)
│   │   ├── Infrastructure/Persistence/ # PaymentDbContext (schema: payment)
│   │   ├── API/Endpoints/            # Minimal API endpoint
│   │   └── PaymentModule.cs          # Modül kayıt dosyası
│   │
│   ├── Loyalty/                      # Sadakat puan modülü
│   │   ├── Domain/Entities/          # UserPoints entity
│   │   ├── Application/EventHandlers/ # PaymentSucceededEventHandler
│   │   ├── Infrastructure/Persistence/ # LoyaltyDbContext (schema: loyalty)
│   │   └── LoyaltyModule.cs
│   │
│   └── Notification/                 # Bildirim modülü
│       ├── Domain/Services/          # IEmailService arayüzü
│       ├── Domain/Configuration/     # EmailSettings
│       ├── Application/EventHandlers/ # PaymentSucceeded + PointsAdded handler'ları
│       ├── Infrastructure/Services/  # EmailService (MailKit SMTP)
│       └── NotificationModule.cs
│
├── Program.cs                        # Uygulama giriş noktası
├── DatabaseMigrationExtensions.cs    # Otomatik migration
├── appsettings.json
└── Dockerfile
```

---

## Modüller ve Sorumlulukları

### Payment Modülü
- **Sorumluluk:** Ödeme işleme, sipariş oluşturma
- **API:** `POST /api/payment/process`
- **Yayınladığı Event:** `PaymentSucceededEvent`
- **Veritabanı:** PostgreSQL `payment` schema
- **Kullandığı Kütüphaneler:** MediatR (CQRS), FluentValidation

### Loyalty Modülü
- **Sorumluluk:** Kullanıcı sadakat puanlarını yönetme
- **Dinlediği Event:** `PaymentSucceededEvent`
- **Yayınladığı Event:** `PointsAddedEvent`
- **Veritabanı:** PostgreSQL `loyalty` schema
- **Puan Kuralı:** Her 10 TL için 1 puan (%10 oran)

### Notification Modülü
- **Sorumluluk:** E-posta bildirimleri gönderme
- **Dinlediği Event'ler:** `PaymentSucceededEvent`, `PointsAddedEvent`
- **Yayınladığı Event:** `EmailSentEvent` (audit/loglama amaçlı)
- **Veritabanı:** Yok
- **Kullandığı Kütüphaneler:** MailKit (SMTP)

---

## Event Akışı

```
POST /api/payment/process
        │
        ▼
ProcessPaymentHandler (MediatR)
        │ veritabanına kayıt
        │ IEventBus.PublishAsync(PaymentSucceededEvent)
        │
        ▼ [Channel<IEvent>]
EventBusBackgroundService
        │
        ├──► Loyalty: PaymentSucceededEventHandler
        │         │ puan hesapla & kaydet
        │         │ IEventBus.PublishAsync(PointsAddedEvent)
        │         │
        │         ▼ [Channel<IEvent>]
        │    Notification: PointsAddedEventHandler
        │         └─ puan kazanma e-postası gönder
        │
        └──► Notification: PaymentSucceededEventHandler
                   └─ ödeme onay e-postası gönder
```

---

## Veritabanı Stratejisi

Tek bir PostgreSQL veritabanı kullanılır (`modularmonolithdb`). Her modül kendi **schema**'sına sahiptir:

| Modül | Schema | Tablolar |
|-------|--------|---------|
| Payment | `payment` | `payment."Payments"` |
| Loyalty | `loyalty` | `loyalty."UserPoints"` |

Schema izolasyonu sayesinde modüller birbirinin tablolarına doğrudan erişemez.

---

## Modül İzolasyon Kuralları

1. **Modüller arası doğrudan referans yasak** — Loyalty, Payment'ın DbContext'ini kullanamaz
2. **Yalnızca event üzerinden iletişim** — `Shared/Events/` klasöründeki event'ler ortak sözleşmedir
3. **Her modülün kendi DbContext'i var** — Ayrı schema, ayrı migration
4. **Namespace izolasyonu** — `ModularMonolith.Modules.Payment.*`, `ModularMonolith.Modules.Loyalty.*` vb.

---

## Teknoloji Yığını

| Katman | Teknoloji |
|--------|-----------|
| Framework | ASP.NET Core 9, .NET 9 |
| ORM | Entity Framework Core 9 + Npgsql |
| CQRS (Payment) | MediatR 13 |
| Validasyon | FluentValidation 11 |
| Email | MailKit 4 |
| Event Bus | System.Threading.Channels (built-in) |
| Veritabanı | PostgreSQL 16 |
| Container | Docker + Docker Compose |


