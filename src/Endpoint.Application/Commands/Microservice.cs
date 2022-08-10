using CommandLine;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using Endpoint.Core.Factories;
using Nelibur.ObjectMapper;
using Endpoint.Core.Options;
using Endpoint.Core;
using Endpoint.Core.Strategies.Common.Git;
using Endpoint.Core.Models;
using Endpoint.Core.Strategies.Solutions.Crerate;
using Endpoint.Core.Strategies.Solutions.Update;
using Endpoint.Core.Services;
using System.IO;
using Endpoint.Core.Strategies.WorkspaceSettingss.Update;
using static Endpoint.Core.CoreConstants.SolutionTemplates;

namespace Endpoint.Application.Commands
{
    public class Microservice
    {
        [Verb("microservice")]
        public class Request : IRequest<Unit> {
            [Option('n',"name")]
            public string Name { get; set; }
            
            [Option('g')]
            public bool CreateGitRepository { get; set; } = false;

            [Option('t',"template")]
            public string TemplateType { get; set; } = CleanArchitectureByJasonTalyor;

            [Option('p', "properties")]
            public string Properties { get; set; } = Environment.GetEnvironmentVariable($"{nameof(Endpoint)}:Properties");

            [Option('r', "resource")]
            public string Resource { get; set; } = Environment.GetEnvironmentVariable($"{nameof(Endpoint)}:Resource");

            [Option("db-context-name")]
            public string DbContextName { get; set; } = Environment.GetEnvironmentVariable($"{nameof(Endpoint)}:DbContextName");

            [Option('w',"workspace-name")]
            public string WorkspaceName { get; set; }

            [Option('d', "directory")]
            public string Directory { get; set; } = System.Environment.CurrentDirectory; 
        }

        public class Handler : IRequestHandler<Request, Unit>
        {
            private readonly ILogger _logger;
            private readonly ISolutionGenerationStrategy _solutionGenerationStrategy;
            private readonly ISolutionSettingsFileGenerationStrategyFactory _factory;
            private readonly IGitGenerationStrategyFactory _gitGenerationStrategyFactory;
            private readonly IWorkspaceGenerationStrategyFactory _workspaceSettingsGenerationStrategyFactory;
            private readonly ISolutionUpdateStrategyFactory _solutionUpdateStrategyFactory;
            private readonly IWorkspaceSettingsUpdateStrategyFactory _workspaceSettingsUpdateStrategyFactory;
            private readonly IFileSystem _fileSystem;
            public Handler(ILogger logger, ISolutionGenerationStrategy solutionGenerationStrategy, ISolutionSettingsFileGenerationStrategyFactory factory, IGitGenerationStrategyFactory gitGenerationStrategyFactory, IWorkspaceGenerationStrategyFactory workspaceGenerationStrategyFactory, ISolutionUpdateStrategyFactory solutionUpdateStrategyFactory, IFileSystem fileSystem, IWorkspaceSettingsUpdateStrategyFactory workspaceSettingsUpdateStrategyFactory)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _solutionGenerationStrategy = solutionGenerationStrategy ?? throw new ArgumentNullException(nameof(solutionGenerationStrategy));
                _factory = factory ?? throw new ArgumentNullException(nameof(factory));
                _gitGenerationStrategyFactory = gitGenerationStrategyFactory ?? throw new ArgumentNullException(nameof(gitGenerationStrategyFactory));
                _workspaceSettingsGenerationStrategyFactory = workspaceGenerationStrategyFactory;
                _solutionUpdateStrategyFactory = solutionUpdateStrategyFactory;
                _fileSystem = fileSystem;
                _workspaceSettingsUpdateStrategyFactory = workspaceSettingsUpdateStrategyFactory;
            }

