using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mng.Microconnect.Core;

internal static class JsonSerializerContext
{
    public static readonly JsonSerializerOptions Options;

    static JsonSerializerContext()
    {
        Options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };
    }
}