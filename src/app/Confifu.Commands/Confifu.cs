using Confifu.Abstractions;
using Confifu.Abstractions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Confifu.Commands
{
    public static class AppConfigExtensions
    {
        public class Config
        {
            readonly IAppConfig appConfig;

            public Config(IAppConfig appConfig)
            {
                this.appConfig = appConfig;
            }

            internal Config InitDefaults()
            {
                appConfig.RegisterServices(sc =>
                {
                    sc.Replace(ServiceDescriptor.Singleton<ICommandRepository>(sp
                        => new CommandRepository(sp.GetServices<ICommand>())
                        ));
                    sc.Replace(ServiceDescriptor.Singleton<ICommandRunner, CommandRunner>());
                    sc.AddTransient<ICommandRunnerOutput, NullCommandRunnerOutput>();
                });
                return RegisterCommand<HelpCommand>();
            }

            public Config RegisterCommand(Type commandType)
            {
                appConfig.RegisterServices(sc =>
                {
                    sc.Add(ServiceDescriptor.Transient(typeof(ICommand), commandType));
                });
                return this;
            }

            public Config RegisterCommand<TCommand>() => RegisterCommand(typeof(TCommand));

            public Config RegisterCommandsFromAssembly(Assembly assembly)
            {
                foreach (var command in new CommandsAssemblyScanner(assembly).Scan())
                    RegisterCommand(command);
                return this;
            }
        }

        class CommandsAssemblyScanner
        {
            readonly Assembly assembly;

            public CommandsAssemblyScanner(Assembly assembly)
            {
                this.assembly = assembly;
            }

            public IEnumerable<Type> Scan()
            {
                return assembly.GetTypes()
                    .Where(x => typeof(ICommand).IsAssignableFrom(x)
                                    && !x.IsAbstract && x.IsClass);
            }
        }

        public static IAppConfig UseCommands(this IAppConfig appConfig, Action<Config> configurator = null)
        {
            var config = appConfig.EnsureConfig("Commands", () => new Config(appConfig), c =>
            {
                c.InitDefaults();
            });
            configurator?.Invoke(config);
            return appConfig;
        }

        public static ICommandRunner GetCommandRunner(this IAppConfig appConfig)
            => appConfig.GetServiceProvider().GetService<ICommandRunner>();
    }
}
