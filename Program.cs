using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using NETCore.MailKit.Extensions;
using NETCore.MailKit.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using common_morph_backend;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Database connection string
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddSingleton<IMyCacheService, MyCacheService>();

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseNpgsql(connectionString));

// builder.Services.AddDbContext<AppDbContext>(options =>
//   options.UseSqlServer(connectionString));

// builder.Services.AddDbContext<AppDbContext>(options =>
//   options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddDataProtection()
  .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Keys")))
  .SetApplicationName("CommonMorph")
  .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

builder.Services.AddMemoryCache();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(options =>
  {
    options.Cookie.Name = "CommonMorph";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/user/login";
    options.LogoutPath = "/user/logout";
    options.AccessDeniedPath = "/user/AccessDenied";
  });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();

// MailKit
builder.Services.AddMailKit(optionBuilder =>
{
  optionBuilder.UseMailKit(new MailKitOptions()
  {
    Server = Environment.GetEnvironmentVariable("MAILKIT_SMTP_SERVER") ?? builder.Configuration.GetValue<string>("MailKitOptions:Server"),
    Port = Convert.ToInt32(Environment.GetEnvironmentVariable("MAILKIT_SMTP_PORT") ?? builder.Configuration.GetValue<string>("MailKitOptions:Port")),
    SenderEmail = Environment.GetEnvironmentVariable("MAILKIT_SMTP_USER") ?? builder.Configuration.GetValue<string>("MailKitOptions:SenderEmail"),
    Account = Environment.GetEnvironmentVariable("MAILKIT_SMTP_USER") ?? builder.Configuration.GetValue<string>("MailKitOptions:Account"),
    Password = Environment.GetEnvironmentVariable("MAILKIT_SMTP_PASSWORD") ?? builder.Configuration.GetValue<string>("MailKitOptions:Password"),
    SenderName = Environment.GetEnvironmentVariable("MAILKIT_SMTP_USER") ?? builder.Configuration.GetValue<string>("MailKitOptions:SenderName"),
    Security = true
  });
});

builder.Services.Configure<JsonOptions>(options =>
{
  options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep PascalCase for responses
  options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // Accept camelCase in requests
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowAll");

// app.MapControllers();
app.MapControllerRoute(
    name: "User",
    pattern: "user/{action=login}",
    defaults: new { controller = "User" });

app.MapControllerRoute(
    name: "app",
    pattern: "app/{action=dashboard}",
    defaults: new { controller = "App" });

app.MapControllerRoute(
  name: "dataset",
  pattern: "dataset/{langid}",
  defaults: new { controller = "Home", action = "dataset" });

app.MapControllerRoute(
  name: "downlaod",
  pattern: "downlaod/{type}/{langid}",
  defaults: new { controller = "Home", action = "Download" });

app.MapControllerRoute(
  name: "default",
  pattern: "{action=Index}/{id?}",
  defaults: new { controller = "Home" });

app.Run();
app.Run();