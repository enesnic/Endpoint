using Endpoint.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;

namespace Endpoint.Core.Models.Syntax.Types;

public class TypeSyntaxGenerationStrategy : SyntaxGenerationStrategyBase<TypeModel>
{
    private readonly ILogger<TypeSyntaxGenerationStrategy> _logger;
    public TypeSyntaxGenerationStrategy(
        IServiceProvider serviceProvider,
        ILogger<TypeSyntaxGenerationStrategy> logger) 
        : base(serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override string Create(ISyntaxGenerationStrategyFactory syntaxGenerationStrategyFactory, TypeModel model, dynamic configuration = null)
    {
        _logger.LogInformation("Generating syntax for {0}.", model);

        var builder = new StringBuilder();

        builder.Append(model.Name);

        if(model.GenericTypeParameters.Count > 0)
        {
            builder.Append('<');

            builder.AppendJoin(',', model.GenericTypeParameters.Select(x => syntaxGenerationStrategyFactory.CreateFor(x)));

            builder.Append('>');
        }

        return builder.ToString();
    }
}