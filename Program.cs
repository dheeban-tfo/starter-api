using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using starterapi;
using starterapi.Filters;
using starterapi.Middleware;
using starterapi.Repositories;
using starterapi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure TenantManagementDbContext
builder.Services.AddDbContext<TenantManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TenantManagement"))
);


builder.Services.AddScoped<ITenantService, TenantService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

builder.Services.AddEndpointsApiExplorer();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add health checks
builder
    .Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddSqlServer(
        builder.Configuration.GetConnectionString("TenantManagement"),
        name: "database",
        failureStatus: HealthStatus.Degraded
    );

// Configure Hangfire
builder.Services.AddHangfire(configuration =>
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("HangfireConnection"),
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }
        )
);

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// Add API Explorer for versioning
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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
                new string[] { }
            }
        }
    );
});
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
// );

// // Register DbContextFactory
// builder.Services.AddDbContextFactory<TenantDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
// );

// // Register ApplicationDbContext
// builder.Services.AddScoped<TenantDbContext>(p =>
//     p.GetRequiredService<IDbContextFactory<TenantDbContext>>().CreateDbContext()
// );

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITenantDbContextAccessor, TenantDbContextAccessor>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure JWT Authentication
// Check JWT configuration
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (
    string.IsNullOrEmpty(jwtKey)
    || string.IsNullOrEmpty(jwtIssuer)
    || string.IsNullOrEmpty(jwtAudience)
)
{
    throw new InvalidOperationException(
        "JWT configuration is incomplete. Please check your appsettings.json file."
    );
}

builder
    .Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        x.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                logger.LogError(
                    "Authentication failed: {ExceptionMessage}",
                    context.Exception.Message
                );
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                logger.LogInformation("Token validated successfully");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                logger.LogWarning(
                    "OnChallenge error: {ErrorDescription}",
                    context.ErrorDescription
                );
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                logger.LogInformation("JWT token received: {Token}", context.Token);
                return Task.CompletedTask;
            }
        };

        // x.Events = new JwtBearerEvents
        // {
        //     OnTokenValidated = context =>
        //     {
        //         var logger = context.HttpContext.RequestServices.GetRequiredService<
        //             ILogger<Program>
        //         >();
        //         logger.LogInformation("JWT token validated successfully");

        //         // Preserve original claims
        //         var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
        //         if (claimsIdentity != null)
        //         {
        //             logger.LogInformation("Claims after token validation:");
        //             foreach (var claim in claimsIdentity.Claims)
        //             {
        //                 logger.LogInformation(
        //                     $"Claim Type: {claim.Type}, Claim Value: {claim.Value}"
        //                 );
        //             }
        //         }

        //         return Task.CompletedTask;
        //     },
        //     OnAuthenticationFailed = context =>
        //     {
        //         var logger = context.HttpContext.RequestServices.GetRequiredService<
        //             ILogger<Program>
        //         >();
        //         logger.LogError($"Authentication failed: {context.Exception.Message}");
        //         return Task.CompletedTask;
        //     }
        // };
    });

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "PermissionPolicy",
        policy => policy.Requirements.Add(new PermissionRequirement(null, null))
    );
});

builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<PermissionAuthorizationFilter>();
});

// Configure rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(
    builder.Configuration.GetSection("IpRateLimitPolicies")
);
builder.Services.AddInMemoryRateLimiting();

// Register dependencies for rate limiting
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(1)
            }
        )
    );

    options.RejectionStatusCode = 429;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

var app = builder.Build();

// Add health check endpoints
app.MapHealthChecks(
    "/health",
    new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse }
);

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }
);

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = (_) => false,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }
);

// Configure Hangfire dashboard
app.UseHangfireDashboard(
    "/hangfire",
    new DashboardOptions { Authorization = new[] { new HangfireAuthorizationFilter() } }
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    var apiVersionDescriptionProvider =
        app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant()
            );
        }
    });
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();



// Use rate limiting middleware
app.UseRateLimiter();

app.MapControllers();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var options = new SqlServerStorageOptions { PrepareSchemaIfNecessary = true };
        var storage = new SqlServerStorage(
            builder.Configuration.GetConnectionString("HangfireConnection"),
            options
        );
        var monitoringApi = storage.GetMonitoringApi();

        logger.LogInformation("Starting tenant seeding process...");
        TenantSeeder.SeedTenants(services);
        logger.LogInformation("Tenant seeding process completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

try
{
    Log.Information("Starting web application");
    // Log the URLs the application is running on
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var urls = app.Urls;
        app.Logger.LogInformation("Application is running on the following URLs:");
        foreach (var url in urls)
        {
            app.Logger.LogInformation(url);
        }
    });
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Hangfire dashboard authorization filter
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In development, allow all access to the Hangfire dashboard
        // In production, you should implement proper authorization here
        return true;
    }
}
