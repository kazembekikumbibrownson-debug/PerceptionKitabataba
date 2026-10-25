using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PerceptionKitabataba.Models
{
    public enum TypeFrais
    {
        Inscription = 0,
        Mensuel = 1,
        Ponctuel = 2
    }

    public class Frais
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Libelle { get; set; } = string.Empty;

        public decimal Montant { get; set; }

        [StringLength(5)]
        public string Devise { get; set; } = "CDF";

        [StringLength(60)]
        public string Periode { get; set; } = string.Empty;

        [StringLength(100)]
        public string CiblesNiveaux { get; set; } = "TOUTES";

        public TypeFrais Type { get; set; } = TypeFrais.Ponctuel;
        public int? Mois { get; set; }
        public int? AnneeCible { get; set; }
        public int Ordre { get; set; } = 100;

        [NotMapped]
        public bool ToutesClasses => CiblesNiveaux == "TOUTES";

        [NotMapped]
        public List<Niveau> Cibles
        {
            get
            {
                if (ToutesClasses || string.IsNullOrWhiteSpace(CiblesNiveaux))
                    return new List<Niveau>();
                return CiblesNiveaux
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => (Niveau)int.Parse(s))
                    .ToList();
            }
            set
            {
                if (value == null || value.Count == 0) CiblesNiveaux = "TOUTES";
                else CiblesNiveaux = string.Join(",", value.Select(n => (int)n));
            }
        }

        [NotMapped]
        public string CiblesLibelle
        {
            get
            {
                if (ToutesClasses) return "Toutes les classes";
                return string.Join(", ", Cibles.Select(n => n.Libelle()));
            }
        }

        public bool SAppliqueA(Niveau niveau) => ToutesClasses || Cibles.Contains(niveau);
    }
}