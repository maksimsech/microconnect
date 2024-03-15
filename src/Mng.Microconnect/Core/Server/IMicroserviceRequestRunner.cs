namespace Mng.Microconnect.Core.Server;

public interface IMicroserviceRequestRunner<T> where T: class
{
    Task<Response> RunAsync(Request request);
}