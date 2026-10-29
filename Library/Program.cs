using Microsoft.EntityFrameworkCore;
using Library.Data;
using Library.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite("Data Source=library.db"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    context.Database.EnsureCreated();

    if (!context.Authors.Any())
    {
        context.Authors.AddRange(
            new Author { FirstName = "George", LastName = "Orwell" },
            new Author { FirstName = "Jane", LastName = "Austen" }
        );
        context.SaveChanges();
    }

    if (!context.Categories.Any())
    {
        context.Categories.AddRange(
            new Category { Name = "Fiction" },
            new Category { Name = "Classics" }
        );
        context.SaveChanges();
    }
}

app.Run();