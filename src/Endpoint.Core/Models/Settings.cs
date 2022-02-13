using Endpoint.Core.Services;
using Endpoint.Core.ValueObjects;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using static System.Text.Json.JsonSerializer;

namespace Endpoint.Core.Models
{
    public class Settings
    {
        public bool IsRoot { get; set; } = true;
        public string Prefix { get; set; } = "app";
        public IdFormat IdFormat { get; set; } = IdFormat.Long;
        public IdDotNetType IdDotNetType { get; set; } = IdDotNetType.Guid;
        public bool IsMicroserviceArchitecture { get; set; } = true;
        public List<string> Projects { get; set; } = new List<string>();
        public string RootDirectory { get; set; }
        public string SolutionName { get; set; }
        public string SolutionFileName { get; set; }
        public string ApiFullPath { get; set; }
        public string RootNamespace { get; set; }
        public string DomainNamespace { get; set; }
        public string ApplicationNamespace { get; set; }
        public string InfrastructureNamespace { get; set; }
        public string ApiNamespace { get; set; }
        public string DomainDirectory { get; set; }
        public string ApplicationDirectory { get; set; }
        public string InfrastructureDirectory { get; set; }
        public string ApiDirectory { get; set; }
        public string UnitTestsDirectory { get; set; }
        public string TestingDirectory { get; set; }
        public List<string> AppDirectories { get; set; } = new List<string>();
        public string BuildingBlocksCoreNamespace { get; set; } = "BuildingBlocks.Core";
        public string BuildingBlocksEventStoreNamespace { get; set; } = "BuildingBlocks.EventStore";
        public string SourceFolder { get; set; } = "src";
        public string TestFolder { get; set; } = "tests";
        public string DbContextName { get; set; }
        public int? Port { get; set; } = 5000;
        public int? SslPort { get; set; } = 5001;
        public List<string> Plugins { get; set; }
        public List<Entity> Entities { get; set; } = new List<Entity>();
        public List<AggregateRoot> Resources { get; set; } = new List<AggregateRoot>();

        public Settings(string name, string dbContextName, AggregateRoot resource, string directory, bool isMicroserviceArchitecture = true, List<string> plugins = default, IdFormat idFormat = IdFormat.Long, IdDotNetType idDotNetType = IdDotNetType.Guid, string prefix = "app")
            : this(name, dbContextName, new List<AggregateRoot>() { resource }, directory, isMicroserviceArchitecture, plugins, idFormat, idDotNetType, prefix)
        { }

        public Settings(string name, string dbContextName, List<AggregateRoot> resources, string directory, bool isMicroserviceArchitecture = true, List<string> plugins = default, IdFormat idFormat = IdFormat.Long, IdDotNetType idDotNetType = IdDotNetType.Guid, string prefix = "app")
        {
            name = ((Token)name).PascalCase.Replace("-", "_");
            Plugins = plugins;
            IdDotNetType = idDotNetType;
            IdFormat = idFormat;
            Prefix = prefix;

            foreach (var resource in resources)
            {
                Resources.Add(resource);
            }

            IsMicroserviceArchitecture = isMicroserviceArchitecture;

            SolutionName = name;
            SolutionFileName = $"{name}.sln";


            var parts = name.Split('.');
            DbContextName = dbContextName ?? $"{parts[parts.Length - 1]}DbContext";


            RootDirectory = $"{directory}{Path.DirectorySeparatorChar}{SolutionName}";
            RootNamespace = SolutionName;
            ApiNamespace = $"{RootNamespace}.Api";
            ApiFullPath = $"{RootDirectory}{Path.DirectorySeparatorChar}{SourceFolder}{Path.DirectorySeparatorChar}{ApiNamespace}{Path.DirectorySeparatorChar}{ApiNamespace}.csproj";
            InfrastructureNamespace = IsMicroserviceArchitecture ? $"{RootNamespace}.Api" : $"{RootNamespace}.Infrastructure";
            DomainNamespace = IsMicroserviceArchitecture ? $"{RootNamespace}.Api" : $"{RootNamespace}.Core";
            ApplicationNamespace = IsMicroserviceArchitecture ? $"{RootNamespace}.Api" : $"{RootNamespace}.Core";

            var sourceFolder = $"{RootDirectory}{Path.DirectorySeparatorChar}{SourceFolder}{Path.DirectorySeparatorChar}";
            var testsFolder = $"{RootDirectory}{Path.DirectorySeparatorChar}{TestFolder}{Path.DirectorySeparatorChar}";

            if (IsMicroserviceArchitecture)
            {
                ApiDirectory = $"{sourceFolder}{SolutionName}.Api";
                InfrastructureDirectory = $"{sourceFolder}{SolutionName}.Api";
                DomainDirectory = $"{sourceFolder}{SolutionName}.Api";
                ApplicationDirectory = $"{sourceFolder}{SolutionName}.Api";
            }
            else
            {
                ApiDirectory = $"{RootDirectory}{Path.DirectorySeparatorChar}{SourceFolder}{Path.DirectorySeparatorChar}{SolutionName}.Api";
                InfrastructureDirectory = $"{RootDirectory}{Path.DirectorySeparatorChar}{SourceFolder}{Path.DirectorySeparatorChar}{SolutionName}.Infrastructure";
                DomainDirectory = $"{RootDirectory}{Path.DirectorySeparatorChar}{SourceFolder}{Path.DirectorySeparatorChar}{SolutionName}.Core";
                ApplicationDirectory = $"{RootDirectory}{Path.DirectorySeparatorChar}{SourceFolder}{Path.DirectorySeparatorChar}{SolutionName}.Core";
            }

            UnitTestsDirectory = $"{testsFolder}{RootNamespace}.UnitTests";
            TestingDirectory = $"{testsFolder}{RootNamespace}.Testing";

        }

        public void AddResource(string resource, IFileSystem fileSystem)
        {
            if (Resources.FirstOrDefault(x => x.Name == resource) == null)
            {
                Resources = Resources.Concat(new AggregateRoot[1] { new AggregateRoot(resource) }).ToList();
            }

            fileSystem.WriteAllLines($"{RootDirectory}{Path.DirectorySeparatorChar}clisettings.json", new string[1] {
                    Serialize(this, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = true
                        })
                });
        }

        public void AddApp(string directory, IFileSystem fileSystem)
        {
            if (!AppDirectories.Contains(directory))
            {
                AppDirectories = AppDirectories.Concat(new string[1] { directory }).ToList();
            }

            fileSystem.WriteAllLines($"{RootDirectory}{Path.DirectorySeparatorChar}clisettings.json", new string[1] {
                    Serialize(this, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = true
                        })
                });
        }

        public Settings()
        {

        }

    }
}