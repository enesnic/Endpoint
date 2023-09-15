// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Endpoint.Core.Artifacts;
using Endpoint.Core.Artifacts.Files;
using Endpoint.Core.Internals;
using Endpoint.Core.Syntax.Classes;
using Endpoint.Core.Syntax.Constructors;
using Endpoint.Core.Syntax.Fields;
using Endpoint.Core.Syntax.Interfaces;
using Endpoint.Core.Syntax.Methods;
using Endpoint.Core.Syntax.Params;
using Endpoint.Core.Syntax.Properties;
using Endpoint.Core.Syntax.Types;
using MediatR;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Endpoint.Core.Services;

public class DomainDrivenDesignFileService : IDomainDrivenDesignFileService
{

    private readonly IArtifactGenerator _artifactGenerator;
    private readonly IFileProvider _fileProvider;
    private readonly INamingConventionConverter _namingConventionConverter;
    private readonly ISyntaxGenerator _syntaxGenerator;
    private readonly IFileSystem _fileSystem;
    private readonly INamespaceProvider _namespaceProvider;

    public DomainDrivenDesignFileService(
        INamespaceProvider namespaceProvider,
        IFileSystem fileSystem,
        IArtifactGenerator artifactGenerator,
        IFileProvider fileProvider,
        INamingConventionConverter namingConventionConverter,
        ISyntaxGenerator syntaxGenerator)
    {
        _artifactGenerator = artifactGenerator ?? throw new ArgumentNullException(nameof(artifactGenerator));
        _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        _namingConventionConverter = namingConventionConverter ?? throw new ArgumentNullException(nameof(namingConventionConverter));
        _syntaxGenerator = syntaxGenerator ?? throw new ArgumentNullException(nameof(syntaxGenerator));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _namespaceProvider = namespaceProvider ?? throw new ArgumentNullException(nameof(namespaceProvider));
    }

    public async Task MessageCreate(string name, List<PropertyModel> properties, string directory)
    {
        var classModel = new ClassModel(name);

        classModel.Properties.AddRange(properties);

        classModel.Usings.Add(new("MediatR"));

        var constructorModel = new ConstructorModel(classModel, classModel.Name);

        foreach (var property in properties)
        {
            classModel.Fields.Add(new()
            {
                Name = $"_{_namingConventionConverter.Convert(NamingConvention.CamelCase, property.Name)}",
                Type = property.Type
            });

            constructorModel.Params.Add(new()
            {
                Name = $"{_namingConventionConverter.Convert(NamingConvention.CamelCase, property.Name)}",
                Type = property.Type
            });
        }

        classModel.Constructors.Add(constructorModel);

        classModel.Implements.Add(new("IRequest"));

        var classFileModel = new CodeFileModel<ClassModel>(classModel, classModel.Usings, classModel.Name, directory, ".cs");

        await _artifactGenerator.GenerateAsync(classFileModel);

    }

    public async Task MessageHandlerCreate(string name, string directory)
    {
        var messageName = $"{name}Message";

        var messageHandlerName = $"{messageName}Handler";

        var classModel = new ClassModel(messageHandlerName);

        classModel.Usings.Add(new UsingModel() { Name = "MediatR" });

        classModel.Fields = new List<FieldModel>()
        {
            FieldModel.LoggerOf(name)
        };

        var constructorModel = new ConstructorModel(classModel, classModel.Name)
        {
            Params = new List<ParamModel>()
            {
                ParamModel.LoggerOf(name)
            }
        };

        foreach (var typeModel in new List<TypeModel>() { })
        {
            classModel.Fields.Add(new FieldModel()
            {
                Name = $"_{_namingConventionConverter.Convert(NamingConvention.CamelCase, await _syntaxGenerator.GenerateAsync(typeModel))}",
                Type = typeModel
            });

            constructorModel.Params.Add(new ParamModel()
            {
                Name = $"{_namingConventionConverter.Convert(NamingConvention.CamelCase, await _syntaxGenerator.GenerateAsync(typeModel))}",
                Type = typeModel
            });
        }

        classModel.Constructors.Add(constructorModel);

        classModel.Implements.Add(new TypeModel("IRequestHandler")
        {
            GenericTypeParameters = new List<TypeModel>
            {
                new TypeModel(messageName)
            }
        });

        var methodModel = new MethodModel()
        {
            ReturnType = TypeModel.Task,
            Async = true,
            Name = "Handle",
            AccessModifier = AccessModifier.Public,
            Params = new List<ParamModel>()
            {
                new ParamModel()
                {
                    Type = new TypeModel(messageName),
                    Name = "message"
                },
                ParamModel.CancellationToken
            },
            Body = new Syntax.Expressions.ExpressionModel(new StringBuilder().AppendLine("_logger.LogInformation(\"Message Handled: {message}\", message);").ToString())
        };

        classModel.Methods.Add(methodModel);

        var classFileModel = new CodeFileModel<ClassModel>(classModel, classModel.Usings, classModel.Name, directory, ".cs");

        await _artifactGenerator.GenerateAsync(classFileModel);

    }

