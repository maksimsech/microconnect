namespace Mng.Microconnect.Server.Contract;

public interface IServerMicroservice
{
    public Task<string> GetMessageAsync(string toWhom, string by);
}