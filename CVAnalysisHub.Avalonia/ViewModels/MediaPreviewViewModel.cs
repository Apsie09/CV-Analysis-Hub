using Avalonia.Media.Imaging;
using CVAnalysisHub.Application.Common.ViewModels;

namespace CVAnalysisHub.Avalonia.ViewModels;

public sealed class MediaPreviewViewModel : ViewModelBase, IDisposable
{
    private Bitmap? bitmap;

    public Bitmap? Bitmap
    {
        get => bitmap;
        private set
        {
            if (ReferenceEquals(bitmap, value))
            {
                return;
            }

            var previous = bitmap;
            bitmap = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasBitmap));
            OnPropertyChanged(nameof(MissingBitmap));
            previous?.Dispose();
        }
    }

    public bool HasBitmap => Bitmap is not null;

    public bool MissingBitmap => !HasBitmap;

    public void SetBitmap(Bitmap? value)
    {
        Bitmap = value;
    }

    public void Clear()
    {
        Bitmap = null;
    }

    public void Dispose()
    {
        Clear();
    }
}
