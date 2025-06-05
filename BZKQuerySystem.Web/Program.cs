using BZKQuerySystem.DataAccess;
using BZKQuerySystem.Services;
using BZKQuerySystem.Web.Filters;
using BZKQuerySystem.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text;
// 第一阶段优化：健康检查相关using
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// 设置文化信息为中文
var supportedCultures = new[] { "zh-CN", "zh-TW", "en-US" };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("zh-CN"),
    SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList(),
    SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList()
};

// 配置编码
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// 配置EPPlus许可证
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// 数据库连接字符串
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=ZBKQuerySystem;User Id=sa;Password=123;TrustServerCertificate=True;";

// 注册DbContext
builder.Services.AddDbContext<BZKQueryDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // 第一阶段优化：数据库连接池和重试策略优化
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: builder.Configuration.GetValue<int>("DatabaseConnection:MaxRetryCount", 3),
            maxRetryDelay: TimeSpan.Parse(builder.Configuration.GetValue<string>("DatabaseConnection:MaxRetryDelay", "00:00:10")),
            errorNumbersToAdd: null);

        sqlOptions.CommandTimeout(builder.Configuration.GetValue<int>("DatabaseConnection:CommandTimeout", 30));
    });

    // 第一阶段优化：开发环境启用详细错误信息
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(builder.Configuration.GetValue<bool>("DatabaseConnection:EnableSensitiveDataLogging", false));
        options.EnableDetailedErrors(builder.Configuration.GetValue<bool>("DatabaseConnection:EnableDetailedErrors", true));
    }

    // 第一阶段优化：性能监控
    options.LogTo(message =>
    {
        if (message.Contains("Executed DbCommand"))
        {
            var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
            logger?.LogDebug("EF Core Query: {Message}", message);
        }
    }, LogLevel.Debug);
});

// 第二阶段优化：注册HTTP上下文访问器以支持实时通信
builder.Services.AddHttpContextAccessor();

// 注册查询构建服务
builder.Services.AddScoped<QueryBuilderService>(provider =>
{
    var dbContext = provider.GetRequiredService<BZKQueryDbContext>();
    var userService = provider.GetService<UserService>();
    return new QueryBuilderService(dbContext, connectionString, userService);
});

builder.Services.AddScoped<ExcelExportService>();
// 第二阶段优化：注册PDF导出服务
builder.Services.AddScoped<PdfExportService>();

builder.Services.AddScoped<UserService>(provider =>
{
    var dbContext = provider.GetRequiredService<BZKQueryDbContext>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    return new UserService(dbContext, httpContextAccessor);
});

// 注册数据种子服务
builder.Services.AddScoped<DataSeederService>(provider =>
{
    var dbContext = provider.GetRequiredService<BZKQueryDbContext>();
    var logger = provider.GetRequiredService<ILogger<DataSeederService>>();
    return new DataSeederService(dbContext, connectionString, logger);
});

// 注册增强查询构建服务
builder.Services.AddScoped<EnhancedQueryBuilderService>(provider =>
{
    var dbContext = provider.GetRequiredService<BZKQueryDbContext>();
    var userService = provider.GetRequiredService<UserService>();
    var logger = provider.GetRequiredService<ILogger<EnhancedQueryBuilderService>>();
    var cache = provider.GetRequiredService<IMemoryCache>();
    return new EnhancedQueryBuilderService(dbContext, connectionString, userService, logger, cache);
});

builder.Services.AddScoped<IQueryBuilderService>(provider =>
    provider.GetRequiredService<EnhancedQueryBuilderService>());

builder.Services.AddScoped<PaginatedQueryService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<PaginatedQueryService>>();
    return new PaginatedQueryService(connectionString, logger);
});

// 注册缓存服务 - 第二层缓存支持Redis分布式模式
var cacheSettings = builder.Configuration.GetSection("CacheSettings");
var useRedis = cacheSettings.GetValue<bool>("UseRedis");
var useMemoryCache = cacheSettings.GetValue<bool>("UseMemoryCache");

// 注册缓存配置
builder.Services.Configure<CacheSettings>(cacheSettings);

// 如果使用内存缓存
if (useMemoryCache)
{
    builder.Services.AddMemoryCache(options =>
    {
        options.SizeLimit = 1000; // 最大缓存大小
        options.CompactionPercentage = 0.25; // 缓存压缩时保留25%的缓存
    });
}

// Redis分布式模式支持
if (useRedis)
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        // 注册Redis ConnectionMultiplexer - 用于监控服务检查连接状态
        builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
        {
            var configuration = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
            configuration.AbortOnConnectFail = false; // 启动时即使Redis不可用也不阻塞
            return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
        });

        // 注册Redis分布式缓存
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "BZKQuerySystem";
        });

        // 根据配置选择缓存服务实现
        if (useMemoryCache)
        {
            // 使用混合缓存：L1内存 + L2 Redis
            builder.Services.AddScoped<ICacheService, HybridCacheService>();
        }
        else
        {
            // 仅使用Redis缓存
            builder.Services.AddScoped<ICacheService, RedisCacheService>();
        }
    }
    else
    {
        // Redis连接字符串为空，则使用本地缓存
        builder.Services.AddScoped<ICacheService, CacheService>();
    }
}
else
{
    // 不使用Redis，仅使用本地缓存
    builder.Services.AddScoped<ICacheService, CacheService>();
}

