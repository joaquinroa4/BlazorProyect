namespace ProyectoBlazor.Data;

public class DbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _services;

    public DbInitializerHostedService(IServiceProvider services)
    {
        _services = services;
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => DbInitializer.InicializarAsync(_services);

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}