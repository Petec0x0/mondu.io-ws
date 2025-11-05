# Multi-Tenant Wallet System

A secure, scalable wallet system built with **.NET 8**, featuring **multi-tenant architecture**, **payment gateway integration**, and **transaction-safe operations**.

---
## Architecture Diagram Specification
#### 1. Multi-Tenancy Data Isolation (Tenant_ID Pattern)
<img width="486" height="625" alt="Multi-Tenancy-Data-Isolation" src="https://github.com/user-attachments/assets/83f70785-1843-4365-8304-2b1f3e942a57" />

#### 2. Services Architecture
<img width="414" height="767" alt="Services-Architecture" src="https://github.com/user-attachments/assets/0bf8eda7-61df-4107-8854-d86bb8bc77c1" />

#### 3. MPGS Integration Flow
<img width="467" height="712" alt="3-MPGS-Integration-Flow" src="https://github.com/user-attachments/assets/4ced9861-e7fb-4584-818a-b0c0fa14b695" />

#### 4. Database Schema Details
<img width="375" height="656" alt="4-Database-Schema-Details" src="https://github.com/user-attachments/assets/e54ac485-4d6e-4754-9191-82a89bdeed8d" />

#### 5. Complete System Flow
<img width="527" height="470" alt="5-Complete-System-Flow" src="https://github.com/user-attachments/assets/156acc06-81ec-4159-afe7-4bbe7b7698dd" />

#### 6. Transaction Safety Mechanism
<img width="427" height="342" alt="6-Transaction-Safety-Mechanism" src="https://github.com/user-attachments/assets/8d1d3fb0-5ce0-47fa-9a6a-3c975bdab9fd" />

#### 7. Tenant Isolation Flow
<img width="423" height="451" alt="7-Tenant-Isolation-Flow" src="https://github.com/user-attachments/assets/4e03f996-e1aa-44e1-97fe-abfbd34d2f47" />

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) & Docker Compose

### Running the Application

```bash
# Start PostgreSQL database
docker-compose up -d

# Run the application
dotnet run
```

**API available at:**
- HTTP → http://localhost:5168

---

## API Endpoints

| Method | Endpoint | Description |
|--------|-----------|-------------|
| GET | `/api/wallet/balance/{userId}` | Get wallet balance |
| GET | `/api/wallet/transactions/{userId}` | Get transaction history |
| POST | `/api/wallet/topup/initiate` | Initiate payment top-up |
| POST | `/api/wallet/topup/confirm` | Confirm top-up completion |
| POST | `/api/wallet/transfer` | Transfer funds between users |

---

## Architecture

**System Design**
```
Client → API Layer → Business Layer → Data Layer → PostgreSQL
                      │
                      └→ Payment Gateway (MPGS)
```

### Multi-Tenancy Strategy
- **Pattern:** Tenant_ID based isolation  
- **Implementation:** Global query filters + request middleware  
- **Header:** `X-Tenant-ID (GUID)`

---

## Database Schema

```sql
Tenants (Id, Name, IsActive)
Wallets (Id, UserId, TenantId, Balance, Currency, CreatedAt)
Transactions (Id, WalletId, Amount, BalanceBefore, BalanceAfter, Type, Status, Description, Reference, CreatedAt)
```

---

## Core Features

### Transaction Safety
- Database transactions with proper isolation levels  
- Balance verification within transaction scope  
- Automatic rollback on failures  
- Prevention of race conditions and double-spending  

### Payment Integration
- Mock MPGS payment gateway  
- Payment initiation and verification flow  
- Webhook-style callback processing  
- Secure payment reference tracking  

### Multi-Tenant Security
- Tenant isolation at application and data levels  
- Cross-tenant transfer prevention  
- Tenant context middleware  
- Secure data access patterns  

---

## ⚙️ Technical Implementation

### Services
- `IWalletService` — Core business logic (transfers, top-ups, balance checks)  
- `IPaymentService` — Payment gateway abstraction  
- `ICurrentTenantService` — Tenant context management  

### Key Technologies
- **.NET 8** — ASP.NET Core Web API  
- **Entity Framework Core** — PostgreSQL ORM  
- **Docker** — Containerized database  
- **GUIDs** — Universal identifiers for all entities  

---

## Testing the API

### Get Balance
```bash
curl -X GET "http://localhost:5001/api/wallet/balance/33333333-3333-3333-3333-333333333333"
```

### Transfer Funds
```bash
curl -X POST "http://localhost:5001/api/wallet/transfer"   -H "Content-Type: application/json"   -d '{
    "fromUserId": "33333333-3333-3333-3333-333333333333",
    "toUserId": "44444444-4444-4444-4444-444444444444",
    "amount": 50
  }'
```

### Top-Up Flow
```bash
# 1. Initiate payment
curl -X POST "http://localhost:5001/api/wallet/topup/initiate"   -H "Content-Type: application/json"   -d '{
    "amount": 100,
    "userId": "33333333-3333-3333-3333-333333333333"
  }'

# 2. Confirm payment (use transactionId from step 1)
curl -X POST "http://localhost:5001/api/wallet/topup/confirm?transactionId=YOUR_TX_ID&userId=33333333-3333-3333-3333-333333333333"
```

### Get Transactions
```bash
curl -X GET "http://localhost:5001/api/wallet/transactions/33333333-3333-3333-3333-333333333333"
```

---

## Project Structure

```
SimpleWalletSystem/
├── Controllers/
│   └── WalletController.cs          # API endpoints
├── Models/
│   ├── Wallet.cs                    # Domain models
│   ├── Transaction.cs
│   ├── Tenant.cs
│   └── TransactionDto.cs            # Data transfer objects
├── Services/
│   ├── IWalletService.cs            # Business logic contracts
│   ├── WalletService.cs             # Core wallet operations
│   └── PaymentService.cs            # Payment gateway integration
├── Middleware/
│   └── TenantMiddleware.cs          # Multi-tenant context
├── Program.cs                       # Application configuration
└── docker-compose.yml               # Database infrastructure
```

---

## Security & Safety Features

### Data Consistency
- ACID transactions for all financial operations  
- Balance validation before transfers  
- Audit trails for all transactions  
- No partial updates in case of failures  

### Tenant Isolation
- Data segregation by `TenantId`  
- Cross-tenant operation prevention  
- Secure tenant context propagation  
- Query filtering at EF Core level  

### Error Handling
- Comprehensive exception handling  
- User-friendly error messages  
- Transaction rollback on errors  
- Structured logging  

---

## Design Decisions

### Why Tenant_ID Pattern?
- Simpler than schema-per-tenant  
- Easier database maintenance  
- Better performance for cross-tenant analytics  
- Simplified backup/restore  

### Why GUIDs?
- Globally unique identifiers  
- No ID collision concerns  
- Secure against ID enumeration  
- Ideal for distributed systems  

### Transaction Safety Approach
- Database-level transactions  
- Serializable isolation for consistency  
- Explicit balance checks within transaction scope  
- Proper cleanup on failures  

### Scalability Considerations
- Stateless service design  
- Connection pooling  
- Async operations  
- Separation of concerns  
- Mockable dependencies  

---

## Development Workflow

```bash
# Start database
docker-compose up -d

# Run app
dotnet run
```

Then:
- Test: Use provided curl commands
- Debug: Logs appear in console output  

---

## ⚠️ Important Notes
- **Default Tenant:** `11111111-1111-1111-1111-111111111111` (if no `X-Tenant-ID` header)  
- **Seeded Data:** Includes 2 tenants & 3 demo wallets  
- **Mock Payments:** Simulates MPGS API responses  
- **Authentication:** JWT mocked for demo  