            public SolutionModel CreateMinimalSolutionModel(CreateCleanArchitectureMicroserviceOptions createMicroserviceOptions, Request request)
            {
                if(IsExistingWorkspace(request.Directory))
                {
                    return SolutionModelFactory.Minimal(new CreateEndpointSolutionOptions
                    {
                        Name = createMicroserviceOptions.Name,
                        Directory = createMicroserviceOptions.Directory,
                        SolutionDirectory = $"{createMicroserviceOptions.SolutionDirectory}",
                        Properties = request.Properties,
                        Resource = request.Resource,
                        DbContextName = request.DbContextName
                    });
                }

                return SolutionModelFactory.Minimal(new CreateEndpointSolutionOptions
                {
                    Name = createMicroserviceOptions.Name,
                    Directory = $"{createMicroserviceOptions.Directory}",
                    SolutionDirectory = $"{createMicroserviceOptions.Directory}{Path.DirectorySeparatorChar}{createMicroserviceOptions.Name}",
                    Properties = request.Properties,
                    Resource = request.Resource,
                    DbContextName = request.DbContextName
                });
            }
            public async Task<Unit> Handle(Request request, CancellationToken cancellationToken)
            {
                _logger.LogInformation($"Handled: {nameof(Microservice)}");

                ResolveOrCreateWorkspaceOptions resolveOrCreateWorkspaceOptions = IsExistingWorkspace(request.Directory) ? new ResolveOrCreateWorkspaceOptions() {
                    Directory = Directory.GetParent(request.Directory).FullName,
                    Name =  Path.GetFileName(request.Directory)              
                } : TinyMapper.Map<ResolveOrCreateWorkspaceOptions>(request);

                CreateCleanArchitectureMicroserviceOptions createMicroserviceOptions = IsExistingWorkspace(request.Directory) ? new CreateCleanArchitectureMicroserviceOptions() {
                    Directory = $"{Directory.GetParent(request.Directory)}",
                    SolutionDirectory = request.Directory,
                    Name = request.Name
                } : TinyMapper.Map<CreateCleanArchitectureMicroserviceOptions>(request); ;

                (WorkspaceSettingsModel previousWorkspaceSettings, WorkspaceSettingsModel nextWorkspaceSettings) = CreateOrResolvePreviousAndNextWorkspaceSettings(resolveOrCreateWorkspaceOptions);

                (SolutionModel previousWorkspaceSolutionModel, SolutionModel nextWorkspaceSolutionModel) = CreateOrResolvePreviousAndNextWorkspaceSolutions(resolveOrCreateWorkspaceOptions);

                SolutionModel solutionModel = request.TemplateType switch
                {
                    CleanArchitectureByJasonTalyor => SolutionModelFactory.CleanArchitectureMicroservice(createMicroserviceOptions),
                    Minimal => CreateMinimalSolutionModel(createMicroserviceOptions, request),
                    _ => throw new NotImplementedException()
                };

                _solutionGenerationStrategy.Create(solutionModel);

                var settings = SolutionSettingsModelFactory.Create(createMicroserviceOptions);

                _factory.CreateFor(settings);

                foreach (var project in solutionModel.Projects)
                {
                    nextWorkspaceSolutionModel.Projects.Add(project);
                }

                nextWorkspaceSettings.SolutionSettings.Add(settings);

                _solutionUpdateStrategyFactory.UpdateFor(previousWorkspaceSolutionModel, nextWorkspaceSolutionModel);

                _workspaceSettingsUpdateStrategyFactory.UpdateFor(previousWorkspaceSettings, nextWorkspaceSettings);

                if (!IsExistingWorkspace(request.Directory) && request.CreateGitRepository)
                    _gitGenerationStrategyFactory.CreateFor(new GitModel
                    {
                        Directory = solutionModel.SolutionDirectory,
                        RepositoryName = request.WorkspaceName
                    });

                return new();
            }

            public Tuple<SolutionModel, SolutionModel> CreateOrResolvePreviousAndNextWorkspaceSolutions(ResolveOrCreateWorkspaceOptions resolveOrCreateWorkspaceOptions)
            {

                if (!_fileSystem.Exists($"{resolveOrCreateWorkspaceOptions.Directory}{Path.DirectorySeparatorChar}{resolveOrCreateWorkspaceOptions.Name}{Path.DirectorySeparatorChar}Workspace.sln"))
                {
                    var newWorkspaceSolutionModel = SolutionModelFactory.Workspace(resolveOrCreateWorkspaceOptions);

                    _solutionGenerationStrategy.Create(newWorkspaceSolutionModel);

                    return new (SolutionModelFactory.Workspace(resolveOrCreateWorkspaceOptions), SolutionModelFactory.Workspace(resolveOrCreateWorkspaceOptions));
                }

                return new (SolutionModelFactory.Resolve(resolveOrCreateWorkspaceOptions), SolutionModelFactory.Resolve(resolveOrCreateWorkspaceOptions));
            }

            public Tuple<WorkspaceSettingsModel, WorkspaceSettingsModel> CreateOrResolvePreviousAndNextWorkspaceSettings(ResolveOrCreateWorkspaceOptions resolveOrCreateWorkspaceOptions)
            {

                if (!_fileSystem.Exists($"{resolveOrCreateWorkspaceOptions.Directory}{Path.DirectorySeparatorChar}{resolveOrCreateWorkspaceOptions.Name}{Path.DirectorySeparatorChar}Workspace.json"))
                {
                    _workspaceSettingsGenerationStrategyFactory.CreateFor(WorkspaceSettingsModelFactory.Create(resolveOrCreateWorkspaceOptions));

                    return new(WorkspaceSettingsModelFactory.Create(resolveOrCreateWorkspaceOptions), WorkspaceSettingsModelFactory.Create(resolveOrCreateWorkspaceOptions));
                }

                return new(WorkspaceSettingsModelFactory.Resolve(resolveOrCreateWorkspaceOptions), WorkspaceSettingsModelFactory.Resolve(resolveOrCreateWorkspaceOptions));
            }

            public bool IsExistingWorkspace(string directory) => _fileSystem.Exists($"{directory}{Path.DirectorySeparatorChar}Workspace.json");
        }
    }
}
