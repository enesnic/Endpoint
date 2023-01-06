﻿using Endpoint.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;

namespace Endpoint.Core.Models.Syntax.Classes;

public class ClassSyntaxGenerationStrategy : SyntaxGenerationStrategyBase<ClassModel>
{
    private readonly ILogger<ClassSyntaxGenerationStrategy> _logger;
    public ClassSyntaxGenerationStrategy(
        IServiceProvider serviceProvider,
        ILogger<ClassSyntaxGenerationStrategy> logger) 
        : base(serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override string Create(ISyntaxGenerationStrategyFactory syntaxGenerationStrategyFactory, ClassModel model, dynamic configuration = null)
    {
        _logger.LogInformation("Generating syntax for {0}.", model);

        var builder = new StringBuilder();

        builder.Append(syntaxGenerationStrategyFactory.CreateFor(model.AccessModifier));

        if (model.Static)
            builder.Append(" static");

        builder.Append($" {model.Name}");

        if (model.Implements.Count > 0)
        {
            builder.Append(": ");

            builder.Append(string.Join(',', model.Implements.Select(x => syntaxGenerationStrategyFactory.CreateFor(x, configuration))));
        }

        if(model.Properties.Count + model.Methods.Count + model.Constructors.Count + model.Fields.Count == 0)
        {
            builder.Append(" { }");

            return builder.ToString();
        }

        builder.AppendLine($"");

        builder.AppendLine("{");

        builder.AppendLine(syntaxGenerationStrategyFactory.CreateFor(model.Fields, configuration).Indent(1));

        builder.AppendLine(syntaxGenerationStrategyFactory.CreateFor(model.Constructors, configuration).Indent(1));

        builder.AppendLine(syntaxGenerationStrategyFactory.CreateFor(model.Properties, configuration).Indent(1));

        builder.AppendLine(syntaxGenerationStrategyFactory.CreateFor(model.Methods, configuration).Indent(1));

        builder.AppendLine("}");
        
        return builder.ToString();
    }
}