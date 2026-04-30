using CVAnalysisHub.Application.Common.ViewModels;

namespace CVAnalysisHub.Application.Common.Filters;

public sealed class FilterFieldViewModel(
    string key,
    string label,
    string description,
    FilterFieldType type,
    string? primaryPlaceholder = null,
    string? secondaryPlaceholder = null,
    IEnumerable<FilterOption>? options = null) : ViewModelBase
{
    private string? primaryValue;
    private string? secondaryValue;

    public string Key { get; } = key;

    public string Label { get; } = label;

    public string Description { get; } = description;

    public FilterFieldType Type { get; } = type;

    public string? PrimaryPlaceholder { get; } = primaryPlaceholder;

    public string? SecondaryPlaceholder { get; } = secondaryPlaceholder;

    public IReadOnlyList<FilterOption> Options { get; } = options?.ToArray() ?? [];

    public bool IsText => Type == FilterFieldType.Text;

    public bool IsSelect => Type == FilterFieldType.Select;

    public bool IsNumberRange => Type == FilterFieldType.NumberRange;

    public bool IsDateRange => Type == FilterFieldType.DateRange;

    public bool IsRange => IsNumberRange || IsDateRange;

    public FilterOption? SelectedOption
    {
        get => Options.FirstOrDefault(option =>
            string.Equals(option.Value, PrimaryValue, StringComparison.Ordinal));
        set => PrimaryValue = value?.Value;
    }

    public string? PrimaryValue
    {
        get => primaryValue;
        set
        {
            if (!SetProperty(ref primaryValue, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedOption));
        }
    }

    public string? SecondaryValue
    {
        get => secondaryValue;
        set => SetProperty(ref secondaryValue, value);
    }

    public void Clear()
    {
        PrimaryValue = null;
        SecondaryValue = null;
    }
}
