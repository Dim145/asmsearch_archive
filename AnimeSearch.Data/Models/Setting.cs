using HtmlAgilityPack;
using Newtonsoft.Json;

namespace AnimeSearch.Data.Models;

public class Setting
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string TypeValue { get; set; }
    public string JsonValue { get; set; }
    public bool IsDeletable { get; set; } = true;

    /// <summary>
    /// Désérialise le json selon le type value et le donne dans un objet dynamique.
    /// </summary>
    /// <returns></returns>
    public dynamic GetValueObject(params JsonConverter[] converters) => JsonConvert.DeserializeObject(JsonValue, Type.GetType(TypeValue), converters);

    public T GetValueObject<T>(params JsonConverter[] converters) => JsonConvert.DeserializeObject<T>(JsonValue, converters);

    public T GetValueObject<T>(T type) => JsonConvert.DeserializeAnonymousType(JsonValue, type);

    public void SetValue(object value, params JsonConverter[] converters)
    {
        JsonValue = value is null ? null : JsonConvert.SerializeObject(value, converters);
        TypeValue = value?.GetType().FullName;
    }
}