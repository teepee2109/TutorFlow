using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Data.Entities;
using TutorFlow.Services.Auth;
using TutorFlow.Services.Common;
using TutorFlow.Services.Dashboard;
using TutorFlow.Services.Users;
using TutorFlow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.\\SQLEXPRESS;Database=TutorFlow;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddDbContext<TutorFlowDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireGrandManager", policy => policy.RequireRole("Admin", "GrandManager"));
    options.AddPolicy("RequireFacilityManager", policy => policy.RequireRole("Admin", "FacilityManager"));
    options.AddPolicy("RequireLecturer", policy => policy.RequireRole("Admin", "Lecturer"));
    options.AddPolicy("RequireStudent", policy => policy.RequireRole("Admin", "Student"));
});

// 3. DI Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddTransient<DbSeeder>();

// 4. ASP.NET Core MVC (Controllers with Views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 5. Seed dữ liệu môi trường Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

// 6. HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
