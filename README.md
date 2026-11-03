# TutorFlow — Hệ Thống Quản Lý Trung Tâm Dạy Thêm

> **Phiên bản**: 2.0 (PRN232 Final Project)  
> **Kiến trúc**: **ASP.NET Core 8.0 MVC (Model - View - Controller)**, Entity Framework Core 8.0.x, SQL Server.

---

## 1. Cấu Trúc Solution & Phân Chia MVC

```text
TutorFlow/
├── TutorFlow.sln
├── TutorFlow.sql                 # Script Database nguồn sự thật (v2.0)
├── TutorFlow_ERD.md              # Sơ đồ quan hệ thực thể (ERD Mermaid)
├── scripts/
│   ├── generate_erd.sql          # Query metadata DB sinh ERD
│   └── export_erd.ps1            # PowerShell tự động xuất ERD
├── TutorFlow.Data/               # EF Core DbContext, Entities, Keyless DTOs
│   ├── Entities/                 # Scaffolded entities + TutorFlowDbContext.Custom.cs
│   └── Models/Keyless/           # DTO cho các Table-Valued Functions (fn_RoomShiftStatus, ...)
├── TutorFlow.Services/           # Business Logic, Shared Contracts, Services
│   ├── Common/                   # Result<T>, ScheduleError, SqlExceptionMapper, ICurrentUser, DbSeeder
│   ├── Auth/                     # IAuthService, AuthService (BCrypt hash)
│   ├── Dashboard/                # IAdminDashboardService (vw_AdminStats)
│   └── Users/                    # IUserService (F1 CRUD, gán cơ sở, phân quyền)
├── TutorFlow.Web/                # ASP.NET Core 8 MVC Web Application
│   ├── Controllers/
│   │   ├── HomeController.cs     # Trang chủ & thông tin chung
│   │   ├── AccountController.cs  # Đăng nhập, Đăng xuất, AccessDenied
│   │   ├── AdminController.cs    # F10 Dashboard Admin
│   │   ├── UsersController.cs    # F1 Quản lý tài khoản & phân công cơ sở
│   │   ├── ClassesController.cs  # (Dev B phụ trách: Lớp học & Buổi học)
│   │   ├── RoomsController.cs    # (Dev C phụ trách: Lưới phòng & Xếp phòng)
│   │   ├── ScheduleController.cs # (Dev D phụ trách: Lịch dạy & Điểm danh)
│   │   ├── EnrollmentsController.cs # (Dev E phụ trách: Ghi danh học viên)
│   │   └── TuitionsController.cs # (Dev E phụ trách: Học phí & Thu tiền)
│   ├── Models/                   # ViewModels cho các màn hình MVC
│   ├── Views/
│   │   ├── Shared/_Layout.cshtml # Layout MVC chuẩn theo 5 vai trò
│   │   ├── Home/                 # View trang chủ
│   │   ├── Account/              # View đăng nhập & từ chối quyền
│   │   ├── Admin/                # View Dashboard
│   │   └── Users/                # Views danh sách, thêm, sửa, gán cơ sở
│   └── Services/                 # CurrentUser (đọc claims: Role, FacilityId)
└── TutorFlow.Tests/              # Unit & Integration Tests (xUnit, InMemory, SqlExceptionMapper)
```

---

## 2. Hướng Dẫn Cài Đặt Cho Dev Nhóm (Sau khi pull từ Git)

### Bước 1: Khởi tạo Database
Chạy script `TutorFlow.sql` trên SQL Server cục bộ (`.\SQLEXPRESS` hoặc `localhost`):

```powershell
sqlcmd -S ".\SQLEXPRESS" -I -i "TutorFlow.sql"
```

### Bước 2: Cấu hình chuỗi kết nối
File `TutorFlow.Web/appsettings.Development.json` đã cấu hình sẵn:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=TutorFlow;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```
*(Nếu dùng SQL instance khác, chỉ cần đổi `Server=...` tương ứng)*.

### Bước 3: Chạy ứng dụng và Test
```powershell
# Chạy bộ Unit Tests
dotnet test TutorFlow.sln

