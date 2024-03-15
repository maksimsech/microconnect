namespace Mng.Microconnect.Extensions;

public interface IMicroconnectBuilder
{
    IMicroconnectBuilder AddClient<TInterface>() where TInterface : class;
    IMicroconnectBuilder AddRabbitMq();

    IMicroconnectBuilder AddServer<TInterface, TImplementation>()
        where TImplementation : class, TInterface
        where TInterface : class;
}