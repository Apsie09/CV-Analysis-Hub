using System.Windows.Input;

namespace CVAnalysisHub.Avalonia.ViewModels;

public sealed class AsyncDelegateCommand(
    Func<Task> executeAsync,
    Func<bool>? canExecute = null,
    bool allowConcurrentExecutions = false) : ICommand
{
    private bool isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return (allowConcurrentExecutions || !isExecuting) &&
            (canExecute?.Invoke() ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await executeAsync();
        }
        finally
        {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
