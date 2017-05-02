using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Confifu.Commands
{
    public interface ICommandRepository
    {
        IReadOnlyCollection<ICommand> GetCommands();
    }

    class CommandRepository : ICommandRepository
    {
        readonly IReadOnlyCollection<ICommand> commands;

        public CommandRepository(IEnumerable<ICommand> commands)
        {
            this.commands = new ReadOnlyCollection<ICommand>(commands.ToList());
        }

        public IReadOnlyCollection<ICommand> GetCommands()
            => this.commands;
    }

}
