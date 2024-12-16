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
using StarterApi.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using StarterApi.Helpers;
using Hangfire.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(typeof(Program));

// Add services to the container.
builder.Services.AddControllers();

//builder.Services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();

builder.Services.AddScoped<TenantDbSchemaUpdater>();

// Configure TenantManagementDbContext
builder.Services.AddDbContext<TenantManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TenantManagement"))
);

builder.Services.AddScoped<ITenantService, TenantService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
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
                QueuePollInterval = TimeSpan.FromMinutes(1),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                PrepareSchemaIfNecessary = true
            }
        )
);

// Add a single Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Math.Max(Environment.ProcessorCount * 2, 20); // Set minimum of 20 workers
    options.Queues = new[] { "default" };
    options.ServerName = $"{Environment.MachineName}:{Environment.ProcessId}";
    options.ServerTimeout = TimeSpan.FromMinutes(5);
    options.ShutdownTimeout = TimeSpan.FromMinutes(5);
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

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

// builder.Services.AddSwaggerGen(c =>
// {
//     c.AddSecurityDefinition(
//         "Bearer",
//         new OpenApiSecurityScheme
//         {
//             Description = "JWT Authorization header using the Bearer scheme.",
//             Name = "Authorization",
//             In = ParameterLocation.Header,
//             Type = SecuritySchemeType.ApiKey,
//             Scheme = "Bearer"
//         }
//     );

//     c.AddSecurityRequirement(
//         new OpenApiSecurityRequirement
//         {
//             {
//                 new OpenApiSecurityScheme
//                 {
//                     Reference = new OpenApiReference
//                     {
//                         Type = ReferenceType.SecurityScheme,
//                         Id = "Bearer"
//                     }
//                 },
//                 new string[] { }
//             }
//         }
//     );
// });
// builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

builder.Services.AddDbContextFactory<TenantDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


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

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<ICommunityRepository, CommunityRepository>();

builder.Services.AddScoped<ITenantDbContextAccessor, TenantDbContextAccessor>();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IMigrationService, MigrationService>();

builder.Services.AddScoped<IBlockRepository, BlockRepository>();
builder.Services.AddScoped<IFloorRepository, FloorRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();

// ... existing code ...

builder.Services.AddScoped<IFacilityRepository, FacilityRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileRepository, FileRepository>();

// ... rest of your configuration ...

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

//var app = builder.Build();

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
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                //logger.LogInformation("Authorization Header: {AuthHeader}", authHeader);

                if (!string.IsNullOrEmpty(authHeader))
                {
                    // If the Authorization header doesn't start with 'Bearer ', add it
                    if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        authHeader = "Bearer " + authHeader;
                    }
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    context.Token = token;
                    //logger.LogInformation("JWT token extracted: {Token}", token);
                }
                else
                {
                    logger.LogWarning("No Authorization header found");
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                logger.LogInformation("JWT token validated successfully");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                logger.LogError($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                logger.LogWarning("OnChallenge error: {Error}", context.Error);
                return Task.CompletedTask;
            }
        };
    });

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "PermissionPolicy",
        policy => policy.Requirements.Add(new PermissionRequirement())
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

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

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

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = null;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

// Configure UserContext
UserContext.Configure(app.Services.GetRequiredService<IHttpContextAccessor>());

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

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
//app.UseMiddleware<HeaderLoggingMiddleware>(); enable to debug headers
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

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
        options.OAuthUsePkce();
        options.ConfigObject.AdditionalItems["persistAuthorization"] = "true";
    });
}



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

    // Add proper shutdown handling
    app.Lifetime.ApplicationStopping.Register(() =>
    {
        Log.Information("Application is shutting down...");
        // Ensure Hangfire jobs are completed or properly stopped
        var hangfireStorage = app.Services.GetService<JobStorage>();
        if (hangfireStorage != null)
        {
            try
            {
                // Get all running jobs and try to gracefully stop them
                using var connection = hangfireStorage.GetConnection();
                var runningJobs = connection.GetRecurringJobs();
                foreach (var job in runningJobs)
                {
                    BackgroundJob.Delete(job.Id);
                }
                Log.Information("Hangfire jobs stopped successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error stopping Hangfire jobs");
            }
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
