// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Endpoint.Core.Models.Syntax.Methods.Factories;

public interface IMethodModelFactory
{
    MethodModel CreateControllerMethod(string name, string controller, RouteType routeType, string directory);

}

