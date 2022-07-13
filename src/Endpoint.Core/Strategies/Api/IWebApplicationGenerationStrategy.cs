﻿using Endpoint.Core.Models;
using System.Collections.Generic;

namespace Endpoint.Core.Strategies.Api
{
    public interface IWebApplicationGenerationStrategy
    {
        string[] Create(string @namespace, string dbContextName, List<RouteHandlerModel> routeHandlers);
        string[] Update(List<string> existingWebApplication, string @namespace, string dbContextName, List<RouteHandlerModel> routeHandlers);
    }
}