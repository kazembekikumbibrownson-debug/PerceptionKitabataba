using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Data;
using PerceptionKitabataba.Models;
using PerceptionKitabataba.Models.ViewModels;
using System.Diagnostics;

namespace PerceptionKitabataba.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var aujourdhui = DateTime.Today;

            var paiementsDuJour = await _db.Paiements.Where(p => p.DatePaiement == aujourdhui).ToListAsync();
            var depensesDuJour = await _db.Depenses.Where(d => d.DateDepense == aujourdhui && d.Devise == "CDF").ToListAsync();

            var tousLesPaiements = await _db.Paiements.ToListAsync();
            var toutesLesDepenses = await _db.Depenses.Where(d => d.Devise == "CDF").ToListAsync();
            var nombreEleves = await _db.Eleves.CountAsync();

            var derniers = await _db.Paiements
                .Include(p => p.Eleve).Include(p => p.Frais)
                .OrderByDescending(p => p.Id).Take(5).ToListAsync();

            var vm = new DashboardViewModel
            {
                RecettesJourCDF = paiementsDuJour.Where(p => p.Devise == "CDF").Sum(p => p.Montant),
                RecettesJourUSD = paiementsDuJour.Where(p => p.Devise == "USD").Sum(p => p.Montant),
                PaiementsJour = paiementsDuJour.Count,

                // Les nouvelles propriétés pour les dépenses
                DepensesJourCDF = depensesDuJour.Sum(d => d.Montant),
                DepensesTotalesCDF = toutesLesDepenses.Sum(d => d.Montant),

                RecettesTotalesCDF = tousLesPaiements.Where(p => p.Devise == "CDF").Sum(p => p.Montant),
                RecettesTotalesUSD = tousLesPaiements.Where(p => p.Devise == "USD").Sum(p => p.Montant),

                NombreEleves = nombreEleves,
                DerniersPaiements = derniers
            };

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}