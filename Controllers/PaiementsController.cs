using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Data;
using PerceptionKitabataba.Models;
using PerceptionKitabataba.Models.ViewModels;

namespace PerceptionKitabataba.Controllers
{
    [Authorize]
    public class PaiementsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PaiementsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Paiements
        public async Task<IActionResult> Index()
        {
            var aujourdhui = DateTime.Today;
            var paiementsDuJour = await _db.Paiements
                .Include(p => p.Eleve).Include(p => p.Frais)
                .Where(p => p.DatePaiement == aujourdhui)
                .OrderByDescending(p => p.NumeroRecu).ToListAsync();

            ViewBag.TotalJourCDF = paiementsDuJour.Where(p => p.Devise == "CDF").Sum(p => p.Montant);
            ViewBag.TotalJourUSD = paiementsDuJour.Where(p => p.Devise == "USD").Sum(p => p.Montant);

            return View(paiementsDuJour);
        }

        // POST: /Paiements/Rechercher
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechercher(string terme)
        {
            if (string.IsNullOrWhiteSpace(terme))
            {
                TempData["Erreur"] = "Entrez un matricule ou un nom.";
                return RedirectToAction(nameof(Index));
            }

            var t = terme.Trim().ToLower();
            var eleve = await _db.Eleves.Where(e => e.Matricule.ToLower() == t).FirstOrDefaultAsync();

            if (eleve == null)
            {
                eleve = await _db.Eleves.Where(e => e.Nom.ToLower().Contains(t) || e.Prenom.ToLower().Contains(t)).OrderBy(e => e.Nom).FirstOrDefaultAsync();
            }

            if (eleve == null)
            {
                TempData["Erreur"] = $"Aucun élève trouvé pour « {terme} ».";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Encaisser), new { id = eleve.Id });
        }

        // GET: /Paiements/Encaisser/5
        public async Task<IActionResult> Encaisser(int? id)
        {
            if (id == null) return NotFound();
            var situation = await ConstruireSituation(id.Value);
            if (situation == null) return NotFound();
            return View(situation);
        }

        // POST: /Paiements/Encaisser (Ventilation automatique)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Encaisser(int eleveId, decimal montant, int fraisDepartId, string mode, string? observation, DateTime? datePaiement)
        {
            if (montant <= 0)
            {
                TempData["Erreur"] = "Le montant doit être supérieur à 0.";
                return RedirectToAction(nameof(Encaisser), new { id = eleveId });
            }

            var situation = await ConstruireSituation(eleveId);
            if (situation == null) return NotFound();

            var lignesOrdonnees = situation.Lignes.OrderBy(l => l.Frais.Ordre).ToList();
            int indexDepart = lignesOrdonnees.FindIndex(l => l.Frais.Id == fraisDepartId);

            if (indexDepart < 0)
            {
                TempData["Erreur"] = "Frais de départ introuvable.";
                return RedirectToAction(nameof(Encaisser), new { id = eleveId });
            }

            decimal reste = montant;
            var ventilation = new List<(Frais Frais, decimal Montant)>();

            // MOTEUR DE VENTILATION
            for (int i = indexDepart; i < lignesOrdonnees.Count && reste > 0; i++)
            {
                var ligne = lignesOrdonnees[i];
                decimal resteLigne = ligne.Du - ligne.Paye;
                if (resteLigne <= 0) continue;

                decimal allocation = Math.Min(reste, resteLigne);
                ventilation.Add((ligne.Frais, allocation));
                reste -= allocation;
            }

            if (!ventilation.Any())
            {
                TempData["Erreur"] = "Aucun frais à payer à partir de ce mois.";
                return RedirectToAction(nameof(Encaisser), new { id = eleveId });
            }

            if (reste > 0)
            {
                TempData["Erreur"] = $"Le montant dépasse le total dû ({montant - reste:N0} CDF utilisables).";
                return RedirectToAction(nameof(Encaisser), new { id = eleveId });
            }

            int dernierNumero = await _db.Paiements.OrderByDescending(p => p.NumeroRecu).Select(p => p.NumeroRecu).FirstOrDefaultAsync();
            int numeroBase = dernierNumero + 1;
            var parametres = await _db.Parametres.FirstOrDefaultAsync();

            foreach (var (frais, montantAlloue) in ventilation)
            {
                var paiement = new Paiement
                {
                    NumeroRecu = numeroBase,
                    EleveId = eleveId,
                    FraisId = frais.Id,
                    Montant = montantAlloue,
                    Devise = frais.Devise,
                    DatePaiement = datePaiement ?? DateTime.Today,
                    Mode = string.IsNullOrWhiteSpace(mode) ? "Espèces" : mode,
                    Observation = observation ?? string.Empty,
                    Percepteur = parametres?.NomPercepteur ?? string.Empty
                };
                _db.Paiements.Add(paiement);
            }

            await _db.SaveChangesAsync();
            TempData["Succes"] = $"Paiement enregistré. Reçu N° {numeroBase}.";
            return RedirectToAction(nameof(RecuGroupe), new { numero = numeroBase });
        }

        // POST: /Paiements/PayerPonctuel (Frais fixes)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PayerPonctuel(int eleveId, int fraisId, decimal montant, string mode, string? observation, DateTime? datePaiement)
        {
            var frais = await _db.Frais.FindAsync(fraisId);
            if (frais == null || frais.Type != TypeFrais.Ponctuel) return NotFound();

            if (montant <= 0) { TempData["Erreur"] = "Montant invalide."; return RedirectToAction(nameof(Encaisser), new { id = eleveId }); }

            // ⚠️ CORRECTION ICI : On récupère d'abord en mémoire, puis on calcule
            var paiementsExistants = await _db.Paiements
                .Where(p => p.EleveId == eleveId && p.FraisId == fraisId)
                .ToListAsync();

            decimal dejaPaye = paiementsExistants.Sum(p => p.Montant);
            decimal reste = frais.Montant - dejaPaye;

            if (montant > reste) { TempData["Erreur"] = $"Le montant dépasse le reste ({reste:N0} {frais.Devise})."; return RedirectToAction(nameof(Encaisser), new { id = eleveId }); }

            int dernierNumero = await _db.Paiements.OrderByDescending(p => p.NumeroRecu).Select(p => p.NumeroRecu).FirstOrDefaultAsync();
            var parametres = await _db.Parametres.FirstOrDefaultAsync();

            var paiement = new Paiement
            {
                NumeroRecu = dernierNumero + 1,
                EleveId = eleveId,
                FraisId = fraisId,
                Montant = montant,
                Devise = frais.Devise,
                DatePaiement = datePaiement ?? DateTime.Today,
                Mode = string.IsNullOrWhiteSpace(mode) ? "Espèces" : mode,
                Observation = observation ?? string.Empty,
                Percepteur = parametres?.NomPercepteur ?? string.Empty
            };
            _db.Paiements.Add(paiement);
            await _db.SaveChangesAsync();

            TempData["Succes"] = $"Paiement enregistré. Reçu N° {paiement.NumeroRecu}.";
            return RedirectToAction(nameof(RecuGroupe), new { numero = paiement.NumeroRecu });
        }

        // GET: /Paiements/RecuGroupe/12
        public async Task<IActionResult> RecuGroupe(int numero)
        {
            var paiements = await _db.Paiements.Include(p => p.Eleve).Include(p => p.Frais)
                .Where(p => p.NumeroRecu == numero).OrderBy(p => p.Frais!.Ordre).ToListAsync();

            if (!paiements.Any()) return NotFound();
            ViewBag.Parametres = await _db.Parametres.FirstOrDefaultAsync();
            return View(paiements);
        }

        // GET: /Paiements/Historique/5
        public async Task<IActionResult> Historique(int? id)
        {
            if (id == null) return NotFound();
            var eleve = await _db.Eleves.FindAsync(id);
            if (eleve == null) return NotFound();

            var paiements = await _db.Paiements.Include(p => p.Frais)
                .Where(p => p.EleveId == id).OrderByDescending(p => p.DatePaiement).ToListAsync();

            ViewBag.Eleve = eleve;
            return View(paiements);
        }

        // Méthode privée pour construire la situation
        private async Task<SituationEleveViewModel?> ConstruireSituation(int eleveId)
        {
            var eleve = await _db.Eleves.FindAsync(eleveId);
            if (eleve == null) return null;

            var tousFrais = await _db.Frais.OrderBy(f => f.Ordre).ThenBy(f => f.Libelle).ToListAsync();

            // Filtre des frais applicables à l'élève
            var fraisApplicables = tousFrais.Where(f =>
            {
                if (!f.ToutesClasses && !f.Cibles.Contains(eleve.Niveau)) return false;

                if (f.Type == TypeFrais.Mensuel && f.Mois.HasValue)
                {
                    int m = f.Mois.Value;
                    int anneeFrais = m >= 9 ? (f.AnneeCible ?? 0) : (f.AnneeCible ?? 0) + 1;
                    var dateFrais = new DateTime(anneeFrais, m, 1);
                    var dateDebut = new DateTime(eleve.AnneeDebutMinerval, eleve.MoisDebutMinerval, 1);
                    return dateFrais >= dateDebut;
                }
                return true;
            }).ToList();

            var paiements = await _db.Paiements.Where(p => p.EleveId == eleveId).ToListAsync();

            var vm = new SituationEleveViewModel { Eleve = eleve };

            foreach (var f in fraisApplicables)
            {
                var ligne = new LigneSituation
                {
                    Frais = f,
                    Du = f.Montant,
                    Paye = paiements.Where(p => p.FraisId == f.Id).Sum(p => p.Montant)
                };

                if (f.Type == TypeFrais.Ponctuel) vm.LignesPonctuelles.Add(ligne);
                else vm.Lignes.Add(ligne);
            }

            return vm;
        }
    }
}