using System.ComponentModel.DataAnnotations;

namespace PerceptionKitabataba.Models.ViewModels
{
    public class FraisFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le libellé est obligatoire.")]
        public string Libelle { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
        public decimal Montant { get; set; }

        public string Devise { get; set; } = "CDF";

        public string Periode { get; set; } = string.Empty;

        public bool ToutesClasses { get; set; }

        public List<int> NiveauxSelectionnes { get; set; } = new();

        public TypeFrais Type { get; set; } = TypeFrais.Ponctuel;

        public int? Mois { get; set; }

        public int? AnneeCible { get; set; }
    }
}