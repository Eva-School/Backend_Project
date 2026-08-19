# Grade Management System API

A backend REST API for managing school grades, students, teachers, classes, subjects, and administrative approval workflows.

The project is built with **ASP.NET Core .NET 8** and follows a layered architecture to separate API, business logic, domain models, and data-access responsibilities.

## Features

* JWT-based authentication
* Refresh-token authentication flow
* ASP.NET Core Identity
* Role-based authorization
* Student management
* Teacher management
* Classes and subjects management
* Teacher-to-class/subject assignments
* Student dashboard functionality
* Teacher dashboard functionality
* Vice/admin grade-management workflows
* Quarter-grade management
* Final-grade approval and locking
* SQL Server database integration
* Entity Framework Core migrations
* DTO-based API communication
* AutoMapper object mapping
* Dependency Injection
* Swagger / OpenAPI documentation
* Automatic database migrations on application startup
* Optional development data seeding

## User Roles

The API supports different school-system roles and responsibilities, including:

* **Admin**
* **Vice**
* **Teacher**
* **Student**

Authorization is applied to protected endpoints based on the authenticated user's role.

## Tech Stack

### Backend

* C#
* .NET 8
* ASP.NET Core Web API

### Database

* Microsoft SQL Server
* Entity Framework Core 8
* EF Core Migrations

### Authentication & Security

* ASP.NET Core Identity
* JWT Bearer Authentication
* Refresh Tokens
* Role-Based Authorization

### Architecture & Development

* Layered Architecture
* Dependency Injection
* DTOs
* Service Layer
* Repository/Data Access Layer
* AutoMapper
* Async/Await

### API Documentation

* Swagger
* OpenAPI
* Swashbuckle

## Project Architecture

The solution is divided into four main projects:

```text
Backend_Project
│
├── GradeManagementSystem.Api
│   ├── Controllers
│   ├── Data
│   ├── Program.cs
│   └── appsettings.json
│
├── GradeManagementSystem.Core
│   ├── DTOs
│   ├── Entities
│   ├── Interfaces
│   └── Specifications
│
├── GradeManagementSystem.Repository
│   ├── Data
│   ├── Migrations
│   ├── Repositories
│   └── Specifications
│
├── GradeManagementSystem.Services
│   ├── Mapping
│   └── Services
│
└── GradeManagementSystem.Api.sln
```

### `GradeManagementSystem.Api`

The presentation/API layer.

Responsibilities include:

* HTTP endpoints
* Controllers
* Authentication configuration
* Authorization configuration
* Dependency Injection registration
* Swagger configuration
* Application startup

### `GradeManagementSystem.Core`

Contains the core application definitions.

Includes:

* Domain entities
* DTOs
* Interfaces
* Specifications
* Identity models

### `GradeManagementSystem.Repository`

Handles database and persistence responsibilities.

Includes:

* Entity Framework Core
* SQL Server integration
* Database context
* Migrations
* Repositories
* Data-access implementations

### `GradeManagementSystem.Services`

Contains the business logic of the application.

Includes:

* Authentication services
* Student services
* Teacher services
* Grade-management services
* Dashboard services
* AutoMapper profiles

## API Modules

The API contains controllers for functionality such as:

```text
Auth
Students
Teachers
Classes
Subjects
Teacher Assignments
Teacher Dashboard
Vice Dashboard
Vice Students
Quarter Grades
Final Grades
Admin Final Grade Approval
```

## Authentication

Authentication uses **JWT Bearer Tokens**.

Main authentication operations include:

```http
POST /api/auth/login
POST /api/auth/refresh
GET  /api/auth/me
POST /api/auth/logout
```

### Login Flow

```text
User Login
    ↓
Credentials Validation
    ↓
JWT Access Token
    +
Refresh Token
    ↓
Authenticated API Requests
```

Protected endpoints require a valid JWT token.

Example authorization header:

```http
Authorization: Bearer YOUR_ACCESS_TOKEN
```

## Role-Based Authorization

Some API endpoints are restricted to specific roles.

For example, final-grade approval is protected for administrators.

This allows the backend to enforce permissions independently of the frontend.

