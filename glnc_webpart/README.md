# GLNC WebPart

ASP.NET Core Web API project for GLNC (Geolocation Logistics Network Control) system.

## Project Structure

```
glnc_webpart/
├── Controllers/          # API Controllers
├── Data/                 # DbContext and Data Access
├── Models/               # Entity Models
├── Services/             # Business Logic Services (to be added)
├── Program.cs            # Application Entry Point
├── appsettings.json     # Configuration
└── glnc_webpart.csproj  # Project File
```

## Database

The project uses MySQL database with the following tables:
- `users` - User accounts
- `attendance_tracking` - Employee attendance records
- `delivery` - Delivery records
- `delivery_geolocation` - Delivery location tracking
- `drivergeolocation` - Driver location tracking
- `message` - User messages
- `supplier` - Supplier information
- `trucks` - Truck information

## Setup

1. Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=glnc_db;User=root;Password=yourpassword;Port=3306;"
   }
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Create database migrations (if needed):
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

5. Access Swagger UI at: `https://localhost:5001/swagger` (or the port shown in console)

## Technologies

- ASP.NET Core 8.0
- Entity Framework Core 8.0
- Pomelo.EntityFrameworkCore.MySql (MySQL provider)
- Swagger/OpenAPI

## API Endpoints

- `GET /api/Users` - Get all users
- `GET /api/Users/{id}` - Get user by ID
- `POST /api/Users` - Create new user
- `PUT /api/Users/{id}` - Update user
- `DELETE /api/Users/{id}` - Delete user

Additional controllers can be added following the same pattern.

