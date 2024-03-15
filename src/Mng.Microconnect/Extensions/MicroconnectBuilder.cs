using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mng.Microconnect.Core;
using Mng.Microconnect.Core.Client;
using Mng.Microconnect.Core.Server;
using Mng.Microconnect.RabbitMq;

namespace Mng.Microconnect.Extensions;

public sealed class MicroconnectBuilder : IMicroconnectBuilder
{
    private readonly IHostApplicationBuilder _applicationBuilder;
    private readonly ProxyGenerator _proxyGenerator;
    
    
    internal MicroconnectBuilder(IHostApplicationBuilder applicationBuilder)
    {
        _applicationBuilder = applicationBuilder;

        _applicationBuilder.Services
            .AddSingleton<IMessageSerializer, MessageSerializer>()
            .AddSingleton<IMicroserviceQueueNameProvider, MicroserviceQueueNameProvider>();
        
        _proxyGenerator = new ProxyGenerator();
    }

    public IMicroconnectBuilder AddRabbitMq()
    {
        _applicationBuilder.Services.Configure<RabbitMqOptions>(
            _applicationBuilder.Configuration.GetSection(RabbitMqOptions.DefaultSectionName)
        );
        
        _applicationBuilder.Services.AddSingleton<IModelProvider>(
            sp => new ModelProvider(sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value)
        );

        return this;
    }
    
    public IMicroconnectBuilder AddClient<TInterface>() where TInterface : class
    {
        _applicationBuilder.Services.TryAddSingleton(typeof(MicroserviceInterceptor<>));
        
        _applicationBuilder.Services.AddSingleton<TInterface>(sp =>
            _proxyGenerator.CreateInterfaceProxyWithoutTarget<TInterface>(
                sp.GetRequiredService<MicroserviceInterceptor<TInterface>>()
            )
        );

        return this;
    }

    public IMicroconnectBuilder AddServer<TInterface, TImplementation>()
        where TImplementation : class, TInterface
        where TInterface : class
    {
        _applicationBuilder.Services.AddHostedService<MicroserviceRunner<TInterface>>();
        _applicationBuilder.Services
            .AddSingleton<IMicroserviceRequestRunner<TInterface>, MicroserviceRequestRunner<TInterface>>()
            .AddScoped<TInterface, TImplementation>();

        return this;
    }
}