namespace AnimeSearch.Models
{
    public enum EtatSite: byte
    {
        NON_VALIDER = 0,
        VALIDER = 1,
        ERREUR_404 = 2,
        ERREUR_CLOUDFLARE = 3,
        ERREUR_0_RESULT = 4
    }

    public static class EtatSiteString
    {
        private static readonly string[] TabString = { string.Empty, "Pas encore validé par un admin.", "Site fonctionnel", "L'adresse URL de ce site est inateignable (404)",
                                              "CloudFlare à bloquer l'accès à ce site pour le serveur (browser-checking)", "Le serveur ne trouve jamais de résultat pour ce site."};

        /// <summary>
        ///     Donne l'état du site sous forme de chaine de caractères. Si aucune chaine n'est renseigné pour l'état, renvoi une chaine vide.
        /// </summary>
        /// <param name="etat"></param>
        /// <returns></returns>
        public static string GetStringForEtat(EtatSite etat)
        {
            byte val = ((byte)etat);

            if (val < 0 || val >= TabString.Length - 1)
                return TabString[0];

            return TabString[val+1];
        }
    }
}
