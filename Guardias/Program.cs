using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Guardias.Data;
using Guardias.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<CloudinaryService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Apply pending migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await AppDbContext.SeedAsync(db);
}

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

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}/{id?}",
    defaults: new { controller = "Account" });

// Ruta raíz → Historial (requiere auth)
app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Ronda", action = "Historial" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Ronda}/{action=Index}/{id?}");

app.Run();
