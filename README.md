#  QuickIngestFile

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)](https://react.dev/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**High-performance, generic file ingestion system** that imports data from ANY file format into various databases with extreme speed. The application automatically detects the schema and structure of your files - no hardcoded entities needed.

---

##  Propósito

O **QuickIngestFile** nasceu da necessidade de simplificar e acelerar o processo de importação de dados em massa. Em muitos cenários corporativos e de desenvolvimento, equipes gastam tempo excessivo criando soluções customizadas para importar arquivos CSV ou Excel para bancos de dados.

###  Problemas que Resolve

| Problema | Solução QuickIngestFile |
|----------|------------------------|
| **Desenvolvimento repetitivo** - Criar código de importação para cada novo tipo de arquivo | Sistema genérico que aceita qualquer estrutura de dados |
| **Performance lenta** - Imports row-by-row que demoram horas em grandes volumes | Bulk inserts com batching, processamento paralelo via Channels |
| **Rigidez de schema** - Necessidade de criar entidades/tabelas antes de importar | Detecção automática de schema, armazenamento flexível em JSON |
| **Múltiplos formatos** - Código diferente para CSV, Excel, etc. | Strategy Pattern permite parsers plugáveis para qualquer formato |
| **Lock-in de banco** - Solução atrelada a um único SGBD | Arquitetura multi-database (SQL Server, MongoDB, extensível) |
| **Falta de visibilidade** - Não saber o status/progresso da importação | UI moderna com tracking em tempo real e histórico de jobs |
| **Ambiente de dev complexo** - Configurar banco, app, dependências | Docker Compose one-click setup |

###  Casos de Uso

- **Migração de dados**: Importar dados legados de planilhas para sistemas modernos
- **ETL simplificado**: Ingestão rápida de dados para pipelines de análise
- **Integração de sistemas**: Receber arquivos de parceiros/fornecedores em diversos formatos
- **Prototipagem**: Testar rapidamente estruturas de dados sem criar schemas
- **Data Lake/Warehouse**: Alimentar repositórios de dados com múltiplas fontes
- **Backoffice operations**: Permitir que usuários não-técnicos importem dados via UI amigável

###  Por que usar?

1. **Zero configuração de schema** - Arraste o arquivo e importe
2. **Velocidade extrema** - 1 milhão de registros em ~5 segundos
3. **Extensibilidade** - Adicione novos formatos de arquivo ou bancos de dados facilmente
4. **Production-ready** - Clean Architecture, Docker, logs, error handling
5. **Portfolio-quality** - Código moderno demonstrando boas práticas .NET 8 e C# 12

---

##  Features

-  **Multi-format Support**: CSV, Excel (.xlsx, .xls), e extensível para mais
-  **Auto Schema Detection**: Detecta automaticamente colunas, tipos de dados e estrutura
-  **Multi-database**: SQL Server, MongoDB (arquitetura extensível para PostgreSQL, etc.)
-  **Blazing Fast**: Sylvan CSV parser, batch bulk inserts, Channels-based streaming
-  **Clean Architecture**: Domain-driven design, princípios SOLID, Strategy Pattern
-  **Docker Ready**: Containerização completa com docker-compose
-  **Modern UI**: React 18 + Vite + Twind para estilização
-  **Dynamic Data Storage**: Armazenamento de registros baseado em JSON para qualquer estrutura

##  Architecture

```

                      Frontend (React)                        
              Vite + Twind + Drag & Drop Upload              

                              

                   API (Minimal API .NET 8)                   
              REST Endpoints + Swagger + CORS                

                              

                      Application Layer                       
    File Parsers (Strategy)  Import Service (Channels)      
         DTOs  Factory  DataQueryService                    

                              

                       Domain Layer                           
    ImportJob  ImportedRecord  FileSchema  Result<T>      
              Repository Interfaces  Unit of Work            

                              

                    Infrastructure Layer                      
    SQL Server (EF Core + BulkExtensions)  MongoDB Driver   
         AppDbContext  Repository Implementations            

```

##  Quick Start

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

##  Performance

| Scenario | Records | Time |
|----------|---------|------|
| CSV Import | 100,000 | ~0.5s |
| Excel Import | 100,000 | ~2s |
| CSV Import | 1,000,000 | ~5s |

##  Tech Stack

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

##  Project Structure

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

##  Configuration

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

##  License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Made with  using .NET 8, React & Clean Architecture**
