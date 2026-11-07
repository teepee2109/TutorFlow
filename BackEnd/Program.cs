using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Cấu hình Jwt:Key phải có ít nhất 32 byte.");
}

builder.Services.AddDbContext<BackEnd.Data.TutorFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCNN")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Nhập JWT nhận được từ POST /api/auth/login. Dán token, không cần thêm tiền tố Bearer.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddScoped<BackEnd.Services.Auth.IAuthService, BackEnd.Services.Auth.AuthService>();
builder.Services.AddScoped<BackEnd.Services.Auth.ITokenService, BackEnd.Services.Auth.JwtTokenService>();
builder.Services.AddScoped<BackEnd.Services.Users.IUserService, BackEnd.Services.Users.UserService>();
builder.Services.AddScoped<BackEnd.Services.Dashboard.IAdminDashboardService, BackEnd.Services.Dashboard.AdminDashboardService>();
builder.Services.AddScoped<BackEnd.Services.Classes.IClassService, BackEnd.Services.Classes.ClassService>();
builder.Services.AddScoped<BackEnd.Services.Classes.ISessionService, BackEnd.Services.Classes.SessionService>();
builder.Services.AddScoped<BackEnd.Services.Classes.IScheduleConflictChecker, BackEnd.Services.Classes.ScheduleConflictChecker>();
builder.Services.AddScoped<BackEnd.Services.Rooms.IRoomService, BackEnd.Services.Rooms.RoomService>();
builder.Services.AddScoped<BackEnd.Services.Rooms.IChangeRequestReviewService, BackEnd.Services.Rooms.ChangeRequestReviewService>();
builder.Services.AddScoped<BackEnd.Services.Lecturer.ILecturerScheduleService, BackEnd.Services.Lecturer.LecturerScheduleService>();
builder.Services.AddScoped<BackEnd.Services.Lecturer.IAttendanceService, BackEnd.Services.Lecturer.AttendanceService>();
builder.Services.AddScoped<BackEnd.Services.Lecturer.IChangeRequestService, BackEnd.Services.Lecturer.ChangeRequestService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BackEnd.Services.Common.ICurrentUser, BackEnd.Services.Common.CurrentUser>();

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<BackEnd.Middleware.ApiExceptionMiddleware>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
