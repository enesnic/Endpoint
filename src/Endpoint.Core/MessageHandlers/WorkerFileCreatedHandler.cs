using Endpoint.Core.Messages;
using Endpoint.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Endpoint.Core.MessageHandlers;

public class WorkerFileCreatedHandler : INotificationHandler<WorkerFileCreated>
{
    private readonly IDependencyInjectionService _dependencyInjectionService;
    private readonly ILogger<WorkerFileCreatedHandler> _logger;
    public WorkerFileCreatedHandler(
        IDependencyInjectionService dependencyInjectionService,
        ILogger<WorkerFileCreatedHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dependencyInjectionService = dependencyInjectionService ?? throw new ArgumentNullException(nameof(dependencyInjectionService));
    }

    public async Task Handle(WorkerFileCreated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handled: {0}", nameof(WorkerFileCreatedHandler));

        _dependencyInjectionService.AddHosted(notification.Name, notification.Directory);
    }
}