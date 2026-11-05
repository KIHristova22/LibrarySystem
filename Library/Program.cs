using Microsoft.EntityFrameworkCore;
using Library.Data;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Identity;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var projectRoot = @"C:/Users/krist/Documents/LibrarySystem/Library";
var dbPath = Path.Combine(projectRoot, "library.db");

builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LibraryContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

string uploadsPhysicalPath = @"C:/Users/krist/Documents/LibrarySystem/Library/UploadedImages";

if (!Directory.Exists(uploadsPhysicalPath))
{
    Directory.CreateDirectory(uploadsPhysicalPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPhysicalPath),
    RequestPath = "/uploads"
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=Index}/{id?}");

app.Run();