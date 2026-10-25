using Microsoft.EntityFrameworkCore;
using PerceptionKitabataba.Models;

namespace PerceptionKitabataba.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Eleve> Eleves => Set<Eleve>();
        public DbSet<Frais> Frais => Set<Frais>();
        public DbSet<Paiement> Paiements => Set<Paiement>();
        public DbSet<Parametre> Parametres => Set<Parametre>();
        public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
        public DbSet<Depense> Depenses => Set<Depense>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Eleve>()
                .HasIndex(e => e.Matricule).IsUnique();

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.NumeroRecu).IsUnique();

            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Eleve).WithMany()
                .HasForeignKey(p => p.EleveId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Frais).WithMany()
                .HasForeignKey(p => p.FraisId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Utilisateur>()
                .HasIndex(u => u.Login).IsUnique();
        }
    }
}