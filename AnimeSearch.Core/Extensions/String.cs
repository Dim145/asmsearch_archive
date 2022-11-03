using System.Globalization;

namespace AnimeSearch.Core.Extensions;

public static class String
{
    public static bool ContainsGenre(this string str, string genre)
    {
        var tmdbEq = CoreUtils.TMDB_TVMAZE_GENRES_EQ.GetValueOrDefault(str.ToLowerInvariant());

        return str.Equals(genre, StringComparison.InvariantCultureIgnoreCase) || (tmdbEq ?? str).Contains(genre, StringComparison.InvariantCultureIgnoreCase);
    }
    
    /// <summary>
    ///     Transforme un mot en upper camel case. <br/>
    ///     search => Search <br/>
    ///     SEARCH => Search <br/>
    ///     autre_string => Autre_string
    /// </summary>
    /// <param name="str">Un mot à convertir</param>
    /// <returns>le résultat ou string.Empty si param null ou vide</returns>
    public static string ToUpperCamelCase(this string str)
    {
        if (!string.IsNullOrWhiteSpace(str))
            return char.ToUpper(str[0]) + str[1..].ToLower();

        return string.Empty;
    }
    
    /// <summary>
    ///     Transforme une string en double. <br/>
    ///     Si cela échoue, c'est la valeur par défault qui est renvoyée.
    /// </summary>
    /// <param name="value">Une string qui représente un nombre.</param>
    /// <param name="defaultValue">Valeur renvoyer en cas d'echec. 0 par défault.</param>
    /// <returns></returns>
    public static double ToDouble(this string value, double defaultValue = 0.0)
    {
        // Try parsing in the current culture
        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out double result) &&
            // Then try in US english
            !double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result) &&
            // Then in neutral language
            !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            result = defaultValue;
        }

        return result;
    }
}