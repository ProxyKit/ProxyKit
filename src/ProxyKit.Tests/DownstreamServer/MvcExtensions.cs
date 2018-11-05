using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.DownstreamServer
{
    internal static class MvcExtensions
    {
        internal static void UseSpecificControllers(
            this ApplicationPartManager partManager,
            params Type[] controllerTypes)
        {
            partManager.FeatureProviders.Add(new InternalControllerFeatureProvider());
            partManager.ApplicationParts.Clear();
            partManager.ApplicationParts.Add(new SelectedControllersApplicationParts(controllerTypes));
        }

        internal static IMvcCoreBuilder UseSpecificControllers(
            this IMvcCoreBuilder mvcCoreBuilder,
            params Type[] controllerTypes) => mvcCoreBuilder
                .ConfigureApplicationPartManager(partManager => partManager.UseSpecificControllers(controllerTypes));

        private class SelectedControllersApplicationParts : ApplicationPart, IApplicationPartTypeProvider
        {
            public SelectedControllersApplicationParts(Type[] types)
            {
                Types = types.Select(x => x.GetTypeInfo()).ToArray();
            }

            public override string Name { get; } = "Only allow selected controllers";

            public IEnumerable<TypeInfo> Types { get; }
        }

        private class InternalControllerFeatureProvider : ControllerFeatureProvider
        {
            private const string ControllerTypeNameSuffix = "Controller";

            protected override bool IsController(TypeInfo typeInfo)
            {
                if (!typeInfo.IsClass)
                {
                    return false;
                }

                if (typeInfo.IsAbstract)
                {
                    return false;
                }

                if (typeInfo.ContainsGenericParameters)
                {
                    return false;
                }

                if (typeInfo.IsDefined(typeof(Microsoft.AspNetCore.Mvc.NonControllerAttribute)))
                {
                    return false;
                }

                if (!typeInfo.Name.EndsWith(ControllerTypeNameSuffix, StringComparison.OrdinalIgnoreCase) &&
                    !typeInfo.IsDefined(typeof(Microsoft.AspNetCore.Mvc.ControllerAttribute)))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
