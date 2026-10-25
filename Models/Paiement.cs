using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PerceptionKitabataba.Models
{
    public class Paiement
    {
        public int Id { get; set; }

        public int NumeroRecu { get; set; }

        [Required]
        public int EleveId { get; set; }

        [ForeignKey(nameof(EleveId))]
        public Eleve? Eleve { get; set; }

        [Required]
        public int FraisId { get; set; }

        [ForeignKey(nameof(FraisId))]
        public Frais? Frais { get; set; }

        public decimal Montant { get; set; }

        [StringLength(5)]
        public string Devise { get; set; } = "CDF";

        public DateTime DatePaiement { get; set; } = DateTime.Today;

        [StringLength(30)]
        public string Mode { get; set; } = "Espèces";

        [StringLength(200)]
        public string Observation { get; set; } = string.Empty;

        [StringLength(80)]
        public string Percepteur { get; set; } = string.Empty;
    }
}