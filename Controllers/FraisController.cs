using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Data;
using PerceptionKitabataba.Models;
using PerceptionKitabataba.Models.ViewModels;

namespace PerceptionKitabataba.Controllers
{
    [Authorize]
    public class FraisController : Controller
    {
        private readonly ApplicationDbContext _db;

        public FraisController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Frais
        public async Task<IActionResult> Index()
        {
            var frais = await _db.Frais.OrderBy(f => f.Ordre).ThenBy(f => f.Libelle).ToListAsync();
            return View(frais);
        }

        // GET: /Frais/Create
        public IActionResult Create()
        {
            return View(new FraisFormViewModel());
        }

        // POST: /Frais/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FraisFormViewModel vm)
        {
            if (!vm.ToutesClasses && vm.NiveauxSelectionnes.Count == 0)
                ModelState.AddModelError(string.Empty, "Cochez au moins une classe ou « Toutes les classes ».");

            if (vm.Type == TypeFrais.Mensuel && (vm.Mois == null || vm.AnneeCible == null))
                ModelState.AddModelError(string.Empty, "Pour un frais mensuel, précisez le mois et l'année.");

            if (ModelState.IsValid)
            {
                int ordre = vm.Type switch
                {
                    TypeFrais.Inscription => 0,
                    TypeFrais.Mensuel => 100 + (vm.Mois ?? 0),
                    _ => 500
                };

                var frais = new Frais
                {
                    Libelle = vm.Libelle,
                    Montant = vm.Montant,
                    Devise = vm.Devise,
                    Periode = vm.Periode,
                    CiblesNiveaux = vm.ToutesClasses ? "TOUTES" : string.Join(",", vm.NiveauxSelectionnes),
                    Type = vm.Type,
                    Mois = vm.Type == TypeFrais.Mensuel ? vm.Mois : null,
                    AnneeCible = vm.Type == TypeFrais.Mensuel ? vm.AnneeCible : null,
                    Ordre = ordre
                };
                _db.Frais.Add(frais);
                await _db.SaveChangesAsync();
                TempData["Succes"] = $"Frais « {frais.Libelle} » ajouté.";
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        // GET: /Frais/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var frais = await _db.Frais.FindAsync(id);
            if (frais == null) return NotFound();

            return View(new FraisFormViewModel
            {
                Id = frais.Id,
                Libelle = frais.Libelle,
                Montant = frais.Montant,
                Devise = frais.Devise,
                Periode = frais.Periode,
                ToutesClasses = frais.ToutesClasses,
                NiveauxSelectionnes = frais.Cibles.Select(n => (int)n).ToList(),
                Type = frais.Type,
                Mois = frais.Mois,
                AnneeCible = frais.AnneeCible
            });
        }

        // POST: /Frais/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FraisFormViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            if (!vm.ToutesClasses && vm.NiveauxSelectionnes.Count == 0)
                ModelState.AddModelError(string.Empty, "Cochez au moins une classe ou « Toutes les classes ».");

            if (vm.Type == TypeFrais.Mensuel && (vm.Mois == null || vm.AnneeCible == null))
                ModelState.AddModelError(string.Empty, "Pour un frais mensuel, précisez le mois et l'année.");

            if (ModelState.IsValid)
            {
                var frais = await _db.Frais.FindAsync(id);
                if (frais == null) return NotFound();

                frais.Libelle = vm.Libelle;
                frais.Montant = vm.Montant;
                frais.Devise = vm.Devise;
                frais.Periode = vm.Periode;
                frais.CiblesNiveaux = vm.ToutesClasses ? "TOUTES" : string.Join(",", vm.NiveauxSelectionnes);
                frais.Type = vm.Type;
                frais.Mois = vm.Type == TypeFrais.Mensuel ? vm.Mois : null;
                frais.AnneeCible = vm.Type == TypeFrais.Mensuel ? vm.AnneeCible : null;
                frais.Ordre = vm.Type switch
                {
                    TypeFrais.Inscription => 0,
                    TypeFrais.Mensuel => 100 + (vm.Mois ?? 0),
                    _ => 500
                };

                await _db.SaveChangesAsync();
                TempData["Succes"] = "Frais modifié.";
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        // GET: /Frais/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var frais = await _db.Frais.FirstOrDefaultAsync(f => f.Id == id);
            if (frais == null) return NotFound();
            return View(frais);
        }

        // POST: /Frais/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var frais = await _db.Frais.FindAsync(id);
            if (frais != null)
            {
                _db.Frais.Remove(frais);
                await _db.SaveChangesAsync();
                TempData["Succes"] = "Frais supprimé.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}