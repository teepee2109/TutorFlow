# TutorFlow

Solution được tổ chức thành đúng hai project: `TutorFlow_BackEnd` và `TutorFlow_FrontEnd`. Các file C# có từ trước được giữ nguyên nội dung; Data, Services và Tests được đặt bên trong thư mục backend, còn giao diện MVC nằm trong frontend.

## Cấu trúc thư mục

```text
TutorFlow/
├── backend/
│   └── TutorFlow_BackEnd/                 # Project Web API
│       ├── BusinessObjects/
│       │   └── TutorFlow.Data/             # Entities, DbContext, keyless models
│       ├── Controllers/                    # API controllers
│       ├── Database/TutorFlow.sql
│       ├── Dtos/                           # Request/response DTOs của API
│       ├── Repositories/                   # Dành cho repository bổ sung sau
│       ├── Services/
│       │   └── TutorFlow.Services/         # Nghiệp vụ gốc
│       ├── Tests/TutorFlow.Tests/          # Unit tests gốc
│       ├── appsettings.json
│       ├── Program.cs
│       └── TutorFlow_BackEnd.csproj
├── frontend/
│   └── TutorFlow_FrontEnd/                 # Project ASP.NET Core MVC
│       ├── Controllers/
│       ├── Models/
│       ├── Services/
│       ├── Views/
│       ├── wwwroot/
│       └── TutorFlow_FrontEnd.csproj
├── TutorFlow.sln
└── global.json
```

Solution Explorer hiển thị hai project thật. `TutorFlow.Data` và `TutorFlow.Services` vẫn giữ project files của chúng, nhưng được tham chiếu bên trong backend và không còn là project thứ ba/cưỡng bốn trong solution. Unit tests vẫn nằm dưới backend.

## Chạy project

1. Tạo database bằng `backend\TutorFlow_BackEnd\Database\TutorFlow.sql` trên SQL Server.
2. Cấu hình `ConnectionStrings:DefaultConnection` trong `backend\TutorFlow_BackEnd\appsettings.json` và `frontend\TutorFlow_FrontEnd\appsettings.Development.json`.
3. Chạy backend và frontend trong hai terminal:

```powershell
dotnet run --project backend\TutorFlow_BackEnd\TutorFlow_BackEnd.csproj --launch-profile TutorFlow_BackEnd
dotnet run --project frontend\TutorFlow_FrontEnd\TutorFlow_FrontEnd.csproj --launch-profile https
```

Backend API dùng cổng `7100` (HTTP) hoặc `7101` (HTTPS). Frontend MVC dùng profile trong `Properties/launchSettings.json`.

## Tài khoản Development

Frontend MVC seeder tạo dữ liệu mẫu trong môi trường Development. Backend API không chạy seeder riêng để tránh hai tiến trình chèn cùng dữ liệu khi khởi động đồng thời. Mật khẩu mặc định: `Dev@12345`.

| Vai trò | Email |
|---|---|
| Admin | `admin@tutorflow.local` |
| Grand Manager | `gm@tutorflow.local` |
| Facility Manager | `fm1@tutorflow.local` |
| Lecturer | `lecturer1@tutorflow.local` |
| Student | `student1@tutorflow.local` |

## Entity Framework và tests

Scaffold lại DbContext khi schema thay đổi:

```powershell
dotnet ef dbcontext scaffold "Server=.\SQLEXPRESS;Database=TutorFlow;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --project backend\TutorFlow_BackEnd\BusinessObjects\TutorFlow.Data\TutorFlow.Data.csproj --startup-project frontend\TutorFlow_FrontEnd\TutorFlow_FrontEnd.csproj --context TutorFlowDbContext --output-dir Entities --no-onconfiguring --force
```

Chạy unit tests riêng:

```powershell
dotnet test backend\TutorFlow_BackEnd\Tests\TutorFlow.Tests\TutorFlow.Tests.csproj
```
