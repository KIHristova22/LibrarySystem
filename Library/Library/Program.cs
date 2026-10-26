using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Library.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite("Data Source=library.db"));

builder.Services.AddDefaultIdentity<IdentityUser>(options => 
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LibraryContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LibraryContext>();
    
    context.Database.EnsureCreated();
    
    if (!context.Categories.Any())
    {
        context.Categories.Add(new Library.Models.Category { Name = "Учебници" });
        context.SaveChanges();
    }

    if (!context.Authors.Any())
    {
        context.Authors.Add(new Library.Models.Author { FirstName = "Иван", LastName = "Вазов" });
        context.SaveChanges();
    }

    if (!context.Books.Any())
    {
        var categoryId = context.Categories.First().CategoryId;
        var authorId = context.Authors.First().AuthorId;

        context.Books.AddRange(
            new Library.Models.Book { Title = "Под игото", Price = 15.50m, AuthorId = authorId, CategoryId = categoryId },
            new Library.Models.Book { Title = "ASP.NET Core Наръчник", Price = 28.00m, AuthorId = authorId, CategoryId = categoryId }
        );
        context.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization(); 

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapGet("/", async context =>
{
    context.Response.Redirect("/Books");
    await Task.CompletedTask;
});

app.Run();