builder.Services.AddScoped<QueryCacheService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// 注册实时通知服务
builder.Services.AddSignalR();
builder.Services.AddScoped<IRealTimeNotificationService, RealTimeNotificationService>();

// 注册查询性能服务
builder.Services.AddSingleton<IQueryPerformanceService, QueryPerformanceService>();

// 注册数据库优化服务
builder.Services.AddScoped<IDatabaseOptimizationService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<DatabaseOptimizationService>>();
    return new DatabaseOptimizationService(connectionString, logger);
});

// 注册代码质量检查服务
builder.Services.AddScoped<BZKQuerySystem.Services.Quality.CodeQualityChecker>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<BZKQuerySystem.Services.Quality.CodeQualityChecker>>();
    var environment = provider.GetRequiredService<IWebHostEnvironment>();
    return new BZKQuerySystem.Services.Quality.CodeQualityChecker(logger, environment.ContentRootPath);
});

// 注册并发管理服务配置
builder.Services.Configure<ConcurrencySettings>(options =>
{
    var perfSettings = builder.Configuration.GetSection("PerformanceSettings");
    options.MaxConcurrentQueries = perfSettings.GetValue<int>("MaxConcurrentQueries", 180);
    options.MaxConcurrentConnections = perfSettings.GetValue<int>("MaxConcurrentConnections", 200);
    options.ThreadPoolSize = perfSettings.GetValue<int>("ThreadPoolSize", 100);
    options.ConnectionPoolSize = perfSettings.GetValue<int>("ConnectionPoolSize", 50);
    options.QueryTimeout = TimeSpan.FromSeconds(perfSettings.GetValue<int>("QueryTimeout", 30));
    options.EnableThrottling = true;
});

// 注册并发管理服务
builder.Services.AddSingleton<IConcurrencyManagerService, ConcurrencyManagerService>();

// 注册监控服务配置
builder.Services.Configure<MonitoringSettings>(
    builder.Configuration.GetSection("MonitoringSettings"));

// 注册高级监控服务 - 使用单例注册，但通过IServiceProvider获取作用域服务
builder.Services.AddSingleton<IAdvancedMonitoringService, AdvancedMonitoringService>();
builder.Services.AddHostedService<AdvancedMonitoringService>(provider =>
    provider.GetRequiredService<IAdvancedMonitoringService>() as AdvancedMonitoringService);

// 注册高级可视化服务
builder.Services.AddScoped<IAdvancedVisualizationService, AdvancedVisualizationService>();

// 配置线程池设置
ThreadPool.SetMinThreads(
    builder.Configuration.GetValue<int>("PerformanceSettings:ThreadPoolSize", 100),
    builder.Configuration.GetValue<int>("PerformanceSettings:ThreadPoolSize", 100));

// 注册速率限制器
builder.Services.AddRateLimiter(options =>
{
    // 限制查询次数，每分钟最多30次
    options.AddFixedWindowLimiter("QueryLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // 限制导出次数，每小时最多导出10次
    options.AddFixedWindowLimiter("ExportLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromHours(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    // 全局限制器
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// 注册身份验证
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);

        // 启用HttpOnly cookie
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    // 允许执行查询
    options.AddPolicy("ExecuteQueries", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("SaveQueries", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("DeleteQueries", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("ShareQueries", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("ExportData", policy =>
        policy.RequireAuthenticatedUser());

    // 允许保存查询
    options.AddPolicy("CanSaveQueries", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("CanShareQueries", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("CanExportData", policy =>
        policy.RequireAuthenticatedUser());

    // 系统管理员策略
    options.AddPolicy("SystemAdmin", policy =>
        policy.RequireRole("Admin", "SystemAdmin"));

    options.AddPolicy("ViewPerformanceMetrics", policy =>
        policy.RequireRole("Admin", "SystemAdmin", "PowerUser"));

    // 管理表策略
    options.AddPolicy("ManageTables", policy =>
        policy.RequireClaim("Permission", "ManageTables", "SystemAdmin"));

    // 管理用户策略
    options.AddPolicy("ManageUsers", policy =>
        policy.RequireClaim("Permission", "ManageUsers", "SystemAdmin"));
});

// 添加全局异常过滤器
builder.Services.AddControllersWithViews(options =>
{
    // 启用全局异常处理
    options.Filters.Add<GlobalExceptionFilter>();
});

// 注册健康检查
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BZKQueryDbContext>("database", tags: new[] { "ready", "db" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "ready" });

// 注册Redis健康检查
var enableRedisCheck = builder.Configuration.GetValue<bool>("HealthChecks:EnableRedisCheck");
if (useRedis && enableRedisCheck && !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("Redis")))
{
    builder.Services.AddHealthChecks()
        .AddRedis(builder.Configuration.GetConnectionString("Redis"), "redis", tags: new[] { "ready", "cache" });
}

// 注册日志配置
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
    if (builder.Environment.IsProduction())
    {
        config.AddEventLog();
    }
});

var app = builder.Build();

// 启用开发异常页面
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 启用全局异常处理中间件
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

// 启用静态文件缓存
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value;

        // 缓存CSS、JS、字体文件
        if (path.Contains("/lib/") || path.Contains("/css/") || path.Contains("/js/") ||
            path.Contains("/fonts/") || path.Contains(".woff") || path.Contains(".woff2") ||
            path.Contains(".ttf") || path.Contains(".eot") || path.Contains(".svg") ||
            path.Contains(".png") || path.Contains(".jpg") || path.Contains(".jpeg") ||
            path.Contains(".gif") || path.Contains(".ico"))
        {
            const int durationInSeconds = 60 * 60 * 24 * 30; // 缓存30天
            ctx.Context.Response.Headers["Cache-Control"] = $"public,max-age={durationInSeconds}";
            ctx.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddDays(30).ToString("R");

            // 启用ETag
            if (ctx.File.Exists)
            {
                var etag = $"\"{ctx.File.LastModified.ToFileTime():X}-{ctx.File.Length:X}\"";
                ctx.Context.Response.Headers["ETag"] = etag;
            }
        }

        // 缓存HTML页面
        else if (path.EndsWith(".html") || path == "/" || !path.Contains("."))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=300"; // 缓存5分钟
        }
    }
});

