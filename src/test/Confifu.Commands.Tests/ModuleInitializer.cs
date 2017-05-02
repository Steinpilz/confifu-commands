using System;
using System.Collections.Generic;
using System.Text;
using Confifu.Abstractions;
using Xunit;
using Confifu.ConfigVariables;
using Confifu.Autofac;
using Autofac;
using Confifu.Abstractions.DependencyInjection;
using Shouldly;
using System.Reflection;

namespace Confifu.Commands.Tests
{

    public static class ModuleInitializer
    {
        public static void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name);
                if (name.Name == "Autofac")
                {
                    return typeof(global::Autofac.IContainer).Assembly;
                }
                return null;
            };
        }
    }
}
