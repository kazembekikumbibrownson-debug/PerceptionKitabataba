namespace PerceptionKitabataba.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Chiffres du jour
        public decimal RecettesJourCDF { get; set; }
        public decimal RecettesJourUSD { get; set; }
        public int PaiementsJour { get; set; }

        // Dépenses
        public decimal DepensesJourCDF { get; set; }
        public decimal DepensesTotalesCDF { get; set; }

        // Chiffres globaux
        public decimal RecettesTotalesCDF { get; set; }
        public decimal RecettesTotalesUSD { get; set; }

        // Élèves
        public int NombreEleves { get; set; }

        // Derniers paiements
        public List<Models.Paiement> DerniersPaiements { get; set; } = new();
    }
}