using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FexSync
{
    /// <summary>
    /// Simplistic delegate command for the demo.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        public Action CommandAction { get; set; }

        public Func<bool> CanExecuteFunc { get; set; }

        public void Execute(object parameter)
        {
            this.CommandAction();
        }

        public bool CanExecute(object parameter)
        {
            return this.CanExecuteFunc == null || this.CanExecuteFunc();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
