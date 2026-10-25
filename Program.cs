using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Data;
using PerceptionKitabataba.Models;

var builder = WebApplication.CreateBuilder(args);

// Base de données
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Outil pour crypter les mots de passe
builder.Services.AddScoped<IPasswordHasher<Utilisateur>, PasswordHasher<Utilisateur>>();

// Système de connexion par Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Création de la base et de l'utilisateur par défaut
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    if (!db.Utilisateurs.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Utilisateur>>();
        var admin = new Utilisateur
        {
            Login = "percepteur",
            NomComplet = "Percepteur de l'école",
            Role = "Percepteur",
            Actif = true
        };
        // Mot de passe par défaut : kitabataba2025
        admin.MotDePasseHash = hasher.HashPassword(admin, "kitabataba2025");
        db.Utilisateurs.Add(admin);
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication(); // IMPORTANT : Doit être avant UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();