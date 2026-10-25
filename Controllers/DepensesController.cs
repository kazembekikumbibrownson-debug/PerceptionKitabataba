using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Data;
using PerceptionKitabataba.Models;

namespace PerceptionKitabataba.Controllers
{
    [Authorize]
    public class DepensesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DepensesController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var depenses = await _db.Depenses
                .OrderByDescending(d => d.DateDepense)
                .ThenByDescending(d => d.Id)
                .ToListAsync();

            ViewBag.TotalCDF = depenses.Where(d => d.Devise == "CDF").Sum(d => d.Montant);
            ViewBag.TotalUSD = depenses.Where(d => d.Devise == "USD").Sum(d => d.Montant);

            return View(depenses);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Depense depense)
        {
            if (ModelState.IsValid)
            {
                _db.Depenses.Add(depense);
                await _db.SaveChangesAsync();
                TempData["Succes"] = "Dépense enregistrée avec succès.";
            }
            else
            {
                TempData["Erreur"] = "Erreur dans le formulaire.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var depense = await _db.Depenses.FindAsync(id);
            if (depense != null)
            {
                _db.Depenses.Remove(depense);
                await _db.SaveChangesAsync();
                TempData["Succes"] = "Dépense supprimée.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}