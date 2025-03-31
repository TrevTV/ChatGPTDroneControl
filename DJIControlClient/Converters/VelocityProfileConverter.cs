using System.Text.Json.Serialization;
using System.Text.Json;

namespace DJIControlClient.Converters
{
    public class VelocityProfileConverter : JsonConverter<VelocityProfile>
    {
        public override VelocityProfile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? enumString = reader.GetString();
                if (Enum.TryParse(enumString, true, out VelocityProfile mode))
                {
                    return mode;
                }
            }

            throw new JsonException($"Unable to convert to {typeof(VelocityProfile)}.");
        }

        public override void Write(Utf8JsonWriter writer, VelocityProfile value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString().ToUpper());
        }
    }
}
