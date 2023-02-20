// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CommandLine;
using Endpoint.Core.Abstractions;
using Endpoint.Core.Models.Artifacts.Projects;
using Endpoint.Core.Models.Artifacts.Projects.Enums;
using Endpoint.Core.Models.Artifacts.Projects.Factories;
using Endpoint.Core.Models.Artifacts.Solutions;
using Endpoint.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Endpoint.Cli.Commands;


[Verb("background-processor-create")]
public class BackgroundProcessorCreateRequest : IRequest {
    [Option('n',"name")]
    public string Name { get; set; }


    [Option('d', Required = false)]
    public string Directory { get; set; } = System.Environment.CurrentDirectory;
}

public class BackgroundProcessorCreateRequestHandler : IRequestHandler<BackgroundProcessorCreateRequest>
{
    private readonly ILogger<BackgroundProcessorCreateRequestHandler> _logger;
    private readonly ISolutionModelFactory _solutionModelFactory;
    private readonly IProjectModelFactory _projectModelFactory;
    private readonly IArtifactGenerationStrategyFactory _artifactGenerationStrategyFactory;
    private readonly ICommandService _commandService;

    public BackgroundProcessorCreateRequestHandler(
        ILogger<BackgroundProcessorCreateRequestHandler> logger,
        ISolutionModelFactory solutionModelFactory,
        IProjectModelFactory projectModelFactory,
        IArtifactGenerationStrategyFactory artifactGenerationStrategyFactory,
        ICommandService commandService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _solutionModelFactory = solutionModelFactory ?? throw new ArgumentNullException(nameof(solutionModelFactory));
        _projectModelFactory = projectModelFactory ?? throw new ArgumentNullException(nameof(projectModelFactory));
        _artifactGenerationStrategyFactory = artifactGenerationStrategyFactory ?? throw new ArgumentNullException(nameof(artifactGenerationStrategyFactory));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));


    }

    public async Task Handle(BackgroundProcessorCreateRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handled: {0}", nameof(BackgroundProcessorCreateRequestHandler));

        var model = new SolutionModel(request.Name, request.Directory);

        var src = new FolderModel("src", model.SolutionDirectory);

        var core = new ProjectModel(DotNetProjectType.ClassLib, $"{request.Name}.Core", src.Directory);

        var backgroundProcessor = new ProjectModel(DotNetProjectType.Worker, $"{request.Name}.BackgroundProcessor", src.Directory);

        backgroundProcessor.References.Add(Path.Combine("..",core.Name,$"{core.Name}.csproj"));

        src.Projects.Add(core);

        src.Projects.Add(backgroundProcessor);

        model.Folders.Add(src);

        _artifactGenerationStrategyFactory.CreateFor(model);

        _commandService.Start($"start {model.SolutionPath}");
    }
}