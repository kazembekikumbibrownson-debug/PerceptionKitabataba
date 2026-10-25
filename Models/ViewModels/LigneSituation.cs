using PerceptionKitabataba.Models;

namespace PerceptionKitabataba.Models.ViewModels
{
    public class LigneSituation
    {
        public Frais Frais { get; set; } = null!;
        public decimal Du { get; set; }
        public decimal Paye { get; set; }
        public decimal Reste => Du - Paye;
        public bool EstSolde => Reste <= 0;
    }
}