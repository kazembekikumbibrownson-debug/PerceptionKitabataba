using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Data;
using PerceptionKitabataba.Models;

namespace PerceptionKitabataba.Controllers
{
    [Authorize]
    public class ElevesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ElevesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Eleves
        public async Task<IActionResult> Index(string? q)
        {
            var eleves = _db.Eleves.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var terme = q.Trim().ToLower();
                eleves = eleves.Where(e =>
                    e.Nom.ToLower().Contains(terme) ||
                    e.Postnom.ToLower().Contains(terme) ||
                    e.Prenom.ToLower().Contains(terme) ||
                    e.Matricule.ToLower().Contains(terme));
            }

            ViewBag.Recherche = q;
            return View(await eleves.OrderBy(e => e.Nom).ThenBy(e => e.Prenom).ToListAsync());
        }

        // GET: /Eleves/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var eleve = await _db.Eleves.FirstOrDefaultAsync(e => e.Id == id);
            if (eleve == null) return NotFound();

            var paiements = await _db.Paiements
                .Include(p => p.Frais)
                .Where(p => p.EleveId == id)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();

            ViewBag.Paiements = paiements;
            ViewBag.TotalPayeCDF = paiements.Where(p => p.Devise == "CDF").Sum(p => p.Montant);
            ViewBag.TotalPayeUSD = paiements.Where(p => p.Devise == "USD").Sum(p => p.Montant);

            return View(eleve);
        }

        // GET: /Eleves/Create
        public IActionResult Create()
        {
            return View(new Eleve
            {
                AnneeScolaire = "2025-2026",
                DateInscription = DateTime.Today
            });
        }

        // POST: /Eleves/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ⚠️ AJOUT DE "Section" DANS LE BIND
        public async Task<IActionResult> Create([Bind("Matricule,Nom,Postnom,Prenom,Sexe,Niveau,Section,Tuteur,Telephone,AnneeScolaire,DateInscription")] Eleve eleve)
        {
            if (string.IsNullOrWhiteSpace(eleve.Matricule))
                eleve.Matricule = await GenererMatricule();

            bool existe = await _db.Eleves.AnyAsync(e => e.Matricule == eleve.Matricule);
            if (existe)
                ModelState.AddModelError(nameof(Eleve.Matricule), "Ce matricule existe déjà.");

            if (ModelState.IsValid)
            {
                var dateInscription = eleve.DateInscription == default ? DateTime.Today : eleve.DateInscription;
                if (dateInscription.Month == 9)
                {
                    eleve.MoisDebutMinerval = 10;
                    eleve.AnneeDebutMinerval = dateInscription.Year;
                }
                else
                {
                    eleve.MoisDebutMinerval = dateInscription.Month;
                    eleve.AnneeDebutMinerval = dateInscription.Year;
                }

                _db.Eleves.Add(eleve);
                await _db.SaveChangesAsync();
                TempData["Succes"] = $"Élève {eleve.NomComplet} enregistré avec le matricule {eleve.Matricule}.";
                return RedirectToAction(nameof(Index));
            }
            return View(eleve);
        }

        // GET: /Eleves/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var eleve = await _db.Eleves.FindAsync(id);
            if (eleve == null) return NotFound();
            return View(eleve);
        }

        // POST: /Eleves/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ⚠️ AJOUT DE "Section" DANS LE BIND
        public async Task<IActionResult> Edit(int id, [Bind("Id,Matricule,Nom,Postnom,Prenom,Sexe,Niveau,Section,Tuteur,Telephone,AnneeScolaire,DateInscription,MoisDebutMinerval,AnneeDebutMinerval")] Eleve eleve)
        {
            if (id != eleve.Id) return NotFound();

            bool existe = await _db.Eleves.AnyAsync(e => e.Matricule == eleve.Matricule && e.Id != eleve.Id);
            if (existe)
                ModelState.AddModelError(nameof(Eleve.Matricule), "Ce matricule est déjà utilisé.");

            if (ModelState.IsValid)
            {
                try
                {
                    _db.Update(eleve);
                    await _db.SaveChangesAsync();
                    TempData["Succes"] = "Élève modifié avec succès.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _db.Eleves.AnyAsync(e => e.Id == eleve.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(eleve);
        }

        // GET: /Eleves/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var eleve = await _db.Eleves.FirstOrDefaultAsync(e => e.Id == id);
            if (eleve == null) return NotFound();

            return View(eleve);
        }

        // POST: /Eleves/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eleve = await _db.Eleves.FindAsync(id);
            if (eleve != null)
            {
                _db.Eleves.Remove(eleve);
                await _db.SaveChangesAsync();
                TempData["Succes"] = $"Élève {eleve.NomComplet} supprimé.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Eleves/ImprimerListe?niveau=1
        public async Task<IActionResult> ImprimerListe(Niveau? niveau)
        {
            var eleves = _db.Eleves.AsQueryable();

            if (niveau.HasValue)
            {
                eleves = eleves.Where(e => e.Niveau == niveau.Value);
            }

            var liste = await eleves.OrderBy(e => e.Nom).ThenBy(e => e.Prenom).ToListAsync();

            var parametres = await _db.Parametres.FirstOrDefaultAsync();

            ViewBag.NomEcole = parametres?.NomEcole ?? "École Secondaire de Kitabataba";
            ViewBag.AnneeScolaire = parametres?.AnneeScolaire ?? "";
            ViewBag.NiveauFiltre = niveau;

            return View(liste);
        }

        private async Task<string> GenererMatricule()
        {
            int total = await _db.Eleves.CountAsync();
            string candidat;
            do
            {
                total++;
                candidat = $"KTB-{total:D3}";
            } while (await _db.Eleves.AnyAsync(e => e.Matricule == candidat));
            return candidat;
        }
    }
}