app.UseRouting();

// 启用本地化
app.UseRequestLocalization(localizationOptions);

// 启用速率限制器
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// 注册健康检查
app.MapHealthChecks("/health");

// 注册数据库健康检查
app.MapHealthChecks("/health/ready", new HealthCheckOptions()
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                exception = entry.Value.Exception?.Message,
                duration = entry.Value.Duration.ToString()
            })
        });
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/live", new HealthCheckOptions()
{
    Predicate = _ => false
});

// 注册默认路由
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

// 注册API
app.MapControllers();

// 注册SignalR Hub
app.MapHub<QueryNotificationHub>("/hubs/queryNotification");

// 初始化数据
using (var scope = app.Services.CreateScope())
{
    var dataSeederService = scope.ServiceProvider.GetRequiredService<DataSeederService>();
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("初始化数据库...");

        // 检查数据库连接
        bool connectionSuccess = await dataSeederService.CheckDatabaseConnectionAsync();
        if (!connectionSuccess)
        {
            logger.LogError("数据库连接失败，请检查以下几点：");
            logger.LogError("1. SQL Server服务是否启动？");
            logger.LogError("2. 数据库连接字符串是否正确？");
            logger.LogError("3. SQL Server是否允许远程连接？");
            logger.LogError("4. SQL Server端口是否正确？");

            // 等待用户解决问题
            logger.LogInformation("等待用户解决问题...");
        }
        else
        {
            logger.LogInformation("数据库连接成功！");
        }

        // 创建数据库
        bool dbCreated = await dataSeederService.CreateDatabaseIfNotExistsAsync();
        if (dbCreated)
        {
            logger.LogInformation("数据库创建成功！");
        }

        // 初始化admin用户
        var adminUser = await userService.ValidateUserAsync("admin", "123");
        if (adminUser == null)
        {
            logger.LogInformation("Admin用户不存在，正在初始化...");

            // 使用DataSeederService初始化测试数据
            await dataSeederService.InitializeTestDataAsync();

            logger.LogInformation("Admin用户初始化完成！");
        }
        else
        {
            logger.LogInformation("Admin用户已存在，用户ID: {UserId}", adminUser.Id);
        }

        // 刷新数据库架构
        var queryBuilderService = scope.ServiceProvider.GetRequiredService<QueryBuilderService>();
        await queryBuilderService.RefreshDatabaseSchemaAsync();

        logger.LogInformation("数据库初始化完成！");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "数据库初始化失败：{ErrorMessage}", ex.Message);
        app.Logger.LogError("提示：{Advice}", ex.StackTrace);

        // 提供解决方案
        app.Logger.LogError("解决方案：");
        app.Logger.LogError("1. 检查SQL Server服务是否启动？");
        app.Logger.LogError("2. 检查数据库连接字符串是否正确？");
        app.Logger.LogError("3. 检查SQL Server是否允许远程连接？");
        app.Logger.LogError("4. 检查SQL Server端口是否正确？");
        app.Logger.LogError("5. 使用SQL Server Management Studio连接SQL Server");

        // 警告：不要直接在生产环境中执行此操作
        app.Logger.LogWarning("警告：不要直接在生产环境中执行此操作！");
    }
}

app.Run();
