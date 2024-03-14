using System.Collections.Concurrent;
using System.Text.Json;
using Castle.DynamicProxy;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mng.Microconnect.Core.Client;

internal sealed class MicroserviceInterceptor : IMicroserviceInterceptor, IInterceptor, IDisposable
{
    private readonly ConcurrentDictionary<string, MessageTaskCompletionSource> _completionSources = new();
    private readonly IModel _channel;
    private readonly string _replyQueueName;
    private readonly string _microserviceQueueName;

    internal MicroserviceInterceptor(IModel channel, Type microserviceInterface)
    {
        _channel = channel;

        _replyQueueName = _channel.QueueDeclare().QueueName;
        _microserviceQueueName = GetMicroserviceQueueName(microserviceInterface);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += ConsumerOnReceived;
    }

    public void Intercept(IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;

        // if (!(typeof(Task).IsAssignableFrom(returnType) && returnType.IsGenericType))
        // {
        //     throw new NotImplementedException("Method should return generic task.");
        // }
        
        // TODO: Configure continuation to run on separate thread
        var tcsType = typeof(TaskCompletionSource<>)
            .MakeGenericType(returnType.GetGenericArguments()[0]);
        var tcs = Activator.CreateInstance(tcsType);
        invocation.ReturnValue = tcsType.GetProperty("Task")?.GetValue(tcs, null);

        var props = _channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = _replyQueueName;
        
        // TODO: Configurable, use Task.Run ?
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => CancelCall(correlationId));
        
        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _microserviceQueueName,
            basicProperties: props,
            body: GetRequestBody(invocation));

        _completionSources.TryAdd(correlationId, new MessageTaskCompletionSource(tcsType, tcs!));
    }
    
    private void ConsumerOnReceived(object? sender, BasicDeliverEventArgs e)
    {
        if (!_completionSources.TryRemove(e.BasicProperties.CorrelationId, out var tsc))
        {
            // TODO: Something happened. Log
            return;
        }
        // TODO: Process
        
        tsc.SetResult("new string()");
    }

    private void CancelCall(string correlationId)
    {
        if (!_completionSources.TryRemove(correlationId, out var tsc))
        {
            // TODO: Something happened. Log
            return;
        }
        
        tsc.SetException(new Exception());
    }

    private static string GetMicroserviceQueueName(Type type)
    {
        if (!type.IsInterface)
        {
            throw new ArgumentException();
        }

        var name = type.Name;
        name = name.StartsWith('I')
            ? name.Substring(1, name.Length - 1)
            : name;

        name = RemovePostfix("Service");
        name = RemovePostfix("Microservice");
        
        return name;

        string RemovePostfix(string postfix) => name.EndsWith(postfix)
            ? name[..^postfix.Length]
            : name;
    }

    private static byte[] GetRequestBody(IInvocation invocation) =>
        JsonSerializer.SerializeToUtf8Bytes(
            new Request
            {
                MethodName = invocation.Method.Name,
                Arguments = invocation.Arguments
                    .Select(a => new Request.RequestArgument(a.GetType().FullName!, a))
                    .ToList(),
            },
            JsonSerializerContext.Options
        );

    public void Dispose()
    {
        _channel.Dispose();
    }

    
    private readonly record struct MessageTaskCompletionSource(Type SourceType, object Source)
    {
        public void SetResult(object result)
        {
            SourceType
                .GetMethod("SetResult")!
                .Invoke(Source, new [] { result });
        }

        public void SetException(Exception exception)
        {
            SourceType
                .GetMethod("SetException", new []{ typeof(Exception) })!
                .Invoke(Source, new object[] { exception });
        }
        
    }
}