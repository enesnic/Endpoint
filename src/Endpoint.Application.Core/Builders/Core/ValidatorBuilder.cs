using Endpoint.SharedKernal.Services;
using Endpoint.SharedKernal.ValueObjects;

namespace Endpoint.Application.Builders
{
    public class ValidatorBuilder 
    {
        public void Build()
        {
/*            var template = _templateLocator.Get(nameof(ValidatorBuilder));

            var tokens = new TokensBuilder()
                .With(nameof(_rootNamespace), _rootNamespace)
                .With(nameof(_applicationNamespace), _applicationNamespace)
                .With(nameof(_directory), _directory)
                .With(nameof(_namespace), _namespace)
                .With(nameof(_entityName), _entityName)
                .Build();

            var contents = _templateProcessor.Process(template, tokens);

            _fileSystem.WriteAllLines($@"{_directory.Value}/{_entityName.PascalCase}Validator.cs", contents);*/
        }
    }
}