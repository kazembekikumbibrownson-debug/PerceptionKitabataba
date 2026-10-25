using System.ComponentModel.DataAnnotations;

namespace PerceptionKitabataba.Models
{
    public class Utilisateur
    {
        public int Id { get; set; }

        [Required, StringLength(40)]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string MotDePasseHash { get; set; } = string.Empty;

        [StringLength(80)]
        public string NomComplet { get; set; } = string.Empty;

        [StringLength(30)]
        public string Role { get; set; } = "Percepteur";

        public bool Actif { get; set; } = true;
    }
}