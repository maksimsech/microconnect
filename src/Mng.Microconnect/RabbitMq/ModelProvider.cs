using RabbitMQ.Client;

namespace Mng.Microconnect.RabbitMq;

public class ModelProvider : IModelProvider, IDisposable
{
    private readonly IConnection _connection;

    public ModelProvider(RabbitMqOptions options)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
        };

        _connection = connectionFactory.CreateConnection();
    }

    public IModel GetModel()
    {
        return _connection.CreateModel();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}