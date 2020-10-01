using System;
using System.Windows.Input;

namespace DeltaSphereTestApp.Views.Shared
{
    /// <summary>
    /// Generic implementation for the <see cref="ICommand"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the parameter of the action to execute.</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> execute;
        private readonly Func<T, bool> canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The action to execute.</param>
        /// <param name="canExecute">Optional predicate that is executed before execute is called. If it returns true execute is called.</param>
        /// <exception cref="ArgumentNullException">Is thrown if <paramref name="execute"/> is null.</exception>
        public RelayCommand(Action<T> execute, Func<T,bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        /// <summary>
        /// <see cref="EventHandler"/> called when <see cref="CanExecute"/> has changed.
        /// </summary>
        /// <remarks>Used in <see cref="CommandBinding"/> and <see cref="InputBinding"/> To achieve the enabling / disabling functionality.</remarks>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (canExecute != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }
            remove
            {
                if (canExecute != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        /// <summary>
        /// Optional check if the Execution can take place. If it is not set Execute can always be executed.
        /// </summary>
        /// <param name="parameter">Parameter passed along to the execute predicate.</param>
        /// <returns>True if Execute can be called.</returns>
        /// <exception cref="ArgumentException">Is thrown if the type of the parameter is not the same as <see cref="T"/> or derived from <see cref="T"/>
        /// or implements interface <see cref="T"/> if it is an interface.</exception>
        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(MakeTyped(parameter));
        }

        /// <summary>
        /// Executes the execute action. If canExecute is set it first checks if execution can be performed.
        /// </summary>
        /// <param name="parameter">Passed along to the execute action.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the parameter is null.</exception>
        /// <exception cref="ArgumentException">Is thrown if the type of the parameter is not the same as <see cref="T"/> or derived from <see cref="T"/>
        /// or implements interface <see cref="T"/> if it is an interface.</exception>
        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                execute(MakeTyped(parameter));
            }
        }

        /// <summary>
        /// Make the <paramref name="parameter"/> typed. Value types are converted, reference types are cast.
        /// </summary>
        /// <param name="parameter">Object to convert into the specific type <see cref="T"/>.</param>
        /// <exception cref="ArgumentException">Is thrown if the type of the parameter is not the same as <see cref="T"/> or derived from <see cref="T"/>
        /// or implements interface <see cref="T"/> if it is an interface.</exception>
        /// <returns>The typed parameter.</returns>
        private static T MakeTyped(object parameter)
        {
            if (typeof(T).IsValueType)
            {
                return (T)Convert.ChangeType(parameter, typeof(T));
            }
            if (parameter is T specificParameter)
            {
                return specificParameter;
            }

            throw new ArgumentException("Parameter has the wrong type", nameof(parameter));
        }
    }
}
