using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Library.Data;
using Library.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<LibraryContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LibraryContext>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();

    string[] roleNames = { "Admin", "User" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var adminEmail = "admin@library.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(adminUser, "Admin123!");
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    if (!context.Authors.Any())
    {
        context.Authors.AddRange(
            new Author { FirstName = "Иван", LastName = "Вазов" },
            new Author { FirstName = "Пейо", LastName = "Яворов" },
            new Author { FirstName = "Елин", LastName = "Пелин" },
            new Author { FirstName = "Йордан", LastName = "Йовков" },
            new Author { FirstName = "Христо", LastName = "Смирненски" },
            new Author { FirstName = "Никола", LastName = "Вапцаров" },
            new Author { FirstName = "Димитър", LastName = "Димов" },
            new Author { FirstName = "Димитър", LastName = "Талев" }
        );
        context.SaveChanges();
    }

    if (!context.Categories.Any())
    {
        context.Categories.AddRange(
            new Category { Name = "Роман" },
            new Category { Name = "Поезия" },
            new Category { Name = "Разказ" }
        );
        context.SaveChanges();
    }

    if (!context.Books.Any())
    {
        var vazo = context.Authors.First(a => a.LastName == "Вазов");
        var yavorov = context.Authors.First(a => a.LastName == "Яворов");
        var elinPelin = context.Authors.First(a => a.LastName == "Пелин");
        var jovkov = context.Authors.First(a => a.LastName == "Йовков");
        var smirnenski = context.Authors.First(a => a.LastName == "Смирненски");
        var vaptsarov = context.Authors.First(a => a.LastName == "Вапцаров");
        var dimov = context.Authors.First(a => a.LastName == "Димов");
        var talev = context.Authors.First(a => a.LastName == "Талев");

        var roman = context.Categories.First(c => c.Name == "Роман");
        var poeziya = context.Categories.First(c => c.Name == "Поезия");
        var razkaz = context.Categories.First(c => c.Name == "Разказ");

        context.Books.AddRange(
            new Book { Title = "Под игото", Price = 15.00m, AuthorId = vazo.AuthorId, CategoryId = roman.CategoryId },
            new Book { Title = "Опълченците на Шипка", Price = 10.00m, AuthorId = vazo.AuthorId, CategoryId = poeziya.CategoryId },
            new Book { Title = "Градушка", Price = 12.00m, AuthorId = yavorov.AuthorId, CategoryId = poeziya.CategoryId },
            new Book { Title = "Гераците", Price = 14.00m, AuthorId = elinPelin.AuthorId, CategoryId = razkaz.CategoryId },
            new Book { Title = "Индже", Price = 13.00m, AuthorId = jovkov.AuthorId, CategoryId = razkaz.CategoryId },
            new Book { Title = "Да бъде ден!", Price = 11.00m, AuthorId = smirnenski.AuthorId, CategoryId = poeziya.CategoryId },
            new Book { Title = "Вяра", Price = 9.50m, AuthorId = vaptsarov.AuthorId, CategoryId = poeziya.CategoryId },
            new Book { Title = "Тютюн", Price = 18.00m, AuthorId = dimov.AuthorId, CategoryId = roman.CategoryId },
            new Book { Title = "Железният светилник", Price = 17.00m, AuthorId = talev.AuthorId, CategoryId = roman.CategoryId }
        );
        context.SaveChanges();
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=Index}/{id?}");

app.Run();