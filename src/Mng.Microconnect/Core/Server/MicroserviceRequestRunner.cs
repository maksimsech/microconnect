using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mng.Microconnect.Core.Server;

internal sealed class MicroserviceRequestRunner<T> : IMicroserviceRequestRunner<T> where T: class
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MicroserviceRequestRunner(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Response> RunAsync(Request request)
    {
        var method = GetMethodToRun(request);
        if (method is null)
        {
            // TODO: Custom exceptions with details, etc.
            throw new NotSupportedException($"Method {request.MethodName} is not supported.");
        }

        var parameters = request.Arguments.Select(a => a.Value).ToArray();

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var microservice = scope.ServiceProvider.GetRequiredService(typeof(T));

        var result = await RunMethodAsync(microservice, method, parameters);

        return new Response
        {
            Data = result,
        };
    }

    private static MethodInfo? GetMethodToRun(Request request)
    {
        // TODO: To much type parse... Smells..
        var types = request.Arguments.Select(a => Type.GetType(a.Type)).ToArray();
        
        return typeof(T).GetMethod(
            request.MethodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
            types!
        );
    }

    private static async Task<object> RunMethodAsync(object service, MethodInfo methodInfo, object[] parameters)
    {
        var task = (Task)methodInfo.Invoke(service, parameters)!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty!.GetValue(task)!;
    }

}