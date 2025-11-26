# ECommerceEvents Microservices - Redis Streams with MassTransit

## Overview
This solution demonstrates a **Fan-Out pattern** with .NET 9, MassTransit, and Redis Streams. One Producer (PaymentApi) publishes events that are consumed independently by two Consumer services (Notification and Loyalty).

##Solution Structure

```
ECommerceEvents/
├── src/
│   ├── Shared/                   # Event contracts
│   ├── PaymentService/          # Producer API
│   ├── NotificationService/      # Consumer - Email notifications
│   └── LoyaltyService/          # Consumer - Loyalty points
└── docker-compose.yml
```

## Running the Solution

### 1. Start Redis
```bash
docker-compose up -d redis
```

### 2. Run All Services

Open 3 terminals and run each service:

**Terminal 1 - PaymentService (Producer):**
```bash
cd  src/PaymentService
dotnet run
```

**Terminal 2 - NotificationService:**
```bash
cd src/NotificationService
dotnet run
```

**Terminal 3 - LoyaltyService:**
```bash
cd src/LoyaltyService
dotnet run
```

### 3. Test the Fan-Out Pattern

Send a payment request:
```bash
curl -X POST http://localhost:5001/api/payment/process \
  -H "Content-Type: application/json" \
  -d '{"userId":"user123","amount":99.99}'
```

**Expected Result:**
- PaymentService publishes the event
- Both NotificationService AND LoyaltyService receive and process the SAME event independently
- Email notification is sent (via MailKit)
- Loyalty points are calculated and logged

## Email Configuration

Before running NotificationService, update `appsettings.json` with your SMTP settings:

```json
{
  "EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@ecommerce.com",
    "FromName": "ECommerce Platform",
    "EnableSsl": true
  }
}
```

> **Note:** For Gmail, you need to create an [App Password](https://support.google.com/accounts/answer/185833).

## Testing Validation

**Valid Request:**
```bash
curl -X POST http://localhost:5001/api/payment/process \
  -H "Content-Type: application/json" \
  -d '{"userId":"john.doe","amount":250.00}'
```

**Invalid Request (Amount = 0):**
```bash
curl -X POST http://localhost:5001/api/payment/process \
  -H "Content-Type: application/json" \
  -d '{"userId":"john.doe","amount":0}'
```

Response:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Amount": ["Amount must be greater than 0"]
  }
}
```

## Key Features

### 1. **Minimal API** (PaymentService)
- No controllers, uses endpoint routing
- Built-in FluentValidation middleware
- Clean, concise API definitions

### 2. **FluentValidation**
- Request validation before processing
- Clear validation error messages
- Automatic 400 Bad Request responses

### 3. **MailKit Email Sending**
- SMTP email delivery with HTML templates
- Professional email formatting
- Error handling and logging

### 4. **Fan-Out Pattern**
- Single event published to Redis Stream
- Multiple independent consumers
- Each consumer has its own consumer group
- Guaranteed delivery to all consumers

## Troubleshooting

**Redis Connection Issues:**
```bash
# Check if Redis is running
docker ps | grep redis

# View Redis logs
docker logs ecommerce-redis
```

**Email Send Failures:**
- Verify SM TP credentials in `appsettings.json`
- Check firewall/antivirus settings
- For Gmail, ensure "Less secure app access" or App Password is configured

**Build Errors:**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## Technologies Used

- .NET 9
- MassTransit 8.x
- Redis Streams (via MassTransit.Redis package)
- FluentValid ation
- MailKit
- PostgreSQL (for PaymentService database)
- Docker

