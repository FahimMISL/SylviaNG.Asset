using System.Text.Json;
using System.Text.Json.Serialization;
using SylviaNG.Assets.SharedKernel.Utils;

namespace SylviaNG.Assets.SharedKernel.Utils;

/// <summary>
/// Automatically converts UTC DateTime to local time when serializing to JSON,
/// and treats incoming JSON DateTime as local time (converts to UTC).
/// </summary>
public class LocalDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dt = reader.GetDateTime();
        return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified) == default
            ? default
            : DateTimeUtility.ConvertLocalToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified));
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var local = value.Kind == DateTimeKind.Utc
            ? DateTimeUtility.ConvertUtcToLocal(value)
            : value;

        writer.WriteStringValue(local.ToString("yyyy-MM-ddTHH:mm:ss"));
    }
}

public class NullableLocalDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        var dt = reader.GetDateTime();
        return DateTimeUtility.ConvertLocalToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified));
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var local = value.Value.Kind == DateTimeKind.Utc
            ? DateTimeUtility.ConvertUtcToLocal(value.Value)
            : value.Value;

        writer.WriteStringValue(local.ToString("yyyy-MM-ddTHH:mm:ss"));
    }
}
