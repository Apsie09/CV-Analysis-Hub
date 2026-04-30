namespace CVAnalysisHub.Application.Common.ViewModels;

public sealed class AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
{
    public bool CanExecute()
    {
        return canExecute?.Invoke() ?? true;
    }

    public Task ExecuteAsync()
    {
        return CanExecute()
            ? executeAsync()
            : Task.CompletedTask;
    }
}
