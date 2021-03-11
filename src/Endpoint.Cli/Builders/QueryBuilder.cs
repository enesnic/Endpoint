using Endpoint.Cli.Services;
using Endpoint.Cli.ValueObjects;

namespace Endpoint.Cli.Builders
{
    public class QueryBuilder
    {
        private readonly ICommandService _commandService;
        private readonly ITemplateProcessor _templateProcessor;
        private readonly ITemplateLocator _templateLocator;
        private readonly IFileSystem _fileSystem;

        private Token _directory = (Token)System.Environment.CurrentDirectory;
        private Token _rootNamespace;
        private Token _name;
        private Token _entity;
        
        public QueryBuilder(
            ICommandService commandService,
            ITemplateProcessor templateProcessor,
            ITemplateLocator templateLocator,
            IFileSystem fileSystem)
        {
            _commandService = commandService;
            _templateProcessor = templateProcessor;
            _templateLocator = templateLocator;
            _fileSystem = fileSystem;
        }

        public QueryBuilder SetDirectory(string directory)
        {
            _directory = (Token)directory;
            return this;
        }

        public QueryBuilder SetRootNamespace(string rootNamespace)
        {
            _rootNamespace = (Token)rootNamespace;
            return this;
        }

        public QueryBuilder WithEntity(string entity)
        {
            _entity = (Token)entity;
            return this;
        }

        public QueryBuilder WithName(string name)
        {
            _name = (Token)name;
            return this;
        }

        public void Build()
        {
            var template = _templateLocator.Get(nameof(QueryBuilder ));

            var tokens = new SimpleTokensBuilder()
                .WithToken("entity",_entity)
                .WithToken("name",_name)
                .WithToken(nameof(_rootNamespace), _rootNamespace)
                .WithToken(nameof(_directory), _directory)
                .WithToken("Namespace", (Token)$"{_rootNamespace.Value}.Api.Features")
                .Build();

            var contents = _templateProcessor.Process(template, tokens);

            _fileSystem.WriteAllLines($@"{_directory.Value}/{_name.PascalCase}.cs", contents);
        }
    }
}
