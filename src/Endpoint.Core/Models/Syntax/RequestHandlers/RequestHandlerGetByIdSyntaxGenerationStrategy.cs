// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Endpoint.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Endpoint.Core.Models.Syntax.RequestHandlers;

public class RequestHandlerGetByIdSyntaxGenerationStrategy : SyntaxGenerationStrategyBase<RequestHandlerModel>
{
    private readonly ILogger<RequestHandlerGetByIdSyntaxGenerationStrategy> _logger;
    public RequestHandlerGetByIdSyntaxGenerationStrategy(
        IServiceProvider serviceProvider,
        ILogger<RequestHandlerGetByIdSyntaxGenerationStrategy> logger)
        : base(serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override bool CanHandle(object model, dynamic context = null)
        => model is RequestHandlerModel requestHandlerModel && requestHandlerModel.RouteType == RouteType.GetById;
    public override string Create(ISyntaxGenerator syntaxGenerator, RequestHandlerModel model, dynamic context = null)
    {
        _logger.LogInformation("Generating syntax for {0} and type {1}.", model);

        var builder = new StringBuilder();


        return builder.ToString();
    }
}
