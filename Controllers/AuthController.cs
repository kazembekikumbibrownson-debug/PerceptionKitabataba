using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Data;
using PerceptionKitabataba.Models;

namespace PerceptionKitabataba.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<Utilisateur> _hasher;

        public AuthController(ApplicationDbContext db, IPasswordHasher<Utilisateur> hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        // GET: /Auth/Login
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Auth/Login
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string login, string motDePasse)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(motDePasse))
            {
                ViewBag.Erreur = "Entrez votre identifiant et votre mot de passe.";
                return View();
            }

            var utilisateur = await _db.Utilisateurs.FirstOrDefaultAsync(u => u.Login == login.Trim());

            if (utilisateur == null || !utilisateur.Actif)
            {
                ViewBag.Erreur = "Identifiant ou mot de passe incorrect.";
                return View();
            }

            var resultat = _hasher.VerifyHashedPassword(utilisateur, utilisateur.MotDePasseHash, motDePasse);

            if (resultat == PasswordVerificationResult.Failed)
            {
                ViewBag.Erreur = "Identifiant ou mot de passe incorrect.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, utilisateur.Id.ToString()),
                new Claim(ClaimTypes.Name, utilisateur.Login),
                new Claim("NomComplet", utilisateur.NomComplet)
            };

            var identite = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identite),
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Index", "Home");
        }

        // POST: /Auth/Logout
        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
       
        }
        // GET: /Auth/ChangePassword
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Auth/ChangePassword
        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string ancienMotDePasse, string nouveauMotDePasse, string confirmation)
        {
            if (string.IsNullOrWhiteSpace(ancienMotDePasse) || string.IsNullOrWhiteSpace(nouveauMotDePasse))
            {
                ViewBag.Erreur = "Tous les champs sont obligatoires.";
                return View();
            }

            if (nouveauMotDePasse.Length < 6)
            {
                ViewBag.Erreur = "Le nouveau mot de passe doit faire au moins 6 caractères.";
                return View();
            }

            if (nouveauMotDePasse != confirmation)
            {
                ViewBag.Erreur = "Les deux mots de passe ne correspondent pas.";
                return View();
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var utilisateur = await _db.Utilisateurs.FindAsync(userId);
            if (utilisateur == null) return NotFound();

            var verification = _hasher.VerifyHashedPassword(utilisateur, utilisateur.MotDePasseHash, ancienMotDePasse);
            if (verification == PasswordVerificationResult.Failed)
            {
                ViewBag.Erreur = "L'ancien mot de passe est incorrect.";
                return View();
            }

            utilisateur.MotDePasseHash = _hasher.HashPassword(utilisateur, nouveauMotDePasse);
            await _db.SaveChangesAsync();

            TempData["Succes"] = "Mot de passe modifié avec succès.";
            return RedirectToAction("Index", "Parametres");
        }
    }
}