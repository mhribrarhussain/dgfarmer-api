# 🌾 Digital Farmer - Backend API

RESTful API backend for the Digital Farmer marketplace, built with ASP.NET Core 8.0.

![.NET](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-blue?logo=csharp)
![SQLite](https://img.shields.io/badge/SQLite-3-blue?logo=sqlite)

## 🚀 Features

- 🔐 **JWT Authentication**: Secure token-based auth
- 👥 **User Management**: Farmer and Buyer roles
- 📦 **Product CRUD**: Full product management with image upload (Base64)
- 🛒 **Order System**: Multi-item orders with status tracking
- 💬 **Messaging**: Order-based messaging between farmers and buyers
- 📊 **RESTful API**: Clean, well-documented endpoints
- 🗄️ **Entity Framework Core**: Code-first database with migrations

## 🛠️ Tech Stack

- **Framework**: ASP.NET Core 8.0 (Minimal APIs)
- **Language**: C# 12
- **Database**: SQLite (dev), PostgreSQL-ready
- **ORM**: Entity Framework Core 8.0
- **Authentication**: JWT (JSON Web Tokens)
- **Password Hashing**: BCrypt.Net
- **API Documentation**: Swagger/OpenAPI

## 📁 Project Structure

```
DgFarmerApi/
├── Controllers/         # API endpoints
│   ├── AuthController.cs       (login, register)
│   ├── ProductsController.cs   (CRUD products)
│   ├── OrdersController.cs     (order management)
│   └── MessagesController.cs   (messaging)
├── Models/             # Database entities
├── DTOs/               # Data Transfer Objects
├── Data/               # DbContext and seeding
├── Services/           # Business logic
└── Program.cs          # App configuration
```

## 🛠️ Installation & Setup

### Prerequisites
- .NET 8.0 SDK
- SQLite (included) or PostgreSQL (optional)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/dgfarmer-api.git
   cd dgfarmer-api
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure JWT Secret** (optional)
   - Edit `appsettings.json`
   - Update `Jwt:Key` with a secure random string

4. **Run the application**
   ```bash
   dotnet run
   ```
   API runs on `http://localhost:5004`
   Swagger UI: `http://localhost:5004/swagger`

5. **Database**
   - SQLite database (`dgfarmer.db`) is created automatically
   - Sample data is seeded on first run

## 📊 Database Schema

### Users
- `Id`, `Name`, `Email`, `PasswordHash`, `Role` (farmer/buyer), `Phone`, `Address`

### Products
- `Id`, `Name`, `Description`, `Price`, `Category`, `Unit`, `Stock`, `Rating`, `Image`, `FarmerId`

### Orders
- `Id`, `UserId`, `Status` (pending/accepted/rejected/cancelled), `Total`, `CreatedAt`

### OrderItems
- `Id`, `OrderId`, `ProductId`, `Quantity`, `Price`

### Messages
- `Id`, `OrderId`, `SenderId`, `Content`, `CreatedAt`

## 🔌 API Endpoints

### Authentication
```
POST /api/auth/register    - Register new user
POST /api/auth/login       - Login (returns JWT)
```

### Products
```
GET    /api/products       - Get all products
GET    /api/products/{id}  - Get product by ID
POST   /api/products       - Create product (farmer only)
PUT    /api/products/{id}  - Update product (farmer only)
DELETE /api/products/{id}  - Delete product (farmer only)
```

### Orders
```
GET  /api/orders           - Get user's orders
GET  /api/orders/received  - Get farmer's orders
POST /api/orders           - Place new order
POST /api/orders/{id}/accept  - Accept order (farmer)
POST /api/orders/{id}/reject  - Reject order (farmer)
POST /api/orders/{id}/cancel  - Cancel order (buyer)
```

### Messages
```
GET  /api/orders/{orderId}/messages      - Get messages for order
POST /api/orders/{orderId}/messages      - Send message
```

## 🔐 Authentication

The API uses JWT Bearer tokens. After login, include the token in requests:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Sample Login Request
```json
POST /api/auth/login
{
  "email": "farmer@dgfarmer.com",
  "password": "farmer123"
}
```

### Response
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "name": "Ahmed Khan",
    "email": "farmer@dgfarmer.com",
    "role": "farmer"
  }
}
```

## 🌐 Deployment

### Deploy to Render

1. **Create `render.yaml`** (already included)

2. **Push to GitHub**
   ```bash
   git push origin master
   ```

3. **Connect to Render**
   - Go to [Render Dashboard](https://render.com)
   - New → Blueprint
   - Connect your GitHub repo
   - Render will auto-deploy using `render.yaml`

4. **Set Environment Variables** (in Render dashboard)
   ```
   ASPNETCORE_URLS=http://+:$PORT
   Jwt__Key=your-super-secret-key-min-32-chars
   Jwt__Issuer=dgfarmer-api
   Jwt__Audience=dgfarmer-client
   ```

5. **Database** (Optional)
   - Default: SQLite (ephemeral on Render)
   - For persistence: Add PostgreSQL database in Render
   - Update connection string in `Program.cs`

## 🧪 Testing

### Manual Testing
Use Swagger UI at `http://localhost:5004/swagger`

### With Tools
```bash
# Using curl
curl -X POST http://localhost:5004/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"farmer@dgfarmer.com","password":"farmer123"}'
```

### Demo Accounts (Seeded)
- **Farmer**: `farmer@dgfarmer.com` / `farmer123`
- **Buyer**: `buyer@dgfarmer.com` / `buyer123`

## 🔧 Configuration

### CORS
CORS is configured in `Program.cs`:
- **Development**: Allows all origins (`AllowAll` policy)
- **Production**: Specify frontend URLs in `AllowFrontend` policy

### Database
To switch from SQLite to PostgreSQL:
1. Install `Npgsql.EntityFrameworkCore.PostgreSQL`
2. Update connection string in `Program.cs`
3. Run migrations: `dotnet ef migrations add InitialCreate`

## 📝 Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_URLS` | Server binding URL | `http://localhost:5004` |
| `Jwt__Key` | JWT signing key | (from appsettings.json) |
| `Jwt__Issuer` | JWT issuer | `dgfarmer-api` |
| `Jwt__Audience` | JWT audience | `dgfarmer-client` |

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📝 License

MIT License

## 👨‍💻 Author

Created with ❤️ by [Your Name]

## 🙏 Acknowledgments

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [BCrypt.Net](https://github.com/BcryptNet/bcrypt.net)
