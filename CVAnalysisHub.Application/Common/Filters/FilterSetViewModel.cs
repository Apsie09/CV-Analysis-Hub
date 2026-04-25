using CVAnalysisHub.Application.Common.ViewModels;

namespace CVAnalysisHub.Application.Common.Filters;

public abstract class FilterSetViewModel<TRequest> : ViewModelBase
{
    private readonly IReadOnlyDictionary<string, FilterFieldViewModel> fieldsByKey;

    protected FilterSetViewModel(IEnumerable<FilterFieldViewModel> fields)
    {
        var materializedFields = fields.ToArray();

        if (materializedFields.Length == 0)
        {
            throw new InvalidOperationException("At least one filter field is required.");
        }

        Fields = materializedFields;
        fieldsByKey = materializedFields.ToDictionary(field => field.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<FilterFieldViewModel> Fields { get; }

    public bool HasActiveValues => Fields.Any(filterField =>
        !string.IsNullOrWhiteSpace(filterField.PrimaryValue) ||
        !string.IsNullOrWhiteSpace(filterField.SecondaryValue));

    public void Clear()
    {
        foreach (var field in Fields)
        {
            field.Clear();
        }
    }

    public abstract TRequest BuildRequest();

    protected string? GetPrimaryText(string key)
    {
        return NormalizeText(GetField(key).PrimaryValue);
    }

    protected string? GetSecondaryText(string key)
    {
        return NormalizeText(GetField(key).SecondaryValue);
    }

    protected int? GetPrimaryInt(string key)
    {
        return ParseNullableInt(GetField(key).PrimaryValue);
    }

    protected int? GetSecondaryInt(string key)
    {
        return ParseNullableInt(GetField(key).SecondaryValue);
    }

    protected DateTime? GetPrimaryDateStartUtc(string key)
    {
        return ParseNullableDate(GetField(key).PrimaryValue);
    }

    protected DateTime? GetSecondaryDateExclusiveUtc(string key)
    {
        var date = ParseNullableDate(GetField(key).SecondaryValue);
        return date?.AddDays(1);
    }

    private FilterFieldViewModel GetField(string key)
    {
        if (!fieldsByKey.TryGetValue(key, out var field))
        {
            throw new InvalidOperationException($"Filter field '{key}' is not registered.");
        }

        return field;
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static int? ParseNullableInt(string? value)
    {
        var normalized = NormalizeText(value);
        return normalized is null
            ? null
            : int.Parse(normalized);
    }

    private static DateTime? ParseNullableDate(string? value)
    {
        var normalized = NormalizeText(value);
        return normalized is null
            ? null
            : DateTime.SpecifyKind(DateTime.Parse(normalized), DateTimeKind.Utc);
    }
}
