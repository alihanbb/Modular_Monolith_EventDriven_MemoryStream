# InMemory EventBus — Kullanım Kılavuzu

## Nedir?

InMemory EventBus, `System.Threading.Channels.Channel<T>` üzerine inşa edilmiş, harici bağımlılık gerektirmeyen, process-içi bir event yayın/dinleme mekanizmasıdır. Modüller arası **loose coupling** (gevşek bağlantı) sağlarken Redis, RabbitMQ veya Kafka gibi altyapı gerektirmez.

---

## Bileşenler

### `IEvent`
Tüm event'lerin uygulaması gereken temel arayüz:

```csharp
public interface IEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
}
```

Kullanım:
```csharp
public sealed record MyEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string Data { get; init; } = string.Empty;
}
```

---

### `IEventBus`
Event yayınlama arayüzü:

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;
}
```

DI ile enjekte edip kullanın:

```csharp
public class MyService(IEventBus eventBus)
{
    public async Task DoSomethingAsync()
    {
        await eventBus.PublishAsync(new MyEvent { Data = "hello" });
    }
}
```

---

### `IEventHandler<T>`
Event dinleme arayüzü:

```csharp
public interface IEventHandler<T> where T : IEvent
{
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}
```

Yeni bir handler oluşturmak için:

```csharp
public class MyEventHandler(ILogger<MyEventHandler> logger) : IEventHandler<MyEvent>
{
    public async Task HandleAsync(MyEvent @event, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyEvent alındı: {Data}", @event.Data);
        await Task.CompletedTask;
    }
}
```

---

### `InMemoryEventBus`
`Channel<IEvent>` kullanarak event'leri kuyruğa alır. `PublishAsync` anında döner (non-blocking).

### `EventBusBackgroundService`
`BackgroundService` olarak çalışır:
1. Channel'dan event'leri sırayla okur
2. Her event için yeni bir DI **scope** açar (scoped servisler güvenle kullanılabilir)
3. İlgili tüm `IEventHandler<T>` implementasyonlarını **paralel** çalıştırır
4. Handler hataları diğer handler'ları etkilemez (hata izole edilir)

---

## DI Kaydı

### Otomatik kayıt (assembly tarama)
```csharp
// Program.cs veya modül kayıt dosyasında
services.AddInMemoryEventBus();
services.AddEventHandlersFromAssemblyContaining<Program>();
```

### Manuel kayıt (önerilen — modül bazlı kontrol)
```csharp
// NotificationModule.cs içinde
services.AddScoped<IEventHandler<PaymentSucceededEvent>, PaymentSucceededEventHandler>();
services.AddScoped<IEventHandler<PointsAddedEvent>, PointsAddedEventHandler>();
```

Bir event türü için **birden fazla handler** kaydedebilirsiniz — hepsi çalıştırılır:
```csharp
services.AddScoped<IEventHandler<PaymentSucceededEvent>, LoyaltyHandler>();
services.AddScoped<IEventHandler<PaymentSucceededEvent>, NotificationHandler>();
// Her iki handler da paralel çalışır
```

---

## Event Akışı (Detaylı)

```
1. PublishAsync(event) çağrılır
       │
       ▼
2. Channel.Writer.WriteAsync(event) — anında döner (non-blocking)
       │
       ▼ (arka planda)
3. EventBusBackgroundService.ExecuteAsync()
   Channel.Reader.ReadAllAsync() ile event alınır
       │
       ▼
4. DI scope açılır (using var scope = provider.CreateAsyncScope())
       │
       ▼
5. IEventHandler<T> implementasyonları çözülür
       │
       ▼
6. Task.WhenAll(...) — tüm handler'lar paralel çalışır
       │
       ├──► Handler1.HandleAsync(event) 
       └──► Handler2.HandleAsync(event)
```

---

## Hata Yönetimi

- Her handler kendi try-catch içinde çalışır
- Bir handler hata verse diğerleri etkilenmez
- Hatalar `ILogger` ile loglanır
- **Retry mekanizması** gerekiyorsa handler içinde Polly kullanılabilir:

```csharp
public class MyRobustHandler : IEventHandler<MyEvent>
{
    public async Task HandleAsync(MyEvent @event, CancellationToken ct)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i));
            
        await retryPolicy.ExecuteAsync(() => DoWorkAsync(@event, ct));
    }
}
```

---

## Mevcut Event'ler

| Event | Yayınlayan | Dinleyenler |
|-------|-----------|-------------|
| `PaymentSucceededEvent` | Payment modülü | Loyalty, Notification |
| `PointsAddedEvent` | Loyalty modülü | Notification |
| `EmailSentEvent` | Notification modülü | (audit/log için, şu an dinleyen yok) |

---

## Yeni Event Ekleme

1. `Shared/Events/` altında `IEvent` uygulayan bir record oluştur
2. Yayınlayan modülde `IEventBus.PublishAsync()` çağır
3. Dinleyen modülde `IEventHandler<T>` uygula
4. Modülün `*Module.cs` dosyasında handler'ı DI'ya kaydet

```csharp
// 1. Shared/Events/OrderShippedEvent.cs
public sealed record OrderShippedEvent : IEvent {
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid OrderId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
}

// 2. Shipping modülünde yayınla
await eventBus.PublishAsync(new OrderShippedEvent { OrderId = orderId, TrackingNumber = "TRK123" });

// 3. Notification modülünde dinle
public class OrderShippedHandler : IEventHandler<OrderShippedEvent> { ... }

// 4. NotificationModule.cs'e ekle
services.AddScoped<IEventHandler<OrderShippedEvent>, OrderShippedHandler>();
```

---

## Mikroservis'e Geçiş Yolu

InMemory EventBus, gerçek bir message broker ile değiştirilebilir tasarımda yapılmıştır. İleride ölçeklendirme gerektiğinde:

1. `IEventBus` arayüzünü koruyun
2. `InMemoryEventBus` yerine `RabbitMqEventBus` veya `KafkaEventBus` ekleyin
3. DI kaydını değiştirin — uygulama kodu değişmez

```csharp
// Geçiş: sadece bu satır değişir
services.AddSingleton<IEventBus, RabbitMqEventBus>(); // eski: InMemoryEventBus
```
