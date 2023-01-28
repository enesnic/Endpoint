using Endpoint.Core.Abstractions;
using Endpoint.Core.Models.WebArtifacts.Commands;
using Microsoft.Extensions.Logging;

namespace Endpoint.Core.Models.WebArtifacts.Strategies;

public class AddAngularTranslateGenerationStrategy : ArtifactGenerationStrategyBase<AngularProjectReferenceModel>
{
    private readonly ILogger<AddAngularTranslateGenerationStrategy> _logger;
    public AddAngularTranslateGenerationStrategy(
        IServiceProvider serviceProvider,
        ILogger<AddAngularTranslateGenerationStrategy> logger) 
        : base(serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override bool CanHandle(object model, dynamic context = null)
        => model is AngularProjectReferenceModel && context is AngularTranslateAdd;

    public override void Create(IArtifactGenerationStrategyFactory artifactGenerationStrategyFactory, AngularProjectReferenceModel model, dynamic context = null)
    {
        _logger.LogInformation("Generating artifact for {0}.", model);

    }
}