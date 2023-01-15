﻿using Endpoint.Core.Abstractions;
using Endpoint.Core.Services;
using Endpoint.Core.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;

namespace Endpoint.Core.Models.Syntax.Methods.RequestHandlerMethodBodies;


public class CreateCommandHandlerMethodGenerationStrategy : MethodSyntaxGenerationStrategy
{
    private readonly INamingConventionConverter _namingConventionConverter;
    public CreateCommandHandlerMethodGenerationStrategy(
        IServiceProvider serviceProvider, 
        INamingConventionConverter namingConventionConverter,
        ILogger<MethodSyntaxGenerationStrategy> logger) 
        : base(serviceProvider, logger)
    {
        _namingConventionConverter = namingConventionConverter ?? throw new ArgumentNullException(nameof(namingConventionConverter));
    }
    public override bool CanHandle(object model, dynamic configuration = null)
        => model is MethodModel methodModel 
        && methodModel.Name == "Handle" 
        && methodModel.Params.FirstOrDefault()?.Name == "request"
        && methodModel.Params.FirstOrDefault().Type.Name.StartsWith("Create");

    public override int Priority => int.MaxValue;

    public override string Create(ISyntaxGenerationStrategyFactory syntaxGenerationStrategyFactory, MethodModel model, dynamic configuration = null)
    {
        var builder = new StringBuilder();

        var entityName = model.ParentType.Name;

        builder.AppendLine($"var {((Token)entityName).CamelCase} = new {((Token)entityName).PascalCase}();");

        builder.AppendLine("");

        builder.AppendLine($"_context.{((Token)entityName).PascalCasePlural}.Add({((Token)entityName).CamelCase});");

        builder.AppendLine("");

        foreach (var property in model.ParentType.Properties.Where(x => x.Id == false))
        {
            builder.AppendLine($"{((Token)entityName).CamelCase}.{((Token)property.Name).PascalCase} = request.{((Token)entityName).PascalCase}.{((Token)property.Name).PascalCase};");
        }

        builder.AppendLine("");

        builder.AppendLine("await _context.SaveChangesAsync(cancellationToken);");

        builder.AppendLine("");

        builder.AppendLine("return new ()");

        builder.AppendLine("{");

        builder.AppendLine($"{((Token)entityName).PascalCase} = {((Token)entityName).CamelCase}.ToDto()".Indent(1));

        builder.AppendLine("}");

        model.Body = builder.ToString();
        
        return base.Create(syntaxGenerationStrategyFactory, model);
    }
}