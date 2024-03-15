using Microsoft.Extensions.Hosting;
using Mng.Microconnect.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mng.Microconnect.Core.Server;

public class MicroserviceRunner<T> : BackgroundService where T: class
{
    // TODO: Remove hard dependency on rabbitmq
    private readonly IMicroserviceQueueNameProvider _microserviceQueueNameProvider;
    private readonly IMessageSerializer _messageSerializer;
    private readonly IMicroserviceRequestRunner<T> _requestRunner;
    private readonly IModel _channel;

    public MicroserviceRunner(
        IModelProvider modelProvider,
        IMicroserviceQueueNameProvider microserviceQueueNameProvider,
        IMessageSerializer messageSerializer,
        IMicroserviceRequestRunner<T> requestRunner
    )
    {
        _microserviceQueueNameProvider = microserviceQueueNameProvider;
        _messageSerializer = messageSerializer;
        _requestRunner = requestRunner;
        _channel = modelProvider.GetModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _microserviceQueueNameProvider.GetMicroserviceQueueName(typeof(T));

        _ = _channel.QueueDeclare(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false
        );

        // _channel.QueueBind(queueName, "amq.direct", queueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += ConsumerOnReceived;

        _channel.BasicConsume(consumer, queueName, false);
        
        return Task.CompletedTask;
    }

    private async Task ConsumerOnReceived(object sender, BasicDeliverEventArgs @event)
    {
        var body = @event.Body;
        var replyProperties = _channel.CreateBasicProperties();
        replyProperties.CorrelationId = @event.BasicProperties.CorrelationId;

        Response response = null!;
        
        try
        {
            var request = _messageSerializer.DeserializeRequest(body);
            response = await _requestRunner.RunAsync(request);
        }
        catch (Exception e)
        {
            // TODO: Create some handler/formatter
            response = new Response
            {
                Exception = e.Message,
            };
        }
        finally
        {
            var responseBytes = _messageSerializer.SerializeResponse(response!);
            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: @event.BasicProperties.ReplyTo,
                basicProperties: replyProperties,
                body: responseBytes);
            _channel.BasicAck(deliveryTag: @event.DeliveryTag, multiple: false);
        }
    }
}