using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var logDirectory = Path.Combine(
    builder.Environment.ContentRootPath,
    "Logs");

Directory.CreateDirectory(logDirectory);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(logDirectory, "app-log-.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14);
});

var projectRoot =
    @"C:/Users/krist/Documents/LibrarySystem/Library";

var dbPath = Path.Combine(projectRoot, "library.db");

builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LibraryContext>();

builder.Services.AddScoped<Library.Services.IBorrowRepository, Library.Services.BorrowRepository>();
builder.Services.AddScoped<Library.Services.IBookService, Library.Services.BookService>();

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles);

var app = builder.Build();

app.UseExceptionHandler("/Error/500");
app.UseStatusCodePagesWithReExecute("/Error/{0}");

var uploadsPhysicalPath = Path.Combine(
    projectRoot,
    "UploadedImages");

var profileUploadsPhysicalPath = Path.Combine(
    uploadsPhysicalPath,
    "Profiles");

Directory.CreateDirectory(uploadsPhysicalPath);
Directory.CreateDirectory(profileUploadsPhysicalPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        uploadsPhysicalPath),

    RequestPath = "/uploads"
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.MapControllerRoute(
    name: "errors",
    pattern: "Error/{statusCode:int}",
    defaults: new
    {
        controller = "Error",
        action = "Handle"
    });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=Index}/{id?}");

app.Run();