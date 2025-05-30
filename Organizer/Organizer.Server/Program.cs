using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Organizer.Server.Models;
using Organizer.Server.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB settings
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDB"));

// Configure JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Configure Email settings

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Configure Semantic Kernel
builder.Services.AddSingleton<SemanticKernelService>();


// Register IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Manually register UserService with injected jwtSecret string
builder.Services.AddSingleton<UserService>(sp =>
{
    var dbSettings = sp.GetRequiredService<IOptions<MongoDBSettings>>();
    var emailSettings = sp.GetRequiredService<IOptions<EmailSettings>>();
    var jwtSecret = builder.Configuration.GetValue<string>("JwtSettings:Secret")
                    ?? throw new InvalidOperationException("JWT Secret is not configured.");
    return new UserService(dbSettings, emailSettings, jwtSecret);
});


// Register other services
builder.Services.AddSingleton<TaskService>();

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(builder.Configuration["JwtSettings:Secret"] ?? "default_fallback_key"))
        };
    });

// Add authorization and controllers
builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();

// Middleware
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();



app.Run();