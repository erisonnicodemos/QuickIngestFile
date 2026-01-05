# QuickIngestFile

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)](https://react.dev/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**High-performance, generic file ingestion system** that imports data from ANY file format into various databases with extreme speed. The application automatically detects the schema and structure of your files - no hardcoded entities needed.

---

## Purpose

**QuickIngestFile** was created to simplify and speed up the process of bulk data import. In many business and development scenarios, teams spend too much time creating custom solutions to import CSV or Excel files into databases.

### Problems It Solves

| Problem | QuickIngestFile Solution |
|---------|-------------------------|
| **Repetitive development** - Writing import code for each new file type | Generic system that accepts any data structure |
| **Slow performance** - Row-by-row imports that take hours for large volumes | Bulk inserts with batching, parallel processing via Channels |
| **Rigid schema** - Need to create entities/tables before importing | Automatic schema detection, flexible JSON storage |
| **Multiple formats** - Different code for CSV, Excel, etc. | Strategy Pattern allows pluggable parsers for any format |
| **Database lock-in** - Solution tied to a single DBMS | Multi-database architecture (SQL Server, MongoDB, extensible) |
| **Lack of visibility** - Not knowing import status/progress | Modern UI with real-time tracking and job history |
| **Complex dev environment** - Setting up database, app, dependencies | Docker Compose one-click setup |

### Use Cases

- **Data migration**: Import legacy data from spreadsheets to modern systems
- **Simple ETL**: Fast data ingestion for analysis pipelines
- **System integration**: Receive files from partners/vendors in various formats
- **Prototyping**: Quickly test data structures without creating schemas
- **Data Lake/Warehouse**: Feed data repositories from multiple sources
- **Backoffice operations**: Allow non-technical users to import data via friendly UI

### Why Use It?

1. **Zero schema configuration** - Drag the file and import
2. **Extreme speed** - 1 million records in ~5 seconds
3. **Extensibility** - Easily add new file formats or databases
4. **Production-ready** - Clean Architecture, Docker, logs, error handling
5. **Portfolio-quality** - Modern code showing .NET 8 and C# 12 best practices

---

## Features

- **Multi-format Support**: CSV, Excel (.xlsx, .xls), and extensible for more
- **Auto Schema Detection**: Automatically detects columns, data types, and structure
- **Multi-database**: SQL Server, MongoDB (extensible architecture for PostgreSQL, etc.)
- **Blazing Fast**: Sylvan CSV parser, batch bulk inserts, Channels-based streaming
- **Clean Architecture**: Domain-driven design, SOLID principles, Strategy Pattern
- **Docker Ready**: Full containerization with docker-compose
- **Modern UI**: React 18 + Vite + Twind for styling
- **Dynamic Data Storage**: JSON-based record storage for any data structure

## Architecture

```
+-------------------------------------------------------------+
|                      Frontend (React)                        |
|              Vite + Twind + Drag & Drop Upload              |
+-------------------------------------------------------------+
                              |
+-------------------------------------------------------------+
|                   API (Minimal API .NET 8)                   |
|              REST Endpoints + Swagger + CORS                |
+-------------------------------------------------------------+
                              |
+-------------------------------------------------------------+
|                      Application Layer                       |
|    File Parsers (Strategy) | Import Service (Channels)      |
|         DTOs | Factory | DataQueryService                    |
+-------------------------------------------------------------+
                              |
+-------------------------------------------------------------+
|                       Domain Layer                           |
|    ImportJob | ImportedRecord | FileSchema | Result<T>      |
|              Repository Interfaces | Unit of Work            |
+-------------------------------------------------------------+
                              |
+-------------------------------------------------------------+
|                    Infrastructure Layer                      |
|    SQL Server (EF Core + BulkExtensions) | MongoDB Driver   |
|         AppDbContext | Repository Implementations            |
+-------------------------------------------------------------+
```

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (optional)

### Running with Docker

```bash
# Start all services (API + SQL Server + MongoDB + Frontend)
docker-compose up -d

# Access the application
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
# Frontend: http://localhost:3000
```

### Running Locally

```bash
# Backend
cd src/QuickIngestFile.Api
dotnet run

# Frontend
cd frontend
npm install
npm run dev
```

## Performance

| Scenario | Records | Time |
|----------|---------|------|
| CSV Import | 100,000 | ~0.5s |
| Excel Import | 100,000 | ~2s |
| CSV Import | 1,000,000 | ~5s |

## Tech Stack

### Backend
- .NET 8 with C# 12
- Minimal API + Swagger
- Entity Framework Core 8 + BulkExtensions
- MongoDB.Driver 3.5
- Sylvan.Data.Csv (high-performance CSV parsing)
- ClosedXML (Excel parsing)
- Channels for producer/consumer pattern

### Frontend
- React 18
- Vite 5
- Twind (Tailwind-in-JS)
- Axios
- React Dropzone

### Infrastructure
- Docker & Docker Compose
- SQL Server 2022
- MongoDB 7

## Project Structure

```
QuickIngestFile/
 src/
    QuickIngestFile.Domain/         # Entities, Interfaces, Result Pattern
    QuickIngestFile.Application/    # Parsers, Services, DTOs
    QuickIngestFile.Infrastructure/ # EF Core, MongoDB implementations
    QuickIngestFile.Api/            # Minimal API endpoints
 frontend/                           # React + Vite + Twind
 docker-compose.yml
 README.md
```

## Configuration

### Database Provider

Configure in `appsettings.json`:

```json
{
  "Database": {
    "Provider": "SqlServer",
    "MongoDB": {
      "ConnectionString": "mongodb://localhost:27017",
      "DatabaseName": "quickingestfile"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=QuickIngestFile;Trusted_Connection=True;"
  }
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Made with .NET 8, React & Clean Architecture**
