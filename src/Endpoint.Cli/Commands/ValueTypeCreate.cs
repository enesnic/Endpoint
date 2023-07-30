// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CommandLine;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Endpoint.Core.Abstractions;
using Endpoint.Core.Models.Artifacts.Files;
using System.Collections.Generic;
using Endpoint.Core.Services;

namespace Endpoint.Cli.Commands;


[Verb("value-type-create")]
public class ValueTypeCreateRequest : IRequest {
    [Option('n',"name")]
    public string Name { get; set; }

    [Option('v', "value")]
    public string Value { get; set; }

    [Option('d', Required = false)]
    public string Directory { get; set; } = System.Environment.CurrentDirectory;
}

public class ValueTypeCreateRequestHandler : IRequestHandler<ValueTypeCreateRequest>
{
    private readonly ILogger<ValueTypeCreateRequestHandler> _logger;
    private readonly IArtifactGenerationStrategyFactory _artifactGenerationStrategyFactory;
    private readonly INamingConventionConverter _namingConventionConverter;

    public ValueTypeCreateRequestHandler(
        ILogger<ValueTypeCreateRequestHandler> logger,
        IArtifactGenerationStrategyFactory artifactGenerationStrategyFactory,
        INamingConventionConverter namingConventionConverter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artifactGenerationStrategyFactory = artifactGenerationStrategyFactory ?? throw new ArgumentNullException(nameof(artifactGenerationStrategyFactory));
        _namingConventionConverter = namingConventionConverter ?? throw new ArgumentNullException(nameof(namingConventionConverter));
    }

    public async Task Handle(ValueTypeCreateRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handled: {0}", nameof(ValueTypeCreateRequestHandler));

        var tokens = new TokensBuilder()
            .With(nameof(request.Name), request.Name)
            .With(nameof(request.Value), request.Value)
            .With("namespace", "System")
            .Build();

        var model = new TemplatedFileModel("ValueType", $"{_namingConventionConverter.Convert(NamingConvention.PascalCase, request.Name)}Type", request.Directory, "cs", tokens);

        _artifactGenerationStrategyFactory.CreateFor(model);
    }
}
