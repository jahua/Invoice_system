# Invoice Management System (Prototype)

A prototype web-based invoice management system for Company XYZ, allowing employees to submit invoices and managers to review them.

> **Note**: This is a prototype version with basic functionality. Some features may not be fully implemented.

## System Requirements

- .NET 8.0 SDK or later
- Any of the following databases:
  - PostgreSQL 13 or later
  - SQL Server
  - SQLite (for development)
- Modern web browser (Chrome, Firefox, Safari, Edge)

## Project Structure

```
invoice/
├── database design/          # Database design documentation and scripts
│   ├── schema.sql           # Database schema creation script (PostgreSQL format)
│   ├── seed-data.sql        # Sample data for testing (PostgreSQL format)
│   ├── README.md            # Database documentation
│   └── invoice Entity.drawio.pdf  # Database entity relationship diagram
├── src/                     # Source code
│   ├── InvoiceSystem.Domain/        # Domain layer
│   ├── InvoiceSystem.Infrastructure/# Infrastructure layer
│   └── InvoiceSystem.Web/           # Web application
├── .gitignore              # Git ignore file
└── README.md               # This file
```

## Setup Instructions

1. **Automatic Database Setup**
   ```bash
   # The database will be automatically created and seeded
   # through DataSeedingService when the application starts
   # No manual setup or migrations needed
   ```

   > **Note**: The application uses Entity Framework Core and includes DataSeedingService
   > which automatically handles database creation and sample data population.

2. **Application Configuration**
   - Copy `appsettings.json.example` to `appsettings.json`
   - Update the connection string to match your database settings
   - Default configuration uses PostgreSQL for easy development setup

3. **Build and Run**
   ```bash
   # Restore dependencies
   dotnet restore

   # Build the solution
   dotnet build

   # Run the application
   cd src/InvoiceSystem.Web
   dotnet run
   ```

## Prototype Features Status

Currently implemented basic features:
- ✅ User authentication (login/logout)
- ✅ Basic role-based authorization
- ✅ Simple invoice submission
- ✅ Basic invoice listing
- ✅ Data seeding service for sample data

Features in development:
- 🚧 Advanced invoice management
- 🚧 Contract management
- 🚧 Employee management
- 🚧 Detailed reporting
- 🚧 Advanced validation rules

## Default Users (When Using Seeding Service)

Sample accounts created by the data seeding service:

| Username  | Password    | Role           |
|-----------|------------|----------------|
| manager1  | password123| InvoiceManager |
| employee1 | password123| Employee       |
| employee2 | password123| Employee       |
| employee3 | password123| Employee       |

## Development Notes

- Uses Entity Framework Core with code-first approach
- Includes DataSeedingService for automatic database setup and sample data
- Basic cookie-based authentication implemented
- Password hashing using BCrypt
- Bootstrap 5 for basic UI styling

## Database Design

The `database design` directory contains documentation only:
- `schema.sql`: Reference schema documentation
- `seed-data.sql`: Reference sample data documentation
- `invoice Entity.drawio.pdf`: Entity relationship diagram
- `README.md`: Database documentation

> **Note**: Database creation and seeding is handled automatically by the application.
> The SQL scripts are provided as documentation only.



## Support

For technical support or questions during prototype testing:
- Email: support@company.xyz
- Phone: +1-555-0100 
