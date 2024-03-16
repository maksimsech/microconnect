using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Mng.Microconnect.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mng.Microconnect.Core.Client;

internal sealed class MicroserviceInterceptor<T> : IMicroserviceInterceptor<T>, IInterceptor, IDisposable
{
    private readonly ConcurrentDictionary<string, MessageTaskCompletionSource> _completionSources = new();
    private readonly IModel _channel;
    private readonly IMessageSerializer _messageSerializer;
    private readonly string _replyQueueName;
    private readonly string _microserviceQueueName;

    public MicroserviceInterceptor(
        IModelProvider modelProvider, 
        IMessageSerializer messageSerializer,
        IMicroserviceQueueNameProvider microserviceQueueNameProvider
    )
    {
        _channel = modelProvider.GetModel();
        _messageSerializer = messageSerializer;

        _replyQueueName = _channel.QueueDeclare().QueueName;
        _microserviceQueueName = microserviceQueueNameProvider.GetMicroserviceQueueName(typeof(T));

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += ConsumerOnReceived;
        
        _channel.BasicConsume(consumer, _replyQueueName, true);
    }

    public void Intercept(IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;

        if (returnType.GetGenericTypeDefinition() != typeof(Task<>) || !returnType.IsGenericType)
        {
            throw new NotImplementedException("Method should return generic task.");
        }
        
        // TODO: Configure continuation to run on separate thread
        var tcsType = typeof(TaskCompletionSource<>)
            .MakeGenericType(returnType.GetGenericArguments()[0]);
        var tcs = Activator.CreateInstance(tcsType, new object[] { TaskCreationOptions.RunContinuationsAsynchronously });
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
            exchange: "",
            routingKey: _microserviceQueueName,
            basicProperties: props,
            body: GetRequestBody(invocation));

        _completionSources.TryAdd(correlationId, new MessageTaskCompletionSource(tcsType, tcs!));
    }
    
    private Task ConsumerOnReceived(object? sender, BasicDeliverEventArgs e)
    {
        if (!_completionSources.TryRemove(e.BasicProperties.CorrelationId, out var tsc))
        {
            // TODO: Something happened. Log
            return Task.CompletedTask;
        }

        var body = e.Body;
        var response = _messageSerializer.DeserializeResponse(body);

        // TODO: Better exception handling
        if (response.Exception is not null)
        {
            tsc.SetException(new Exception(response.Exception));
            return Task.CompletedTask;
        }
        
        tsc.SetResult(response.Data!);
        
        return Task.CompletedTask;
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
    
    private ReadOnlyMemory<byte> GetRequestBody(IInvocation invocation) => _messageSerializer.SerializeRequest(new Request
    {
        MethodName = invocation.Method.Name,
        Arguments = invocation.Arguments,
    });

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