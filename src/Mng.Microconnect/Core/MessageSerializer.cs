using System.Text;
using System.Text.Json;

namespace Mng.Microconnect.Core;

internal sealed class MessageSerializer : IMessageSerializer
{
    public ReadOnlyMemory<byte> SerializeRequest(Request request) =>
        JsonSerializer.SerializeToUtf8Bytes(
            request,
            JsonSerializerContext.Options
        );
    
    public Request DeserializeRequest(ReadOnlyMemory<byte> requestBytes)
    {
        // TODO: Handle errors, nulls.
        using var jsonDocument = JsonDocument.Parse(requestBytes);

        var element = jsonDocument.RootElement;

        var methodName = element.GetProperty("methodName").GetString()!;

        var arguments = element
            .GetProperty("arguments")
            .EnumerateArray()
            .Select(e =>
            {
                // TODO: Doesn't work with primitives
                var typeString = e.GetProperty("type").GetString()!;
                var type = Type.GetType(typeString);
                var value = JsonSerializer.Deserialize(
                    e.GetProperty("value").GetString()!,
                    type!,
                    JsonSerializerContext.Options
                );
                return new Request.RequestArgument(typeString, value!);
            })
            .ToList();
        
        
        return new Request
        {
            MethodName = methodName,
            Arguments = arguments,
        };
    }

    public ReadOnlyMemory<byte> SerializeResponse(Response response)
    {
        throw new NotImplementedException();
    }
    
    public Response DeserializeResponse(ReadOnlyMemory<byte> responseBytes)
    {
        throw new NotImplementedException();
    }

}