namespace Mng.Microconnect.Extensions;

public interface IMicroconnectBuilder
{
    IMicroconnectBuilder AddClient<TInterface>() where TInterface : class;
    IMicroconnectBuilder AddRabbitMq();
}