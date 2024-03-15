namespace Mng.Microconnect.Core;

public interface IMessageSerializer
{
    // TODO: Span/memory?
    ReadOnlyMemory<byte> SerializeRequest(Request request);
    
    Request DeserializeRequest(ReadOnlyMemory<byte> requestBytes);
    
    Response DeserializeResponse(ReadOnlyMemory<byte> responseBytes);
    
    ReadOnlyMemory<byte> SerializeResponse(Response response);
}