## Database

The project uses:

```text
Microsoft SQL Server
+
Entity Framework Core
```

The application executes pending EF Core migrations during startup.

## Prerequisites

Install the following before running the project:

* .NET 8 SDK
* SQL Server
* Visual Studio 2022, Rider, or VS Code
* Git

Optional:

* SQL Server Management Studio
* Postman

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Eva-School/Backend_Project.git
```

### 2. Enter the project directory

```bash
cd Backend_Project
```

### 3. Restore dependencies

```bash
dotnet restore
```

## Configuration

Do **not** commit passwords, JWT signing keys, database credentials, or SMTP credentials to GitHub.

For local development, configure values using environment variables, .NET User Secrets, or a local configuration file excluded from Git.

Example configuration structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_SQL_SERVER_CONNECTION_STRING"
  },

  "Jwt": {
    "Key": "YOUR_SECURE_JWT_SECRET",
    "Issuer": "GradeManagementSystem",
    "Audience": "GradeManagementSystemFrontend",
    "DurationInMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },

  "SmtpSettings": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "UserName": "YOUR_EMAIL",
    "Password": "YOUR_SMTP_PASSWORD",
    "From": "YOUR_EMAIL"
  }
}
```

## Using .NET User Secrets

From the API project:

```bash
cd GradeManagementSystem.Api
```

Initialize User Secrets:

```bash
dotnet user-secrets init
```

Example:

```bash
dotnet user-secrets set "Jwt:Key" "YOUR_SECURE_JWT_SECRET"
```

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
```

Secrets should never be pushed to a public repository.

## Run the Application

From the solution directory:

```bash
dotnet run --project GradeManagementSystem.Api
```

Or open:

```text
GradeManagementSystem.Api.sln
```

in Visual Studio and run the API project.

## Swagger

When running in the Development environment, Swagger is enabled.

Swagger can be used to:

* Explore API endpoints
* Inspect request and response models
* Test endpoints
* Authenticate using JWT Bearer tokens

Click **Authorize** in Swagger and provide:

```text
Bearer YOUR_TOKEN
```

## Database Migrations

The repository already contains EF Core migrations.

The application applies pending migrations when starting.

You can also manage migrations manually.

Example:

```bash
dotnet ef database update --project GradeManagementSystem.Repository --startup-project GradeManagementSystem.Api
```

## Development Seeding

The application contains seed logic for development/testing data.

Seeding can be disabled using the environment variable:

```text
RUN_SEED=false
```

## Example Backend Flow

```text
Client
   ↓
Controller
   ↓
Service Interface
   ↓
Service Implementation
   ↓
Repository / EF Core
   ↓
SQL Server
```

DTOs are used between API layers, while AutoMapper helps convert between DTOs and domain models.

## Key Backend Concepts Demonstrated

This project demonstrates practical knowledge of:

* REST API development
* ASP.NET Core
* C#
* Entity Framework Core
* SQL Server
* Authentication
* Authorization
* JWT
* Refresh Tokens
* ASP.NET Core Identity
* Dependency Injection
* Layered Architecture
* Repository/Data Access Layer
* Service Layer
* DTOs
* AutoMapper
* Async programming
* API documentation
* Database migrations
* Role-based access control

## Purpose

This project was developed as a practical backend system demonstrating the design and implementation of a multi-role school grade-management platform using modern .NET backend technologies.

It is also part of my portfolio as a **Junior .NET Backend Developer**.

## Developer

**Abdelrhman Yehia Masoud**

Junior .NET Backend Developer

Core technologies:

```text
C#
ASP.NET Core
Web API
Entity Framework Core
SQL Server
REST APIs
JWT Authentication
ASP.NET Core Identity
AutoMapper
Git
```

## Future Improvements

Planned improvements can include:

* Global exception-handling middleware
* Structured logging
* FluentValidation
* Unit tests
* Integration tests
* Docker support
* CI/CD pipeline
* API versioning
* Pagination/filtering improvements
* Production deployment
* Automated testing
* Enhanced security configuration

---

If you find this project useful, feel free to explore the code and architecture.
