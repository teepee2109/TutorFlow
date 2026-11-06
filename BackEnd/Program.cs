using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Cấu hình Jwt:Key phải có ít nhất 32 byte.");
}

builder.Services.AddDbContext<BackEnd.Data.TutorFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TutorFlow.Services.Auth.IAuthService, TutorFlow.Services.Auth.AuthService>();
builder.Services.AddScoped<TutorFlow.Services.Auth.ITokenService, TutorFlow.Services.Auth.JwtTokenService>();
builder.Services.AddScoped<TutorFlow.Services.Users.IUserService, TutorFlow.Services.Users.UserService>();
builder.Services.AddScoped<TutorFlow.Services.Dashboard.IAdminDashboardService, TutorFlow.Services.Dashboard.AdminDashboardService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TutorFlow.Services.Common.ICurrentUser, TutorFlow.Services.Common.CurrentUser>();

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
