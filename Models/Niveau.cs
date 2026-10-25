namespace PerceptionKitabataba.Models
{
    public enum Niveau
    {
        Septieme = 1,
        Huitieme = 2,
        PremiereCommercialeGestion = 3,
        PremiereScientifique = 4,
        PremiereHumanitePedagogique = 5,
        PremiereTechniqueCoupeCouture = 6,
        PremiereAgronomie = 7,
        PremiereElectricite = 8
    }

    public static class NiveauExtensions
    {
        public static string Libelle(this Niveau n) => n switch
        {
            Niveau.Septieme => "7ème",
            Niveau.Huitieme => "8ème",
            Niveau.PremiereCommercialeGestion => "1ère – Commercial et gestion",
            Niveau.PremiereScientifique => "1ère – Scientifique",
            Niveau.PremiereHumanitePedagogique => "1ère – Humanité pédagogique",
            Niveau.PremiereTechniqueCoupeCouture => "1ère – Technique coupe et couture",
            Niveau.PremiereAgronomie => "1ère – Agronomie",
            Niveau.PremiereElectricite => "1ère – Électricité",
            _ => n.ToString()
        };

        public static IEnumerable<Niveau> Tous() => Enum.GetValues<Niveau>().Cast<Niveau>();
    }
}