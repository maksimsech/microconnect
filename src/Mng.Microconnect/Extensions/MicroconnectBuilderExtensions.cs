using Microsoft.Extensions.Hosting;

namespace Mng.Microconnect.Extensions;

public static class MicroconnectBuilderExtensions
{
    public static IMicroconnectBuilder AddMicroconnect(this IHostApplicationBuilder builder)
    {
        return new MicroconnectBuilder(builder);
    }
}