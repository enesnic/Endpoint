﻿using Endpoint.Core.Models.WebArtifacts;
using System.Collections.Generic;
using System.Linq;

namespace Endpoint.Core.Abstractions;

public class WebGenerationStrategyFactory : IWebGenerationStrategyFactory
{
    private readonly IEnumerable<IWebGenerationStrategy> _strategies;
    public WebGenerationStrategyFactory(IEnumerable<IWebGenerationStrategy> strategies)
    {
        _strategies = strategies;
    }
    public void CreateFor(WebModel model, dynamic configuration = null)
    {
        var strategy = _strategies.Where(x => x.CanHandle(model, configuration))
            .OrderBy(x => x.Priority)
            .FirstOrDefault();

        strategy.Create(model);
    }
}
