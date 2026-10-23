using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartHelpdesk.API.Security.Authorization;
using SmartHelpdesk.BLL.Implementation;
using SmartHelpdesk.BLL.Interfaces;
using SmartHelpdesk.BLL.Services;
using SmartHelpdesk.Common.Security;
using SmartHelpdesk.DAL.Context;
using SmartHelpdesk.DAL.Entities;
using SmartHelpdesk.DAL.Implementation;
using SmartHelpdesk.DAL.Interfaces;
using SmartHelpdesk.DAL.Repository;
using SmartHelpdesk.DAL.Seed;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region Controller Configuration

builder.Services.AddControllers();

#endregion

#region Swagger Configuration

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
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

#endregion

#region Database Configuration

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

#endregion

#region Identity Configuration

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

#endregion

#region Dependency Injection


builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketHistoryRepository,TicketHistoryRepository>();
string aiApiKey = builder.Configuration["AiService:ApiKey"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(aiApiKey))
{
    throw new InvalidOperationException("AiService:ApiKey must be configured.");
}

builder.Services.AddHttpClient<IAiTicketService, AiTicketService>(
    (serviceProvider, client) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        var baseUrl = configuration["AiService:BaseUrl"] ?? "https://generativelanguage.googleapis.com/";

        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("x-goog-api-key", aiApiKey);
        client.Timeout = TimeSpan.FromSeconds(60);
    });


builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


#endregion


#region JWT Authentication

string jwtSecretKey =
    builder.Configuration["Jwt:SecretKey"] ?? string.Empty;

string jwtIssuer =
    builder.Configuration["Jwt:Issuer"] ?? string.Empty;

string jwtAudience =
    builder.Configuration["Jwt:Audience"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(jwtSecretKey) ||
    Encoding.UTF8.GetByteCount(jwtSecretKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:SecretKey must be configured with a secret of at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Issuer and Jwt:Audience must be configured.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultForbidScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

#endregion

#region Authorization Configuration

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministratorOnly",
        policy => policy.RequireRole(AppRoles.Administrator));

    options.AddPolicy("SupportAgentOrAdministrator",
        policy => policy.RequireRole(
            AppRoles.SupportAgent,
            AppRoles.Administrator));

    options.AddPolicy("AuthenticatedUser",
        policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddSingleton<
    IAuthorizationPolicyProvider,
    PermissionPolicyProvider>();

builder.Services.AddScoped<
    IAuthorizationHandler,
    PermissionAuthorizationHandler>();
builder.Services.AddScoped<
    IProfileImageService,
    ProfileImageService>();

#endregion



var app = builder.Build();

#region Database Seeding

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context =
        services.GetRequiredService<AppDbContext>();

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    await DatabaseSeeder.SeedAsync(context, roleManager);
}

#endregion

#region Middleware Configuration

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseCors("AngularClient");

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

#endregion

app.Run();