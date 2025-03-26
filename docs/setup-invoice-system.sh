#!/bin/bash
# Setup script for the Invoice System Prototype using .NET 8

echo "Creating Invoice System Prototype project structure..."

# Create solution
dotnet new sln -n InvoiceSystem

# Create projects
echo "Creating projects..."
dotnet new webapp -f net8.0 -n InvoiceSystem.Web
dotnet new classlib -f net8.0 -n InvoiceSystem.Domain
dotnet new classlib -f net8.0 -n InvoiceSystem.Application
dotnet new classlib -f net8.0 -n InvoiceSystem.Infrastructure

# Add projects to solution
echo "Adding projects to solution..."
dotnet sln add InvoiceSystem.Web/InvoiceSystem.Web.csproj
dotnet sln add InvoiceSystem.Domain/InvoiceSystem.Domain.csproj
dotnet sln add InvoiceSystem.Application/InvoiceSystem.Application.csproj
dotnet sln add InvoiceSystem.Infrastructure/InvoiceSystem.Infrastructure.csproj

# Add project references
echo "Adding project references..."
dotnet add InvoiceSystem.Application/InvoiceSystem.Application.csproj reference InvoiceSystem.Domain/InvoiceSystem.Domain.csproj
dotnet add InvoiceSystem.Infrastructure/InvoiceSystem.Infrastructure.csproj reference InvoiceSystem.Domain/InvoiceSystem.Domain.csproj
dotnet add InvoiceSystem.Infrastructure/InvoiceSystem.Infrastructure.csproj reference InvoiceSystem.Application/InvoiceSystem.Application.csproj
dotnet add InvoiceSystem.Web/InvoiceSystem.Web.csproj reference InvoiceSystem.Application/InvoiceSystem.Application.csproj
dotnet add InvoiceSystem.Web/InvoiceSystem.Web.csproj reference InvoiceSystem.Infrastructure/InvoiceSystem.Infrastructure.csproj

# Add NuGet packages
echo "Adding NuGet packages..."

# Domain project
# No external dependencies needed

# Application project
dotnet add InvoiceSystem.Application/InvoiceSystem.Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions

# Infrastructure project
dotnet add InvoiceSystem.Infrastructure/InvoiceSystem.Infrastructure.csproj package Microsoft.EntityFrameworkCore
dotnet add InvoiceSystem.Infrastructure/InvoiceSystem.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add InvoiceSystem.Infrastructure/InvoiceSystem.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design

# Web project
dotnet add InvoiceSystem.Web/InvoiceSystem.Web.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add InvoiceSystem.Web/InvoiceSystem.Web.csproj package Microsoft.EntityFrameworkCore.Tools

# Create directory structure
echo "Creating directory structure..."

# Domain Layer
mkdir -p InvoiceSystem.Domain/Entities
mkdir -p InvoiceSystem.Domain/Exceptions
mkdir -p InvoiceSystem.Domain/Services

# Application Layer
mkdir -p InvoiceSystem.Application/DTOs
mkdir -p InvoiceSystem.Application/Services
mkdir -p InvoiceSystem.Application/Interfaces

# Infrastructure Layer
mkdir -p InvoiceSystem.Infrastructure/Data
mkdir -p InvoiceSystem.Infrastructure/Identity
mkdir -p InvoiceSystem.Infrastructure/Repositories

# Web Layer
mkdir -p InvoiceSystem.Web/Controllers
mkdir -p InvoiceSystem.Web/ViewModels
mkdir -p InvoiceSystem.Web/Views/Account
mkdir -p InvoiceSystem.Web/Views/Invoice
mkdir -p InvoiceSystem.Web/Views/Shared

# Create empty files for domain layer
echo "Creating empty domain layer files..."
touch InvoiceSystem.Domain/Entities/Employee.cs
touch InvoiceSystem.Domain/Entities/Contract.cs
touch InvoiceSystem.Domain/Entities/Invoice.cs
touch InvoiceSystem.Domain/Entities/User.cs
touch InvoiceSystem.Domain/Exceptions/DomainException.cs
touch InvoiceSystem.Domain/Services/IInvoiceValidationService.cs
touch InvoiceSystem.Domain/Services/InvoiceValidationService.cs

# Create empty files for application layer
echo "Creating empty application layer files..."
touch InvoiceSystem.Application/Interfaces/IInvoiceSystemDbContext.cs
touch InvoiceSystem.Application/DTOs/EmployeeDto.cs
touch InvoiceSystem.Application/DTOs/ContractDto.cs
touch InvoiceSystem.Application/DTOs/InvoiceDto.cs
touch InvoiceSystem.Application/DTOs/CreateInvoiceDto.cs
touch InvoiceSystem.Application/Services/IEmployeeService.cs
touch InvoiceSystem.Application/Services/EmployeeService.cs
touch InvoiceSystem.Application/Services/IInvoiceService.cs
touch InvoiceSystem.Application/Services/InvoiceService.cs
touch InvoiceSystem.Application/Services/ValidationResult.cs
touch InvoiceSystem.Application/Services/ApplicationResult.cs

# Create empty files for infrastructure layer
echo "Creating empty infrastructure layer files..."
touch InvoiceSystem.Infrastructure/Data/InvoiceSystemDbContext.cs
touch InvoiceSystem.Infrastructure/Data/DbInitializer.cs
touch InvoiceSystem.Infrastructure/Identity/IAuthService.cs
touch InvoiceSystem.Infrastructure/Identity/AuthService.cs

# Create empty files for web layer
echo "Creating empty web layer files..."
touch InvoiceSystem.Web/Program.cs
touch InvoiceSystem.Web/appsettings.json
touch InvoiceSystem.Web/Controllers/AccountController.cs
touch InvoiceSystem.Web/Controllers/InvoiceController.cs
touch InvoiceSystem.Web/ViewModels/LoginViewModel.cs
touch InvoiceSystem.Web/ViewModels/CreateInvoiceViewModel.cs
touch InvoiceSystem.Web/Views/Account/Login.cshtml
touch InvoiceSystem.Web/Views/Invoice/Index.cshtml
touch InvoiceSystem.Web/Views/Invoice/Create.cshtml
touch InvoiceSystem.Web/Views/Invoice/Details.cshtml
touch InvoiceSystem.Web/Views/Shared/_Layout.cshtml
touch InvoiceSystem.Web/Views/Shared/_LoginPartial.cshtml
touch InvoiceSystem.Web/Views/Shared/_ValidationScriptsPartial.cshtml
touch InvoiceSystem.Web/Views/_ViewImports.cshtml
touch InvoiceSystem.Web/Views/_ViewStart.cshtml

# Create basic appsettings.json
echo "Creating basic configuration file..."
cat > InvoiceSystem.Web/appsettings.json << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=InvoiceSystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
EOF

# Create a basic ViewImports file for tag helpers
echo "Creating _ViewImports.cshtml..."
cat > InvoiceSystem.Web/Views/_ViewImports.cshtml << EOF
@using InvoiceSystem.Web
@using InvoiceSystem.Web.ViewModels
@using InvoiceSystem.Application.DTOs
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
EOF

# Create a basic ViewStart file
echo "Creating _ViewStart.cshtml..."
cat > InvoiceSystem.Web/Views/_ViewStart.cshtml << EOF
@{
    Layout = "_Layout";
}
EOF

echo "Project structure created successfully!"
echo ""
echo "Next steps:"
echo "1. Run 'dotnet build InvoiceSystem.sln' to check the setup"
echo "2. Implement the entity classes and business logic"
echo "3. Run 'dotnet run --project InvoiceSystem.Web' to start the application"
