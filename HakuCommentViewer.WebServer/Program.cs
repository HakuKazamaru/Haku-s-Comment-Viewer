using System.Diagnostics;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NLog.Web;

using HakuCommentViewer;
using HakuCommentViewer.Common;
using HakuCommentViewer.WebServer;
using HakuCommentViewer.WebServer.Hubs;
using HakuCommentViewer.WebServer.Queues;

/// <summary>
/// NLogロガー
/// </summary>
NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
/// <summary>
/// 設定管理オブジェクト
/// </summary>
Setting setting = new Setting();

// NLogの設定反映
NLog.LogManager.Configuration = setting.GetNLogSetting();

logger.Info("==============================  Start   ==============================");

string bindAddress = setting.GetAppsettingsToSectionStringValue("BindAddress").ToLower();
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
}).ConfigureLogging((hostContext, logging) => { })
.UseNLog();

if (!string.IsNullOrWhiteSpace(bindAddress))
{
    builder.WebHost.UseUrls(bindAddress);
}

int cookieTimeOut = (int)setting.GetAppsettingsToSectionIntValue(setting.AppConfig, "SessionTimeout");

// 分散キャッシュの指定(アプリのインスタンス内で有効)
builder.Services.AddDistributedMemoryCache();
// クッキーポリシーの設定
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

builder.Services.AddHttpContextAccessor();

// セッションサービスの追加
builder.Services.AddSession(opt =>
{
    // オプション指定
    opt.Cookie.Name = "Haku'sCommentViewer.Session";
    opt.IdleTimeout = TimeSpan.FromMinutes(cookieTimeOut);
    opt.Cookie.IsEssential = true;
    // opt.Cookie.MaxAge = TimeSpan.FromDays(7);
});

// DBContextの設定
builder.Services.AddDbContext<HcvDbContext>(options =>
{
    string dbType = setting.GetAppsettingsToSectionStringValue(setting.AppConfig, "DBType");
    // ログ出力設定
    options.EnableSensitiveDataLogging();

    // DB接続先指定
    switch (dbType)
    {
        case "sqlite":
            {
                logger.Info("PostgerSQLモードで実行します。");
                options.UseSqlite(setting.AppConfig.GetConnectionString("Context"), providerOptions =>
                {
                });
                break;
            }
        default:
            {
                logger.Warn("DBTypeに不正な値が指定されています。設定値：{0}", dbType);
                goto case "sqlite";
            }
    }
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.EnableDetailedErrors = true;
    hubOptions.MaximumReceiveMessageSize = 102400000;
});
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
       new[] { "application/octet-stream" });
});

builder.Services.AddHostedService<HakuCommentViewer.WebServer.Services.QueuedHostedService>();
builder.Services.AddSingleton<HakuCommentViewer.WebServer.Queues.IBackgroundTaskQueue>(ctx =>
{
    //if (!int.TryParse(hostContext.Configuration["QueueCapacity"], out var queueCapacity))
    int queueCapacity = 2048;   //キューに格納できるタスクの数の上限
    return new BackgroundTaskQueue(queueCapacity);
});

var app = builder.Build();
string useHttps = setting.GetAppsettingsToSectionStringValue("UseHttps").ToLower();

app.UseResponseCompression();

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Cache-Control", "no-cache");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // if (useHttps == "true") app.UseHsts();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseCookiePolicy();

// クライアントのIPアドレスを取得するため、NginxのProxyヘッダを使う設定
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpMethodOverride();

app.UseRouting();
app.UseCors();

app.UseAuthorization();

// セッションを使用
app.UseSession();

app.MapRazorPages();
app.MapControllers();
app.MapHub<CommentHub>(CommentHub.HubUrl);
app.MapFallbackToFile("index.html");

// DBマイグレーション
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HcvDbContext>();
    db.Database.Migrate();
}

app.Run();

logger.Info("==============================   End    ==============================");

