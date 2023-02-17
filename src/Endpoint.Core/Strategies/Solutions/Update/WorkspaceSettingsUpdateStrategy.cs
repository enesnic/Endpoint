// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Endpoint.Core.Models.Options;
using Endpoint.Core.Services;
using System.IO;
using System.Text.Json;

namespace Endpoint.Core.Strategies.WorkspaceSettingss.Update
{
    public class WorkspaceSettingsUpdateStrategy : IWorkspaceSettingsUpdateStrategy
    {
        private readonly IFileSystem _fileSystem;

        public WorkspaceSettingsUpdateStrategy(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public int Order { get; set; } = 0;

        public bool CanHandle(WorkspaceSettingsModel previous, WorkspaceSettingsModel next) => true;

        public void Update(WorkspaceSettingsModel previous, WorkspaceSettingsModel next)
        {
            var json = JsonSerializer.Serialize(next, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            _fileSystem.WriteAllText($"{next.Directory}{Path.DirectorySeparatorChar}Workspace.json", json);
        }
    }
}

