using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var projectRoot = @"C:/Users/krist/Documents/LibrarySystem/Library";
var dbPath = Path.Combine(projectRoot, "library.db");

builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LibraryContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

var uploadsPhysicalPath = Path.Combine(projectRoot, "UploadedImages");
var profileUploadsPhysicalPath = Path.Combine(uploadsPhysicalPath, "Profiles");

Directory.CreateDirectory(uploadsPhysicalPath);
Directory.CreateDirectory(profileUploadsPhysicalPath);

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