# Chạy ứng dụng Web MVC
dotnet run --project TutorFlow.Web
```

Truy cập: `https://localhost:5001` hoặc `http://localhost:5000` (hoặc port hiển thị trên console).

---

## 3. Tài Khoản Khởi Tạo Mặc Định (Môi trường Dev)

Tất cả tài khoản dev dùng chung mật khẩu: **`Dev@12345`** (đã băm bằng BCrypt).

| Vai trò | Email đăng nhập | Ghi chú |
|---|---|---|
| **Admin** | `admin@tutorflow.local` | Quản trị toàn hệ thống, gán FM cho cơ sở, xem Dashboard |
| **GrandManager** | `gm@tutorflow.local` | Quản lý lớp, tạo lịch học, ghi danh, phát hành học phí |
| **FacilityManager** | `fm1@tutorflow.local` | Quản lý **Cơ sở 1** (claim `FacilityId = 1`) |
| **FacilityManager** | `fm2@tutorflow.local` | Quản lý **Cơ sở 2** (claim `FacilityId = 2`) |
| **FacilityManager** | `fm3@tutorflow.local` | Quản lý **Cơ sở 3** (claim `FacilityId = 3`) |
| **Lecturer** | `lecturer1@tutorflow.local` | Giảng viên mẫu (Nguyễn Văn Toán) |
| **Student** | `student1@tutorflow.local` | Học viên mẫu (Học Viên An) |

---

## 4. Hợp Đồng Dùng Chung Cho 4 Dev Còn Lại (Dev B, C, D, E)

### 4.1 Quy ước Controller & Routing trong MVC
Mỗi Dev phụ trách một Controller và thư mục View tương ứng:
- **Dev B (Lớp học & Buổi học)**: Tạo `ClassesController.cs` và `Views/Classes/`
- **Dev C (Cơ sở & Phòng)**: Tạo `RoomsController.cs`, `ChangeRequestsController.cs`
- **Dev D (Giảng viên & Điểm danh)**: Tạo `ScheduleController.cs`, `AttendanceController.cs`
- **Dev E (Học viên & Học phí)**: Tạo `EnrollmentsController.cs`, `TuitionsController.cs`

### 4.2 Kết quả trả về: `Result` và `Result<T>`
Mọi hàm trong Service trả về `Result` hoặc `Result<T>`:

```csharp
// Thành công:
return Result<ClassDto>.Ok(dto, "Tạo lớp thành công.");
return Result.Ok("Điểm danh hoàn tất.");

// Thất bại (truyền ScheduleError):
return Result<ClassDto>.Fail(ScheduleError.RoomConflict, "Phòng đã có lớp khác cùng ca/ngày.");
```

### 4.3 Bộ dịch lỗi Database: `SqlExceptionMapper`
Bắt `DbUpdateException` hoặc `Exception` và gọi `SqlExceptionMapper.Map(ex)`:

```csharp
try 
{
    await _db.SaveChangesAsync();
    return Result.Ok();
}
catch (Exception ex)
{
    var mapped = SqlExceptionMapper.Map(ex);
    if (mapped.HasValue)
    {
        return Result.Fail(mapped.Value.Error, mapped.Value.Message);
    }
    throw;
}
```

### 4.4 Lấy thông tin người dùng đang đăng nhập: `ICurrentUser`
Inject `ICurrentUser` vào Controller hoặc Service:
- `CurrentUser.UserId`: ID người dùng đang đăng nhập
- `CurrentUser.Role`: Vai trò (`Admin`, `GrandManager`, `FacilityManager`, `Lecturer`, `Student`)
- `CurrentUser.FacilityId`: ID cơ sở phụ trách (dành cho Facility Manager)

---

## 5. Quy tắc Scaffold lại DbContext khi cập nhật `TutorFlow.sql`
> ⚠️ **LƯU Ý**: Không sửa tay các file trong `TutorFlow.Data/Entities/` (trừ file `TutorFlowDbContext.Custom.cs`).

Khi schema database thay đổi, chạy lệnh sau:
```powershell
dotnet ef dbcontext scaffold "Server=.\SQLEXPRESS;Database=TutorFlow;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --project TutorFlow.Data --startup-project TutorFlow.Web --context TutorFlowDbContext --output-dir Entities --no-onconfiguring --force
```
