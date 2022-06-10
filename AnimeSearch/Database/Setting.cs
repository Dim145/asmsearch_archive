using Newtonsoft.Json;
using System;

namespace AnimeSearch.Database
{
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
        public dynamic GetValueObject() => JsonConvert.DeserializeObject(JsonValue, Type.GetType(TypeValue));

        public T GetValueObject<T>() => JsonConvert.DeserializeObject<T>(JsonValue);

        public T GetValueObject<T>(T type) => JsonConvert.DeserializeAnonymousType(JsonValue, type);

        public void SetValue(object value)
        {
            JsonValue = value is null ? null : JsonConvert.SerializeObject(value);
            TypeValue = value?.GetType().FullName;
        }
    }
}
