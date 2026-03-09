using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using pote.Config.DbModel;

namespace pote.Config.DataProvider.File;

/// <summary>
/// Handles backwards compatibility with the old format where Keys was a List&lt;string&gt;.
/// When a plain string is encountered, it is treated as a key with an empty name.
/// </summary>
public class ApiKeyEntryConverter : JsonConverter<ApiKeyEntry>
{
    public override ApiKeyEntry? ReadJson(JsonReader reader, Type objectType, ApiKeyEntry? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
            return new ApiKeyEntry { Key = (string)reader.Value! };

        if (reader.TokenType == JsonToken.StartObject)
        {
            var obj = JObject.Load(reader);
            return new ApiKeyEntry
            {
                Name = obj.Value<string>(nameof(ApiKeyEntry.Name)) ?? string.Empty,
                Key = obj.Value<string>(nameof(ApiKeyEntry.Key)) ?? string.Empty
            };
        }

        return new ApiKeyEntry();
    }

    public override void WriteJson(JsonWriter writer, ApiKeyEntry? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(ApiKeyEntry.Name));
        writer.WriteValue(value?.Name ?? string.Empty);
        writer.WritePropertyName(nameof(ApiKeyEntry.Key));
        writer.WriteValue(value?.Key ?? string.Empty);
        writer.WriteEndObject();
    }
}
