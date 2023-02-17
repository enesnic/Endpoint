// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CommandLine;
using Endpoint.Core.Abstractions;
using Endpoint.Core.Enums;
using Endpoint.Core.Internals;
using Endpoint.Core.Messages;
using Endpoint.Core.Models.Artifacts.Files;
using Endpoint.Core.Models.Syntax;
using Endpoint.Core.Models.Syntax.Classes;
using Endpoint.Core.Models.Syntax.Constructors;
using Endpoint.Core.Models.Syntax.Fields;
using Endpoint.Core.Models.Syntax.Methods;
using Endpoint.Core.Models.Syntax.Params;
using Endpoint.Core.Models.Syntax.Types;
using Endpoint.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Endpoint.Cli.Commands;


[Verb("service-bus-message-consumer-create")]
public class ServiceBusMessageConsumerCreateRequest : IRequest
{
    [Option('n')]
    public string Name { get; set; } = "ServiceBusMessageConsumer";

    [Option('m')]
    public string MessagesNamespace { get; set; }

    [Option('d', Required = false)]
    public string Directory { get; set; } = System.Environment.CurrentDirectory;
}

public class ServiceBusMessageConsumerCreateRequestHandler : IRequestHandler<ServiceBusMessageConsumerCreateRequest>
{
    private readonly ILogger<ServiceBusMessageConsumerCreateRequestHandler> _logger;
    private readonly IArtifactGenerationStrategyFactory _artifactGenerationStrategyFactory;
    private readonly Observable<INotification> _notificationListener;
    private readonly IFileProvider _fileProvider;
    private readonly INamespaceProvider _namespaceProvider;
    private readonly IDomainDrivenDesignFileService _domainDrivenDesignFileService;
    public ServiceBusMessageConsumerCreateRequestHandler(
        ILogger<ServiceBusMessageConsumerCreateRequestHandler> logger,
        IDomainDrivenDesignFileService domainDrivenDesignFileService,
        IArtifactGenerationStrategyFactory artifactGenerationStrategyFactory,
        Observable<INotification> notificationListener,
        IFileProvider fileProvider,
        INamespaceProvider namespaceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artifactGenerationStrategyFactory = artifactGenerationStrategyFactory ?? throw new ArgumentNullException(nameof(artifactGenerationStrategyFactory));
        _notificationListener = notificationListener ?? throw new ArgumentNullException(nameof(notificationListener));
        _namespaceProvider = namespaceProvider;
        _fileProvider = fileProvider;
        _domainDrivenDesignFileService = domainDrivenDesignFileService ?? throw new ArgumentNullException(nameof(domainDrivenDesignFileService));
    }

    public async Task Handle(ServiceBusMessageConsumerCreateRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handled: {0}", nameof(ServiceBusMessageConsumerCreateRequestHandler));

        var classModel = new ClassModel(request.Name);

        if (string.IsNullOrEmpty(request.MessagesNamespace))
        {
            var projectDirectory = Path.GetDirectoryName(_fileProvider.Get("*.csproj", request.Directory));

            var projectNamespace = _namespaceProvider.Get(projectDirectory);

            request.MessagesNamespace = $"{projectNamespace.Split('.').First()}.Core.Messages";
        }

        classModel.Implements.Add(new TypeModel("BackgroundService"));

        classModel.UsingDirectives.Add(new UsingDirectiveModel() { Name = "MediatR" });

        classModel.UsingDirectives.Add(new UsingDirectiveModel() { Name = "Messaging" });

        classModel.UsingDirectives.Add(new UsingDirectiveModel() { Name = "Newtonsoft.Json" });

        classModel.UsingDirectives.Add(new UsingDirectiveModel() { Name = "Microsoft.Extensions.Hosting" });

        classModel.UsingDirectives.Add(new UsingDirectiveModel() { Name = "Microsoft.Extensions.Logging" });

        classModel.UsingDirectives.Add(new UsingDirectiveModel() { Name = "System.Text" });

        var ctor = new ConstructorModel(classModel, classModel.Name);

        foreach (var type in new TypeModel[] { TypeModel.LoggerOf("ServiceBusMessageConsumer"), new TypeModel("IMediator"), new TypeModel("IUdpClientFactory") })
        {
            var propName = type.Name switch
            {
                "ILogger" => "logger",
                "IUdpClientFactory" => "udpClientFactory",
                "IMediator" => "mediator"
            };

            classModel.Fields.Add(new FieldModel()
            {
                Name = $"_{propName}",
                Type = type
            });

            ctor.Params.Add(new ParamModel()
            {
                Name = propName,
                Type = type
            });
        }

        classModel.Fields.Add(new FieldModel()
        {
            Name = $"_supportedMessageTypes",
            Type = new TypeModel("string[]"),
            DefaultValue = "new string[] { }"
        });

        var methodBody = new string[]
        {
            "var client = _udpClientFactory.Create();",

            "",

            "while(!stoppingToken.IsCancellationRequested) {",

            "",

            "var result = await client.ReceiveAsync(stoppingToken);".Indent(1),

            "",

            "var json = Encoding.UTF8.GetString(result.Buffer);".Indent(1),

            "",

            "var message = System.Text.Json.JsonSerializer.Deserialize<ServiceBusMessage>(json)!;".Indent(1),

            "",

            "var messageType = message.MessageAttributes[\"MessageType\"];".Indent(1),

            "",

            "if(_supportedMessageTypes.Contains(messageType))".Indent(1),

            "{".Indent(1),

            new StringBuilder()
            .Append("var type = Type.GetType($\"")
            .Append(request.MessagesNamespace)
            .Append(".{messageType}\");")
            .ToString()
            .Indent(2),

            "",

            "var request = (IRequest)System.Text.Json.JsonSerializer.Deserialize(message.Body, type!)!;".Indent(2),

            "",

            "await _mediator.Send(request, stoppingToken);".Indent(2),

            "}".Indent(1),

            "",

            "await Task.Delay(300);".Indent(1),

            "}",
        };

        var method = new MethodModel
        {
            Name = "ExecuteAsync",
            Override = true,
            AccessModifier = AccessModifier.Protected,
            Async = true,
            ReturnType = new TypeModel("Task"),
            Body = string.Join(Environment.NewLine, methodBody)
        };

        method.Params.Add(new ParamModel()
        {
            Name = "stoppingToken",
            Type = new TypeModel("CancellationToken")
        });

        classModel.Constructors.Add(ctor);

        classModel.Methods.Add(method);

        _artifactGenerationStrategyFactory.CreateFor(new ObjectFileModel<ClassModel>(classModel, classModel.UsingDirectives, classModel.Name, request.Directory, "cs"));

        _notificationListener.Broadcast(new WorkerFileCreated(classModel.Name, request.Directory));


    }
}
