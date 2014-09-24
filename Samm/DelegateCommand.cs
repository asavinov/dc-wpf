using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

// Copied from here: http://kentb.blogspot.de/search?q=DelegateCommand
// And mentioined also here: http://stackoverflow.com/questions/3476686/how-does-one-disable-a-button-in-wpf-using-the-mvvm-pattern
namespace Samm
{
    /// <summary>
    /// A command that executes delegates to determine whether the command can execute, and to execute the command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This command implementation is useful when the command simply needs to execute a method on a view model. The delegate for
    /// determining whether the command can execute is optional. If it is not provided, the command is considered always eligible
    /// to execute.
    /// </para>
    /// </remarks>
    public class DelegateCommand : Command
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        /// <summary>
        /// Constructs an instance of <c>DelegateCommand</c>.
        /// </summary>
        /// <remarks>
        /// This constructor creates the command without a delegate for determining whether the command can execute. Therefore, the
        /// command will always be eligible for execution.
        /// </remarks>
        /// <param name="execute">
        /// The delegate to invoke when the command is executed.
        /// </param>
        public DelegateCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Constructs an instance of <c>DelegateCommand</c>.
        /// </summary>
        /// <param name="execute">
        /// The delegate to invoke when the command is executed.
        /// </param>
        /// <param name="canExecute">
        /// The delegate to invoke to determine whether the command can execute.
        /// </param>
        public DelegateCommand(Action<object> execute, Predicate<object> canExecute)
        {
            //execute.AssertNotNull("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether this command can execute.
        /// </summary>
        /// <remarks>
        /// If there is no delegate to determine whether the command can execute, this method will return <see langword="true"/>. If a delegate was provided, this
        /// method will invoke that delegate.
        /// </remarks>
        /// <param name="parameter">
        /// The command parameter.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the command can execute, otherwise <see langword="false"/>.
        /// </returns>
        public override bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute(parameter);
        }

        /// <summary>
        /// Executes this command.
        /// </summary>
        /// <remarks>
        /// This method invokes the provided delegate to execute the command.
        /// </remarks>
        /// <param name="parameter">
        /// The command parameter.
        /// </param>
        public override void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    /// <summary>
    /// A base class for command implementations.
    /// </summary>
    public abstract class Command : ICommand
    {
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// Constructs an instance of <c>CommandBase</c>.
        /// </summary>
        protected Command()
        {
            if (Application.Current != null)
            {
                _dispatcher = Application.Current.Dispatcher;
            }
            else
            {
                //this is useful for unit tests where there is no application running
                _dispatcher = Dispatcher.CurrentDispatcher;
            }

            Debug.Assert(_dispatcher != null);
        }

        /// <summary>
        /// Occurs whenever the state of the application changes such that the result of a call to <see cref="CanExecute"/> may return a different value.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether this command can execute.
        /// </summary>
        /// <param name="parameter">
        /// The command parameter.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the command can execute, otherwise <see langword="false"/>.
        /// </returns>
        public abstract bool CanExecute(object parameter);

        /// <summary>
        /// Executes this command.
        /// </summary>
        /// <param name="parameter">
        /// The command parameter.
        /// </param>
        public abstract void Execute(object parameter);

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.Invoke((ThreadStart)OnCanExecuteChanged, DispatcherPriority.Normal);
            }
            else
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
