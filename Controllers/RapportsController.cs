using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Data;
using PerceptionKitabataba.Models;
using PerceptionKitabataba.Models.ViewModels;
using System.Text;

namespace PerceptionKitabataba.Controllers
{
    [Authorize]
    public class RapportsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RapportsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Rapports
        public async Task<IActionResult> Index(DateTime? du, DateTime? au)
        {
            var dateDebut = du?.Date ?? DateTime.Today;
            var dateFin = au?.Date ?? DateTime.Today;

            if (dateDebut > dateFin) (dateDebut, dateFin) = (dateFin, dateDebut);

            var modele = await ConstruireRapport(dateDebut, dateFin);
            return View(modele);
        }

        // GET: /Rapports/ExportCsv
        public async Task<IActionResult> ExportCsv(DateTime? du, DateTime? au)
        {
            var dateDebut = du?.Date ?? DateTime.Today;
            var dateFin = au?.Date ?? DateTime.Today;
            if (dateDebut > dateFin) (dateDebut, dateFin) = (dateFin, dateDebut);

            var paiements = await _db.Paiements
                .Include(p => p.Eleve).Include(p => p.Frais)
                .Where(p => p.DatePaiement >= dateDebut && p.DatePaiement <= dateFin)
                .OrderBy(p => p.DatePaiement).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Date;N° Reçu;Matricule;Élève;Classe;Frais;Mode;Montant;Devise");

            foreach (var p in paiements)
            {
                string nomEleve = p.Eleve != null ? $"{p.Eleve.Nom} {p.Eleve.Postnom} {p.Eleve.Prenom}".Trim() : "";
                string classe = p.Eleve != null ? p.Eleve.Niveau.Libelle() : "";
                string frais = p.Frais != null ? p.Frais.Libelle : "";

                nomEleve = nomEleve.Replace(";", ",");
                frais = frais.Replace(";", ",");

                sb.AppendLine($"{p.DatePaiement:dd/MM/yyyy};{p.NumeroRecu};{p.Eleve?.Matricule};{nomEleve};{classe};{frais};{p.Mode};{p.Montant};{p.Devise}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var nomFichier = $"Rapport_Kitabataba_{dateDebut:yyyyMMdd}au{dateFin:yyyyMMdd}.csv";
            return File(bytes, "text/csv", nomFichier);
        }

        private async Task<RapportViewModel> ConstruireRapport(DateTime dateDebut, DateTime dateFin)
        {
            var parametres = await _db.Parametres.FirstOrDefaultAsync();

            var paiements = await _db.Paiements
                .Include(p => p.Eleve).Include(p => p.Frais)
                .Where(p => p.DatePaiement >= dateDebut && p.DatePaiement <= dateFin)
                .OrderByDescending(p => p.DatePaiement).ThenByDescending(p => p.NumeroRecu)
                .ToListAsync();

            var modele = new RapportViewModel
            {
                DateDebut = dateDebut,
                DateFin = dateFin,
                NombrePaiements = paiements.Count,
                TotalCDF = paiements.Where(p => p.Devise == "CDF").Sum(p => p.Montant),
                TotalUSD = paiements.Where(p => p.Devise == "USD").Sum(p => p.Montant),
                TauxUSDversCDF = parametres?.TauxUSDversCDF ?? 0m,
                Journal = paiements
            };
            modele.ParNiveau = paiements.Where(p => p.Eleve != null)
            .GroupBy(p => new { p.Eleve!.Niveau, p.Eleve.Section }) // <-- Groupement par Classe ET Section
            .Select(g => new LigneParNiveau
            {
                Niveau = g.Key.Niveau,
                Section = g.Key.Section,
                NombrePaiements = g.Count(),
                TotalCDF = g.Where(p => p.Devise == "CDF").Sum(p => p.Montant),
                TotalUSD = g.Where(p => p.Devise == "USD").Sum(p => p.Montant)
            })
            .OrderBy(l => (int)l.Niveau)
            .ThenBy(l => l.Section)
            .ToList();


            var eleves = await _db.Eleves.ToListAsync();
            var tousFrais = await _db.Frais.ToListAsync();
            var tousPaiements = await _db.Paiements.ToListAsync();

            var impayes = new List<LigneImpaye>();
            foreach (var eleve in eleves)
            {
                var fraisApplicables = tousFrais.Where(f => f.ToutesClasses || f.Cibles.Contains(eleve.Niveau)).ToList();
                decimal resteCDF = 0, resteUSD = 0;

                foreach (var f in fraisApplicables)
                {
                    if (f.Type == TypeFrais.Mensuel && f.Mois.HasValue)
                    {
                        int m = f.Mois.Value;
                        int anneeFrais = m >= 9 ? (f.AnneeCible ?? 0) : (f.AnneeCible ?? 0) + 1;
                        var dateFrais = new DateTime(anneeFrais, m, 1);
                        var dateDebutEleve = new DateTime(eleve.AnneeDebutMinerval, eleve.MoisDebutMinerval, 1);
                        if (dateFrais < dateDebutEleve) continue;
                    }

                    decimal paye = tousPaiements.Where(p => p.EleveId == eleve.Id && p.FraisId == f.Id).Sum(p => p.Montant);
                    decimal reste = f.Montant - paye;

                    if (reste > 0)
                    {
                        if (f.Devise == "USD") resteUSD += reste;
                        else resteCDF += reste;
                    }
                }

                if (resteCDF > 0 || resteUSD > 0)
                {
                    impayes.Add(new LigneImpaye { Eleve = eleve, ResteCDF = resteCDF, ResteUSD = resteUSD });
                }
            }

            modele.Impayes = impayes.OrderBy(i => i.Eleve.Nom).ToList();
            return modele;
        }
    }
}