using System.Text.Json.Serialization;
using System.Text.Json;

namespace DJIControlClient.Converters
{
    public class ControlModeConverter : JsonConverter<ControlMode>
    {
        public override ControlMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? enumString = reader.GetString();
                if (Enum.TryParse(enumString, true, out ControlMode mode))
                {
                    return mode;
                }
            }

            throw new JsonException($"Unable to convert to {typeof(ControlMode)}.");
        }

        public override void Write(Utf8JsonWriter writer, ControlMode value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString().ToUpper());
        }
    }
}
