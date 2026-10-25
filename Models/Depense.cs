namespace PerceptionKitabataba.Models
{
    public class Depense
    {
        public int Id { get; set; }
        public string Motif { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public string Devise { get; set; } = "CDF";
        public System.DateTime DateDepense { get; set; } = System.DateTime.Today;
        public string Observation { get; set; } = string.Empty;
    }
}