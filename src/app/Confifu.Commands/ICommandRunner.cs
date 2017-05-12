using Confifu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Confifu.Commands
{
    public interface ICommandRunner
    {
        CommandRunResult Run(string commandName);
    }

    public class CommandRunResult
    {
        public bool Succeed { get; private set; }
        public string Error { get; private set; }

        private CommandRunResult() { }

        public static CommandRunResult Ok()
            => new CommandRunResult { Succeed = true };

        public static CommandRunResult Fail(string error)
            => new CommandRunResult { Succeed = false, Error = error };
    }

    class CommandRunner : ICommandRunner
    {
        readonly ILookup<string, ICommand> commandsLookups;
        readonly IReadOnlyCollection<ICommand> commands;
        readonly IConfigVariables vars;
        readonly ICommandRunnerOutput output;

        public CommandRunner(
            ICommandRepository commandRepository, 
            IConfigVariables vars,
            ICommandRunnerOutput output
            )
        {
            this.output = output;
            this.vars = vars;
            this.commandsLookups = commandRepository.GetCommands()
                .ToLookup(x => x.Definition().Name, StringComparer.CurrentCultureIgnoreCase);

            this.commands = commandRepository.GetCommands();
        }

        public CommandRunResult Run(string commandName)
        {
            var command = this.commandsLookups[commandName].FirstOrDefault();

            if (command == null)
                return CommandRunResult.Fail(
                    $"Command {commandName} not found. Available commands: [{string.Join(", ", this.commands.Select(x => x.Definition().Name))}]");

            var def = command.Definition();
            var parameters = def.Parameters;

            var taskSpecificVars = new ConfigVariablesBuilder()
                .Add(vars)
                .Add(vars.WithPrefix($"Commands:{def.Name}:"))
                .Build();
            var missedRequiredParameters = parameters.Where(x => x.Required && taskSpecificVars[x.Name] == null);

            if (missedRequiredParameters.Any())
            {
                return Failed((error, info) =>
                {
                    error.WriteLine($"Missing required parameters {string.Join(", ", missedRequiredParameters.Select(x => "<" + x.Name + ">"))}");
                    new CommandHelpPrinter(info).Print(command);
                });
            }
            
            var varsWithDefaultParameters = new ConfigVariablesBuilder()
                .Add(new CommandDefinitionConfigVars(def))
                .Add(taskSpecificVars)
                .Build();

            return RunGeneric((error, info) =>
            {
                command.Run(new CommandRunContext(varsWithDefaultParameters, info, error));
            });
        }

        CommandRunResult Failed(Action<TextWriter, TextWriter> action)
        {
            action(output.GetErrorWriter(), output.GetInfoWriter());

            return CommandRunResult.Fail("");
        }

        CommandRunResult RunGeneric(Action<TextWriter, TextWriter> action)
        {
            var errorWriter = this.output.GetErrorWriter();
            var infoWriter = this.output.GetInfoWriter();
            try
            {
                action(errorWriter, infoWriter);
                return CommandRunResult.Ok();
            }
            catch (Exception ex)
            {
                infoWriter.WriteLine("Exception occurred:");
                infoWriter.WriteLine(ex);

                return CommandRunResult.Fail("");
            }
        }
    }

    class CommandDefinitionConfigVars : IConfigVariables
    {
        public string this[string key] => this.defaultParametersLookup[key].FirstOrDefault();

        readonly CommandDefinition commandDefinition;
        readonly ILookup<string, string> defaultParametersLookup;

        public CommandDefinitionConfigVars(CommandDefinition commandDefinition)
        {
            this.commandDefinition = commandDefinition;

            this.defaultParametersLookup = commandDefinition.Parameters.ToLookup(x => x.Name, x => x.DefaultValue);
        }
    }

    class CommandHelpPrinter
    {
        readonly TextWriter writer;

        public CommandHelpPrinter(TextWriter writer)
        {
            this.writer = writer;
        }

        public void Print(ICommand command)
        {
            var def = command.Definition();
            this.writer.WriteLine($"  {def.Name}:");

            this.writer.WriteLine($"    {def.Help}");

            this.writer.WriteLine("  Command Parameters:");

            foreach (var parameter in def.Parameters)
            {
                this.writer.WriteLine($"    <{parameter.Name}>:");
                var requiredStr = parameter.Required ? "Required!" : "Optional";
                var defaultValueStr = string.IsNullOrEmpty(parameter.DefaultValue) ? "<empty>" : parameter.DefaultValue;

                this.writer.WriteLine($"      {parameter.Help}");
                this.writer.WriteLine($"      {requiredStr}, DefaultValue: {defaultValueStr}");
            }
            
            this.writer.WriteLine();
        }
    }

    class HelpCommand : ICommand
    {
        readonly Func<ICommandRepository> commandRepositoryThunk;

        public HelpCommand(Func<ICommandRepository> commandRepositoryThunk)
        {
            this.commandRepositoryThunk = commandRepositoryThunk;
        }

        public CommandDefinition Definition()
            => new CommandDefinition("help", @"prints help info", new List<ParameterDefinition> {});

        public void Run(CommandRunContext context)
        {
            context.Info.WriteLine("Usage: %host% <command> [parameters]");
            //context.Info.WriteLine("Use: amin <command> --help to print command's help");

            context.Info.WriteLine("Available commands: ");
            context.Info.WriteLine();

            foreach (var command in commandRepositoryThunk().GetCommands())
            {
                new CommandHelpPrinter(context.Info).Print(command);
            }
        }
    }

    public interface ICommandRunnerOutput
    {
        TextWriter GetInfoWriter();
        TextWriter GetErrorWriter();
    }

    class NullCommandRunnerOutput : ICommandRunnerOutput
    {
        public TextWriter GetErrorWriter() => new StringWriter();
        public TextWriter GetInfoWriter() => new StringWriter();
    }
}
