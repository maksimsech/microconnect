namespace Mng.Microconnect.Core;

public class MicroserviceQueueNameProvider : IMicroserviceQueueNameProvider
{
    public string GetMicroserviceQueueName(Type type)
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
        
        return $"mc.{name.ToLower()}";

        string RemovePostfix(string postfix) => name.EndsWith(postfix)
            ? name[..^postfix.Length]
            : name;
    }
}