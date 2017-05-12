using Confifu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Confifu.Commands
{
    public interface ICommand
    {
        void Run(CommandRunContext context);
        CommandDefinition Definition();
    }

    public class CommandRunContext
    {
        public IConfigVariables Vars { get; private set; }
        public TextWriter Info { get; private set; } 
        public TextWriter Error { get; private set; }

        public CommandRunContext(IConfigVariables vars, TextWriter info, TextWriter error)
        {
            Vars = vars;
            Info = info;
            Error = error;
        }
    }
    
    public class CommandDefinition
    {
        public string Name { get; private set; }
        public string Help { get; private set; }
        public List<ParameterDefinition> Parameters { get; private set; }

        public CommandDefinition(string name, string help, List<ParameterDefinition> parameters)
        {
            Name = name;
            Help = help;
            Parameters = parameters;
        }
    }

    public class ParameterDefinition
    {
        public string Name { get; private set; }
        public string DefaultValue { get; private set; }
        public bool Required { get; private set; }
        public string Help { get; private set; }

        public ParameterDefinition(string name, bool required, string defaultValue, string help)
        {
            Name = name;
            Required = required;
            Help = help;
            DefaultValue = defaultValue;
        }
    }
}
