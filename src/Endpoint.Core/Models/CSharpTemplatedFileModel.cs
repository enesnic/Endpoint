﻿using Endpoint.Core.Models.Files;

namespace Endpoint.Core.Models
{

    public class CSharpTemplatedFileModel: TemplatedFileModel
    {
        public string Namespace { get; init; }
        public CSharpTemplatedFileModel()
        {
                
        }
    }
}
