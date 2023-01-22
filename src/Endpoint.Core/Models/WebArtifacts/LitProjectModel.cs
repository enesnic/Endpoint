using System.IO;

namespace Endpoint.Core.Models.WebArtifacts;

public class LitProjectModel {

    public LitProjectModel(string name, string rootDirectory, string prefix = null, string kind = "library", string directory = null)
    {
        Name = name;
        RootDirectory = rootDirectory;
        Kind = kind;
        Directory = directory ?? $"{RootDirectory}{Path.DirectorySeparatorChar}{name}";
        Prefix = prefix ?? (Kind == "library" ? "lib" : "app");
    }

    public string Name { get; set; }
    public string Directory { get; set; }
    public string RootDirectory { get; set; }
    public string Kind { get; set; }
    public string Prefix { get; set; }
}
