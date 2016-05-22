using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RoslynPad.Utilities
{
    internal sealed class DelegateCommand : ICommand
    {
        private readonly Action _action;
        private readonly Func<bool> _canExecute;
        private readonly Func<Task> _asyncAction;

        public DelegateCommand(Action action, Func<bool> canExecute = null)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public DelegateCommand(Func<Task> asyncAction, Func<bool> canExecute = null)
        {
            _asyncAction = asyncAction;
            _canExecute = canExecute;
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        public bool CanExecute()
        {
            return _canExecute == null || _canExecute();
        }

        void ICommand.Execute(object parameter)
        {
            Execute();
        }

        public void Execute()
        {
            if (_asyncAction != null)
            {
                ExecuteAsync();
            }
            else
            {
                _action();
            }
        }

        private async void ExecuteAsync()
        {
            await _asyncAction().ConfigureAwait(true);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal sealed class DelegateCommand<T> : ICommand
    {
        private readonly Action<T> _action;
        private readonly Func<T, bool> _canExecute;
        private readonly Func<T, Task> _asyncAction;

        public DelegateCommand(Action<T> action, Func<T, bool> canExecute = null)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public DelegateCommand(Func<T, Task> asyncAction, Func<T, bool> canExecute = null)
        {
            _asyncAction = asyncAction;
            _canExecute = canExecute;
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute((T)parameter);
        }

        public bool CanExecute(T parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            Execute((T)parameter);
        }

        public void Execute(T parameter)
        {
            if (_asyncAction != null)
            {
                ExecuteAsync(parameter);
            }
            else
            {
                _action(parameter);
            }
        }

        private async void ExecuteAsync(T parameter)
        {
            await _asyncAction(parameter).ConfigureAwait(true);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}