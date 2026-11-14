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

## Học phí và thanh toán

- Kỳ mặc định là tháng vừa kết thúc. Chỉ phát hành sau ngày kết thúc kỳ và khi các buổi học trong kỳ đã hoàn tất điểm danh hoặc được hủy.
- Mỗi buổi `Present` hoặc `UnexcusedAbsent` tính một buổi phí; `ExcusedAbsent` và buổi bị hủy không tính.
- Khi phát hành, đơn giá và số buổi được chốt thành khoản học phí. Các khoản đã phát hành và lịch sử thu không tự tính lại nếu cấu hình đơn giá thay đổi.
- Học viên có thể thanh toán nhiều lần. Trạng thái là chưa thanh toán, thanh toán một phần, đã thanh toán hoặc quá hạn; hạn thanh toán là 7 ngày sau ngày phát hành.
- Với database hiện có, chạy `upgrade_tuition_flow.sql` một lần trên database `TutorFlow` để nâng cấp cột và hàm tính học phí, giữ nguyên các khoản đã phát hành và giao dịch. Database mới thì dùng `TutorFlow.sql`.
- Học viên gửi xác nhận thanh toán cho một khoản học phí đã phát hành. Yêu cầu được giữ ở trạng thái chờ và tạm giữ phần số dư tương ứng; chỉ Grand Manager mới được duyệt hoặc từ chối. Khi duyệt, hệ thống mới tạo dòng `Payments` và cập nhật số dư; khi từ chối, cần nhập lý do và số tiền được giải phóng để gửi lại.
- Với database đã cài trước luồng xác nhận thanh toán, chạy lại `upgrade_tuition_flow.sql` để tạo bảng `PaymentRequests`. Script có kiểm tra bảng nên có thể chạy an toàn nhiều lần.
