using CVAnalysisHub.Application.Common.ViewModels;

namespace CVAnalysisHub.Avalonia.ViewModels;

public sealed class DesktopUploadViewModel : ViewModelBase
{
    private string fileName = "No video selected";
    private string fileSize = string.Empty;
    private string? filePath;

    public string FileName
    {
        get => fileName;
        private set => SetProperty(ref fileName, value);
    }

    public string FileSize
    {
        get => fileSize;
        private set => SetProperty(ref fileSize, value);
    }

    public string? FilePath
    {
        get => filePath;
        private set
        {
            if (!SetProperty(ref filePath, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasFile));
        }
    }

    public bool HasFile => !string.IsNullOrWhiteSpace(FilePath);

    public void SelectFile(string path, string name, string size)
    {
        FilePath = path;
        FileName = name;
        FileSize = size;
    }

    public void Clear()
    {
        FilePath = null;
        FileName = "No video selected";
        FileSize = string.Empty;
    }
}
