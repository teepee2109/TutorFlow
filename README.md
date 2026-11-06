# TutorFlow

TutorFlow uses two projects in one solution:

- `BackEnd` — ASP.NET Core Web API, SQL Server access, entities, DTOs and business services.
- `FrontEnd` — ASP.NET Core MVC pages that call the BackEnd over HTTP.

```text
TutorFlow/
├── BackEnd/
│   ├── Controllers/Api/
│   ├── Data/Entities/
│   ├── Dtos/
│   ├── Services/
│   ├── Program.cs
│   └── BackEnd.csproj
├── FrontEnd/
│   ├── Controllers/
│   ├── Models/
│   ├── Services/Api/
│   ├── Views/
│   ├── wwwroot/
│   ├── Program.cs
│   └── FrontEnd.csproj
├── TutorFlow.slnx
└── TutorFlow.sql
```

## Local configuration

Copy `BackEnd/appsettings.Development.json.example` to `BackEnd/appsettings.Development.json`. Set your SQL Server connection string and a private JWT signing key of at least 32 bytes in the copied file. Local Development settings are ignored by Git.

The FrontEnd API base URL is configured in `FrontEnd/appsettings.json`. Its default, `http://localhost:5228/`, matches the BackEnd HTTP launch profile. Update it if you use a different BackEnd port.

## Run

Start both projects from Visual Studio, or run each in a separate terminal:

```powershell
dotnet run --project .\BackEnd\BackEnd.csproj --launch-profile https
dotnet run --project .\FrontEnd\FrontEnd.csproj --launch-profile https
```

The BackEnd Swagger page is `https://localhost:7002/swagger`; the FrontEnd uses `https://localhost:7016` with the current launch settings.

Build the solution with:

```powershell
dotnet build .\TutorFlow.slnx --no-restore -m:1
```

`TutorFlow.sql` contains the SQL Server database schema and seed data.
