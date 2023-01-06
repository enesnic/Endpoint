﻿namespace Endpoint.Core.Abstractions;

public interface ISyntaxGenerationStrategy
{
    bool CanHandle(object model, dynamic configuration = null);
    string Create(object model, dynamic configuration = null);
    int Priority { get; }
}