    public async Task ServiceBusMessageConsumerCreate(string name = "ServiceBusMessageConsumer", string messagesNamespace = null, string directory = null)
    {

        var classModel = new ClassModel(name);

        if (string.IsNullOrEmpty(messagesNamespace))
        {
            var projectDirectory = Path.GetDirectoryName(_fileProvider.Get("*.csproj", directory));

            var projectNamespace = _namespaceProvider.Get(projectDirectory);

            messagesNamespace = $"{projectNamespace.Split('.').First()}.Core.Messages";
        }

        classModel.Implements.Add(new("BackgroundService"));

        classModel.Usings.Add(new("Messaging"));

        classModel.Usings.Add(new("Messaging.Udp"));

        classModel.Usings.Add(new("Microsoft.Extensions.DependencyInjection"));

        classModel.Usings.Add(new("Microsoft.Extensions.Hosting"));

        classModel.Usings.Add(new("System.Text"));

        classModel.Usings.Add(new("Microsoft.Extensions.Logging"));

        classModel.Usings.Add(new("System.Threading.Tasks"));

        classModel.Usings.Add(new("System.Threading"));

        classModel.Usings.Add(new("MediatR"));

        classModel.Usings.Add(new("System.Linq"));


        var constructorModel = new ConstructorModel(classModel, classModel.Name);

        foreach (var type in new TypeModel[] { TypeModel.LoggerOf("ServiceBusMessageConsumer"), new TypeModel("IServiceScopeFactory"), new TypeModel("IUdpClientFactory") })
        {
            var propName = type.Name switch
            {
                "ILogger" => "logger",
                "IUdpClientFactory" => "udpClientFactory",
                "IServiceScopeFactory" => "serviceScopeFactory"
            };

            classModel.Fields.Add(new()
            {
                Name = $"_{propName}",
                Type = type
            });

            constructorModel.Params.Add(new()
            {
                Name = propName,
                Type = type
            });
        }

        classModel.Fields.Add(new()
        {
            Name = $"_supportedMessageTypes",
            Type = new("string[]"),
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
            .Append(messagesNamespace)
            .Append(".{messageType}\");")
            .ToString()
            .Indent(2),

            "",

            "var request = System.Text.Json.JsonSerializer.Deserialize(message.Body, type!)!;".Indent(2),

            "",

            "using (var scope = _serviceScopeFactory.CreateScope())".Indent(2),

            "{".Indent(2),

            "var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();".Indent(3),

            "",

            "await mediator.Send(request, cancellationToken);".Indent(3),

            "}".Indent(2),

            "}".Indent(1),

            "",

            "await Task.Delay(0);".Indent(1),

            "}",
        };

        var method = new MethodModel
        {
            Name = "ExecuteAsync",
            Override = true,
            AccessModifier = AccessModifier.Protected,
            Async = true,
            ReturnType = new("Task"),
            Body = new Syntax.Expressions.ExpressionModel(string.Join(Environment.NewLine, methodBody))
        };

        method.Params.Add(ParamModel.CancellationToken);

        classModel.Constructors.Add(constructorModel);

        classModel.Methods.Add(method);

        await _artifactGenerator.GenerateAsync(new CodeFileModel<ClassModel>(classModel, classModel.Usings, classModel.Name, directory, ".cs"));
    }

    public async Task ServiceCreate(string name, string directory)
    {
        if (_fileSystem.File.Exists(Path.Combine(directory, $"{name}.cs")))
        {
            throw new Exception($"Service exists: {Path.Combine(directory, $"{name}.cs")}");
        }

        var usingDirectives = new List<UsingModel>()
        {
            new () { Name = "Microsoft.Extensions.Logging" },
            new () { Name = "System" },
            new () { Name = "System.Threading.Tasks" }
        };

        var fields = new List<FieldModel>()
        {
            FieldModel.LoggerOf(name)
        };

        var methods = new List<MethodModel>()
        {
            new ()
            {
                Name = "DoWorkAsync",
                ReturnType = TypeModel.Task,
                Async = true,
                Body = new Syntax.Expressions.ExpressionModel("_logger.LogInformation(\"DoWorkAsync\");")
            }
        };

        var @interface = await createInterface(name, methods, usingDirectives, directory);

        _ = await createClass(@interface, name, methods, usingDirectives, directory);

        async Task<InterfaceModel> createInterface(string name, List<MethodModel> methods, List<UsingModel> usings, string directory)
        {
            var @interface = new InterfaceModel($"I{name}");

            @interface.Methods = methods.Select(x => new MethodModel() { Name = x.Name, ReturnType = x.ReturnType, Async = x.Async, IsInterface = true }).ToList();

            @interface.Usings.AddRange(usings);

            var interfaceFile = new CodeFileModel<InterfaceModel>(
                @interface,
                @interface.Usings,
                @interface.Name,
                directory,
                ".cs"
                );

            await _artifactGenerator.GenerateAsync(interfaceFile);

            return @interface;
        }

        async Task<ClassModel> createClass(InterfaceModel @interface, string name, List<MethodModel> methods, List<UsingModel> usings, string directory)
        {
            var @class = new ClassModel(name);

            var constructors = new List<ConstructorModel>()
            {
                new (@class, @class.Name)
                {
                    Params = new List<ParamModel>
                    {
                        ParamModel.LoggerOf(name)
                    }
                }
            };

            @class.Constructors = constructors;

            @class.Methods = methods;

            @class.Fields = fields;

            @class.Usings.AddRange(usingDirectives);

            @class.Implements.Add(new() { Name = @interface.Name });

            var classFile = new CodeFileModel<ClassModel>(
                @class,
                @class.Usings,
                @class.Name,
                directory,
                ".cs"
                );

            await _artifactGenerator.GenerateAsync(classFile);

            return @class;
        }
    }
}

