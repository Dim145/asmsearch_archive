namespace AnimeSearch.Core;

/// <summary>
///     Enum des différents sujet pour les mails.
///     _ = " "
///     __ = "'"
/// </summary>
public enum Subjects: byte
{
    Propositions_d__améliorations_ou_d__ajouts = 0,
    Bug_détecté = 1,
    Canditature_Administrateur = 2,
    Canditature_developpeur = 3,
    Autres = 4
}