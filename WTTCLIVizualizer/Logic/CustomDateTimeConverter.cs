namespace WTTCLIVizualizer;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class CustomDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string dateString = reader.GetString();
        if (DateTime.TryParseExact(dateString, "ddd MMM dd yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }
        throw new JsonException($"Invalid date format: {dateString}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("ddd MMM dd yyyy", CultureInfo.InvariantCulture));
    }
}
