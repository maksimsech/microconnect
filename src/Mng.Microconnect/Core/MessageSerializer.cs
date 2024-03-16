
using System.Text;
using Newtonsoft.Json;

namespace Mng.Microconnect.Core;

internal sealed class MessageSerializer : IMessageSerializer
{
    private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All,
    };
    
    public ReadOnlyMemory<byte> SerializeRequest(Request request)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request, Formatting.Indented, _jsonSerializerSettings));
    }

    public Request DeserializeRequest(ReadOnlyMemory<byte> requestBytes)
    {
        var jsonString = Encoding.UTF8.GetString(requestBytes.Span);

        return (Request)JsonConvert.DeserializeObject(jsonString, _jsonSerializerSettings)!;
    }

    public ReadOnlyMemory<byte> SerializeResponse(Response response)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response, Formatting.Indented, _jsonSerializerSettings));
    }
    
    public Response DeserializeResponse(ReadOnlyMemory<byte> responseBytes)
    {
        var jsonString = Encoding.UTF8.GetString(responseBytes.Span);

        return (Response)JsonConvert.DeserializeObject(jsonString, _jsonSerializerSettings)!;
    }
}