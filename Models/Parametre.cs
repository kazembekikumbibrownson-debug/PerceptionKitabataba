using System.ComponentModel.DataAnnotations;

namespace PerceptionKitabataba.Models
{
    public class Parametre
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string NomEcole { get; set; } = "École Secondaire de Kitabataba";

        [StringLength(20)]
        public string AnneeScolaire { get; set; } = "2025-2026";

        [StringLength(80)]
        public string NomPercepteur { get; set; } = string.Empty;

        public decimal TauxUSDversCDF { get; set; } = 2800;
        public decimal MontantInscription { get; set; } = 70000;
        public decimal MontantMinervalMensuel { get; set; } = 30000;
        public int AnneeDebutScolaire { get; set; } = 2025;
    }
}