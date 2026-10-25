using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Data;
using PerceptionKitabataba.Models;

namespace PerceptionKitabataba.Controllers
{
    [Authorize]
    public class ParametresController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ParametresController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: /Parametres
        public async Task<IActionResult> Index()
        {
            var parametres = await _db.Parametres.FirstOrDefaultAsync();
            if (parametres == null)
            {
                parametres = new Parametre();
                _db.Parametres.Add(parametres);
                await _db.SaveChangesAsync();
            }
            return View(parametres);
        }

        // POST: /Parametres
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Parametre formulaire)
        {
            if (!ModelState.IsValid) return View(formulaire);

            var parametres = await _db.Parametres.FindAsync(formulaire.Id);
            if (parametres == null)
            {
                _db.Parametres.Add(formulaire);
            }
            else
            {
                parametres.NomEcole = formulaire.NomEcole;
                parametres.AnneeScolaire = formulaire.AnneeScolaire;
                parametres.NomPercepteur = formulaire.NomPercepteur;
                parametres.TauxUSDversCDF = formulaire.TauxUSDversCDF;
                parametres.MontantInscription = formulaire.MontantInscription;
                parametres.MontantMinervalMensuel = formulaire.MontantMinervalMensuel;
                parametres.AnneeDebutScolaire = formulaire.AnneeDebutScolaire;
            }
            await _db.SaveChangesAsync();
            TempData["Succes"] = "Paramètres enregistrés avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Parametres/GenererAnnee
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GenererAnnee()
        {
            var p = await _db.Parametres.FirstOrDefaultAsync();
            if (p == null)
            {
                TempData["Erreur"] = "Veuillez d'abord enregistrer les paramètres de base.";
                return RedirectToAction(nameof(Index));
            }

            int annee = p.AnneeDebutScolaire;
            int ajoutes = 0;

            // 1) Génération du frais d'inscription
            bool inscriptionExiste = await _db.Frais.AnyAsync(f => f.Type == TypeFrais.Inscription && f.AnneeCible == annee);
            if (!inscriptionExiste)
            {
                _db.Frais.Add(new Frais
                {
                    Libelle = $"Inscription {annee}-{annee + 1}",
                    Montant = p.MontantInscription,
                    Devise = "CDF",
                    Periode = "Une seule fois",
                    CiblesNiveaux = "TOUTES",
                    Type = TypeFrais.Inscription,
                    AnneeCible = annee,
                    Ordre = 0
                });
                ajoutes++;
            }

            // 2) Génération des 9 mois de Minerval (Octobre -> Juin)
            var mois = new[] { 10, 11, 12, 1, 2, 3, 4, 5, 6 };
            var nomsMois = new[] { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };

            for (int i = 0; i < mois.Length; i++)
            {
                int m = mois[i];
                bool existe = await _db.Frais.AnyAsync(f => f.Type == TypeFrais.Mensuel && f.AnneeCible == annee && f.Mois == m);
                if (existe) continue;

                string anneeReelle = m >= 9 ? $"{annee}" : $"{annee + 1}";

                _db.Frais.Add(new Frais
                {
                    Libelle = $"Minerval {nomsMois[m]} {anneeReelle}",
                    Montant = p.MontantMinervalMensuel,
                    Devise = "CDF",
                    Periode = $"Mensuel {annee}-{annee + 1}",
                    CiblesNiveaux = "TOUTES",
                    Type = TypeFrais.Mensuel,
                    Mois = m,
                    AnneeCible = annee,
                    Ordre = 100 + m
                });
                ajoutes++;
            }

            await _db.SaveChangesAsync();
            TempData["Succes"] = ajoutes > 0
                ? $"{ajoutes} frais générés avec succès pour l'année {annee}-{annee + 1}."
                : "Les frais de cette année existent déjà.";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Parametres/TelechargerSauvegarde
        // C'est LA méthode corrigée pour éviter l'erreur de verrouillage de fichier
        public async Task<IActionResult> TelechargerSauvegarde()
        {
            // 1. On demande à SQLite de faire une copie propre dans un fichier temporaire.
            var cheminTemp = Path.Combine(Path.GetTempPath(), $"sauvegarde_{Guid.NewGuid()}.db");
            try
            {
                await _db.Database.ExecuteSqlRawAsync($"VACUUM INTO '{cheminTemp}';");

                // 2. On lit la copie temporaire en mémoire
                var bytes = await System.IO.File.ReadAllBytesAsync(cheminTemp);

                // 3. On prépare un nom de fichier propre avec la date
                var nomFichier = $"sauvegarde_kitabataba_{DateTime.Now:yyyyMMdd_HHmm}.db";

                // 4. On renvoie le fichier au navigateur
                return File(bytes, "application/octet-stream", nomFichier);
            }
            catch (Exception ex)
            {
                TempData["Erreur"] = $"Impossible de générer la sauvegarde : {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
            finally
            {
                // 5. On nettoie le fichier temporaire une fois qu'il a été téléchargé
                if (System.IO.File.Exists(cheminTemp))
                {
                    try { System.IO.File.Delete(cheminTemp); } catch { /* On ignore */ }
                }
            }
        }
        // POST: /Parametres/RestaurerSauvegarde
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RestaurerSauvegarde(IFormFile fichierSauvegarde)
        {
            if (fichierSauvegarde == null || fichierSauvegarde.Length == 0)
            {
                TempData["Erreur"] = "Veuillez sélectionner un fichier de sauvegarde (.db).";
                return RedirectToAction(nameof(Index));
            }

            if (!fichierSauvegarde.FileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Erreur"] = "Le fichier choisi n'est pas un fichier de base de données valide (.db).";
                return RedirectToAction(nameof(Index));
            }

            var cheminActuel = Path.Combine(_env.ContentRootPath, "perception.db");
            var cheminTemp = Path.Combine(_env.ContentRootPath, "perception_restauration.db");

            try
            {
                // 1. On enregistre le fichier uploadé dans un fichier temporaire
                using (var stream = new FileStream(cheminTemp, FileMode.Create))
                {
                    await fichierSauvegarde.CopyToAsync(stream);
                }

                // 2. On ferme la connexion actuelle à la base de données
                await _db.Database.CloseConnectionAsync();

                // 3. On fait une copie de sécurité de la base actuelle (au cas où)
                if (System.IO.File.Exists(cheminActuel))
                {
                    System.IO.File.Copy(cheminActuel, cheminActuel + ".bak", true);
                }

                // 4. On remplace l'ancienne base par la nouvelle
                System.IO.File.Copy(cheminTemp, cheminActuel, true);
                System.IO.File.Delete(cheminTemp);

                TempData["Succes"] = "Les données ont été restaurées avec succès ! Veuillez redémarrer l'application (F5) pour voir les nouvelles données.";
            }
            catch (Exception ex)
            {
                TempData["Erreur"] = $"Erreur lors de la restauration : {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}