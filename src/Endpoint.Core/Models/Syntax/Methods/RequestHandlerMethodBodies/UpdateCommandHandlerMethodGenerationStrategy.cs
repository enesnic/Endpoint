﻿using Endpoint.Core.Abstractions;
using Endpoint.Core.Models.Syntax.Classes;
using Endpoint.Core.Services;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;

namespace Endpoint.Core.Models.Syntax.Methods.RequestHandlerMethodBodies;

public class UpdateCommandHandlerMethodGenerationStrategy : MethodSyntaxGenerationStrategy
{
    private readonly INamingConventionConverter _namingConventionConverter;
    public UpdateCommandHandlerMethodGenerationStrategy(
        IServiceProvider serviceProvider,
        INamingConventionConverter namingConventionConverter,
        ILogger<MethodSyntaxGenerationStrategy> logger)
        : base(serviceProvider, logger)
    {
        _namingConventionConverter = namingConventionConverter ?? throw new ArgumentNullException(nameof(namingConventionConverter));
    }


    public override bool CanHandle(object model, dynamic context = null)
    {        
        if (model is MethodModel methodModel && context?.Entity is ClassModel entity)
        {
            var types = methodModel.Params.Select(x => x.Type.Name);

            return methodModel.Name == "Handle" && methodModel.Params.FirstOrDefault().Type.Name.StartsWith($"Update{entity.Name}Request");
        }

        return false;
    }

    public override int Priority => int.MaxValue;

    public override string Create(ISyntaxGenerationStrategyFactory syntaxGenerationStrategyFactory, MethodModel model, dynamic context = null)
    {
        var entityName = context.Entity.Name;

        var entityNamePascalCasePlural = _namingConventionConverter.Convert(NamingConvention.PascalCase,entityName,pluralize: true);

        var entityNameCamelCase = _namingConventionConverter.Convert(NamingConvention.CamelCase, entityName); ;

        var builder = new StringBuilder();

        builder.AppendLine($"var {entityNameCamelCase} = await _context.{entityNamePascalCasePlural}.SingleAsync(x => x.{entityName}Id == new {entityName}Id(request.{entityName}.{entityName}Id.Value));");

        builder.AppendLine("");

        foreach (var property in model.ParentType.Properties.Where(x => x.Id == false))
        {
            builder.AppendLine($"{entityNameCamelCase}.{property.Name} = request.{entityName}.{property.Name};");
        }

        builder.AppendLine("");

        builder.AppendLine("await _context.SaveChangesAsync(cancellationToken);");

        builder.AppendLine("");

        builder.AppendLine("return new ()");

        builder.AppendLine("{");

        builder.AppendLine($"{entityName} = {entityNameCamelCase}.ToDto()".Indent(1));

        builder.AppendLine("};");

        model.Body = builder.ToString();

        return base.Create(syntaxGenerationStrategyFactory, model);
    }
}
