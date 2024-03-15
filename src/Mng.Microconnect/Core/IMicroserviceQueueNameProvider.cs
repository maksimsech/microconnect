namespace Mng.Microconnect.Core;

public interface IMicroserviceQueueNameProvider
{
    string GetMicroserviceQueueName(Type type);
}