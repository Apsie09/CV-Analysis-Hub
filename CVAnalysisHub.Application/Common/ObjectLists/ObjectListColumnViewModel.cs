using CVAnalysisHub.Application.Common.ViewModels;

namespace CVAnalysisHub.Application.Common.ObjectLists;

public sealed class ObjectListColumnViewModel(string key, string header, bool isVisible) : ViewModelBase
{
    private bool isVisible = isVisible;

    public string Key { get; } = key;

    public string Header { get; } = header;

    public bool IsVisible
    {
        get => isVisible;
        set => SetProperty(ref isVisible, value);
    }
}
