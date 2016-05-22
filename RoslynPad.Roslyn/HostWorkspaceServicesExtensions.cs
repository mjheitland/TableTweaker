using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using RoslynPad.Annotations;

namespace RoslynPad.Roslyn
{
    public static class HostWorkspaceServicesExtensions
    {
        public static IEnumerable<object> FindLanguageServices([NotNull] this HostWorkspaceServices services, Type type)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return (IEnumerable<object>)typeof(HostWorkspaceServices).GetMethod(nameof(HostWorkspaceServices.FindLanguageServices))
                .MakeGenericMethod(type)
                .Invoke(services, new object[] { new HostWorkspaceServices.MetadataFilter(x => true) });
        }

        public static object GetService([NotNull] this HostWorkspaceServices services, Type type)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return typeof(HostWorkspaceServices).GetMethod(nameof(HostWorkspaceServices.GetService))
                .MakeGenericMethod(type)
                .Invoke(services, null);
        }
    }
}