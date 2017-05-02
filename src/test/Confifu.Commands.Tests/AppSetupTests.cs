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

namespace Confifu.Commands.Tests
{
    public class AppSetupTests
    {
        App CreateApp(Dictionary<string, string> args = null)
        {
            var app = new App(new DictionaryConfigVariables(args ?? new Dictionary<string, string> { }));
            app.Setup().Run();
            return app;
        }

        CommandRunResult RunCommand(string cmd, Dictionary<string, string> args = null)
        {
            var app = CreateApp(args);
            return app.CommandRunner.Run(cmd);
        }

        [Fact]
        public void it_register_default_services()
        {
            var app = CreateApp();

            app.CommandRunner.ShouldNotBeNull();
        }

        [Fact]
        public void it_run_help_command()
        {
            var app = CreateApp(new Dictionary<string, string> { ["x"] = "1" });

            var result = app.CommandRunner.Run("help");

            result.Succeed.ShouldBeTrue();
        }

        [Fact]
        public void help_command_print_registered_commands()
        {
            var res = RunCommand("help");

            res.InfoLog.ShouldContain("does some amazing stuff");
        }

        [Fact]
        public void it_fails_if_required_param_not_passed()
        {
            var res = RunCommand("test1");

            res.Succeed.ShouldBeFalse();
        }

        [Fact]
        public void it_does_not_fail_if_required_param_passed()
        {
            var res = RunCommand("test1", new Dictionary<string, string>
            {
                ["p1"]="xyz"
            });

            res.Succeed.ShouldBeTrue();
        }

        [Fact]
        public void it_uses_config_vars_with_prefixed_using_task_name()
        {
            var res = RunCommand("test1", new Dictionary<string, string>
            {
                ["Commands:Test1:p1"] = "xyz"
            });

            res.Succeed.ShouldBeTrue();
        }
    }

    class App : Confifu.AppSetup
    {
        public IContainer Container { get; private set; }
        public IServiceProvider Sp => AppConfig.GetServiceProvider();
        public ICommandRunner CommandRunner => AppConfig.GetCommandRunner();

        public App(IConfigVariables env) : base(env)
        {
            Configure(() =>
            {
                AppConfig
                    .RegisterCommonServices()
                    .UseCommands(c =>
                    {
                        c.RegisterCommand<TestCommand1>();
                    });
            });

            Configure(() =>
            {
                AppConfig.AddAppRunnerAfter(() =>
                {
                    Container = AppConfig.SetupAutofacContainer();
                });
            });
        }
    }

    class TestCommand1 : ICommand
    {
        public CommandDefinition Definition() => new CommandDefinition(
            "Test1",
            "does some amazing stuff",
            new List<ParameterDefinition> {
                new ParameterDefinition("p1", true, "", "some required parameter"),
                new ParameterDefinition("p2", false, "v2", "optional value"),
            }
            );

        public void Run(CommandRunContext context)
        {
            context.Info.WriteLine("some info");
        }
    }
}
