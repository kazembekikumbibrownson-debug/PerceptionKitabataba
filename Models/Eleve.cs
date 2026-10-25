using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PerceptionKitabataba.Models
{
    public class Eleve
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Matricule { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string Nom { get; set; } = string.Empty;

        [StringLength(60)]
        public string Postnom { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string Prenom { get; set; } = string.Empty;

        [StringLength(1)]
        public string Sexe { get; set; } = "M";

        public Niveau Niveau { get; set; }

        // Pour gérer les parallèles (A, B...)
        [StringLength(5)]
        [Display(Name = "Section")]
        public string Section { get; set; } = string.Empty;

        [StringLength(100)]
        public string Tuteur { get; set; } = string.Empty;

        [StringLength(20)]
        public string Telephone { get; set; } = string.Empty;

        [StringLength(20)]
        public string AnneeScolaire { get; set; } = string.Empty;

        public DateTime DateInscription { get; set; } = DateTime.Today;

        public int MoisDebutMinerval { get; set; }
        public int AnneeDebutMinerval { get; set; }

        [NotMapped]
        public string NomComplet => $"{Nom} {Postnom} {Prenom}".Trim();

        [NotMapped]
        public string NiveauComplet => string.IsNullOrWhiteSpace(Section)
            ? Niveau.Libelle()
            : $"{Niveau.Libelle()} {Section}";
    }
}