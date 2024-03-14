namespace Mng.Microconnect.RabbitMq;

public class RabbitMqOptions
{
    public const string DefaultSectionName = "RabbitMq";

    public required string HostName { get; init; }
    
    public required int Port { get; init; }
}