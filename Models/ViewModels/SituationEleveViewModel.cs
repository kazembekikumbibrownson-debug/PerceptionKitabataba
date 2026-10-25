using PerceptionKitabataba.Models;

namespace PerceptionKitabataba.Models.ViewModels
{
    public class SituationEleveViewModel
    {
        public Eleve Eleve { get; set; } = null!;
        public List<LigneSituation> Lignes { get; set; } = new();
        public List<LigneSituation> LignesPonctuelles { get; set; } = new();
        public bool AToutesLignesSoldees => Lignes.All(l => l.EstSolde);
    }
}