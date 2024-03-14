using RabbitMQ.Client;

namespace Mng.Microconnect.RabbitMq;

public interface IModelProvider
{
    IModel GetModel();
}