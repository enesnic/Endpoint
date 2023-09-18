// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Endpoint.Core.Artifacts.Projects;

namespace Endpoint.Core.Artifacts.Solutions;

public class DependsOnModel : ArtifactModel
{
    public DependsOnModel(ProjectModel client, ProjectModel service)
    {
        Client = client;
        Service = service;
    }

    public ProjectModel Client { get; init; }

    public ProjectModel Service { get; init; }
}
