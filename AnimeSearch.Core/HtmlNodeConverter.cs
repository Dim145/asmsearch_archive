using HtmlAgilityPack;
using Newtonsoft.Json;

namespace AnimeSearch.Core;

public class HtmlNodeConverter : JsonConverter<HtmlNode>
{
    public override void WriteJson(JsonWriter writer, HtmlNode value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.OuterHtml);
    }

    public override HtmlNode ReadJson(JsonReader reader, Type objectType, HtmlNode existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        return HtmlNode.CreateNode(serializer.Deserialize<string>(reader));
    }
}