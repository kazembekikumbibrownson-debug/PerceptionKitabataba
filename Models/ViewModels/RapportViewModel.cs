using PerceptionKitabataba.Models;

namespace PerceptionKitabataba.Models.ViewModels
{
    public class RapportViewModel
    {
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public int NombrePaiements { get; set; }
        public decimal TotalCDF { get; set; }
        public decimal TotalUSD { get; set; }
        public decimal TauxUSDversCDF { get; set; }
        public decimal ContreValeurCDF => TotalCDF + (TotalUSD * TauxUSDversCDF);

        // La liste qui posait problème
        public List<LigneParNiveau> ParNiveau { get; set; } = new();

        public List<Paiement> Journal { get; set; } = new();
        public List<LigneImpaye> Impayes { get; set; } = new();
    }

    public class LigneParNiveau
    {
        public Niveau Niveau { get; set; }
        public string Section { get; set; } = string.Empty; // La propriété qu'il manquait
        public decimal TotalCDF { get; set; }
        public decimal TotalUSD { get; set; }
        public int NombrePaiements { get; set; }
    }

    public class LigneImpaye
    {
        public Eleve Eleve { get; set; } = null!;
        public decimal ResteCDF { get; set; }
        public decimal ResteUSD { get; set; }
    }
}