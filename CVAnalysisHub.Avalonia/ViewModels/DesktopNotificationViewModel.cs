using CVAnalysisHub.Application.Common.ViewModels;

namespace CVAnalysisHub.Avalonia.ViewModels;

public sealed class DesktopNotificationViewModel : ViewModelBase
{
    private string statusMessage = "Desktop client ready to browse videos and queue analyses.";
    private string? errorMessage;

    public string StatusMessage
    {
        get => statusMessage;
        private set
        {
            if (!SetProperty(ref statusMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (!SetProperty(ref errorMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasErrorMessage));
        }
    }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public void ShowStatus(string message)
    {
        ErrorMessage = null;
        StatusMessage = message;
    }

    public void ShowError(string message)
    {
        ErrorMessage = message;
    }

    public void ClearError()
    {
        ErrorMessage = null;
    }
}
