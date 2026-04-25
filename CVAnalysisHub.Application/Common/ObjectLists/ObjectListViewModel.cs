using CVAnalysisHub.Application.Common.ViewModels;

namespace CVAnalysisHub.Application.Common.ObjectLists;

public sealed class ObjectListViewModel<TItem> : ViewModelBase
{
    private readonly IReadOnlyList<ObjectListColumnDefinition<TItem>> definitions;
    private IReadOnlyList<TItem> items = Array.Empty<TItem>();
    private IReadOnlyList<ObjectListColumnViewModel> availableColumns;
    private IReadOnlyList<ObjectListColumnViewModel> visibleColumns = Array.Empty<ObjectListColumnViewModel>();
    private IReadOnlyList<ObjectListRowViewModel<TItem>> rows = Array.Empty<ObjectListRowViewModel<TItem>>();
    private ObjectListRowViewModel<TItem>? selectedRow;

    public ObjectListViewModel(
        IEnumerable<ObjectListColumnDefinition<TItem>> columnDefinitions,
        IEnumerable<string>? defaultVisibleColumnKeys = null)
    {
        definitions = columnDefinitions.ToArray();

        if (definitions.Count == 0)
        {
            throw new InvalidOperationException("At least one column definition is required.");
        }

        var visibleKeys = new HashSet<string>(
            defaultVisibleColumnKeys ?? definitions.Select(definition => definition.Key),
            StringComparer.OrdinalIgnoreCase);

        availableColumns = definitions
            .Select(definition => new ObjectListColumnViewModel(
                definition.Key,
                definition.Header,
                visibleKeys.Contains(definition.Key)))
            .ToArray();

        ApplyVisibleColumns();
    }

    public IReadOnlyList<ObjectListColumnViewModel> AvailableColumns => availableColumns;

    public IReadOnlyList<ObjectListColumnViewModel> VisibleColumns
    {
        get => visibleColumns;
        private set => SetProperty(ref visibleColumns, value);
    }

    public IReadOnlyList<ObjectListRowViewModel<TItem>> Rows
    {
        get => rows;
        private set
        {
            if (!SetProperty(ref rows, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasItems));
        }
    }

    public ObjectListRowViewModel<TItem>? SelectedRow
    {
        get => selectedRow;
        set
        {
            if (!SetProperty(ref selectedRow, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedItem));
            OnPropertyChanged(nameof(HasSelection));
        }
    }

    public TItem? SelectedItem => SelectedRow is null ? default : SelectedRow.Item;

    public bool HasItems => Rows.Count > 0;

    public bool HasSelection => SelectedRow is not null;

    public void SetItems(IEnumerable<TItem> sourceItems)
    {
        items = sourceItems.ToArray();
        RebuildRows();
    }

    public void Clear()
    {
        items = Array.Empty<TItem>();
        Rows = Array.Empty<ObjectListRowViewModel<TItem>>();
        SelectedRow = null;
    }

    public void SelectRow(ObjectListRowViewModel<TItem>? row)
    {
        SelectedRow = row;
    }

    public void SetColumnVisibility(string columnKey, bool isVisible)
    {
        var column = availableColumns.SingleOrDefault(existingColumn =>
            string.Equals(existingColumn.Key, columnKey, StringComparison.OrdinalIgnoreCase));

        if (column is null || column.IsVisible == isVisible)
        {
            return;
        }

        column.IsVisible = isVisible;
        ApplyCurrentColumns();
    }

    public void ApplyCurrentColumns()
    {
        if (!availableColumns.Any(existingColumn => existingColumn.IsVisible))
        {
            availableColumns[0].IsVisible = true;
        }

        ApplyVisibleColumns();
        RebuildRows();
    }

    private void ApplyVisibleColumns()
    {
        VisibleColumns = availableColumns
            .Where(column => column.IsVisible)
            .ToArray();
    }

    private void RebuildRows()
    {
        var selectedItem = SelectedItem;
        Rows = items
            .Select(item => new ObjectListRowViewModel<TItem>(
                item,
                definitions
                    .Where(definition => VisibleColumns.Any(column =>
                        string.Equals(column.Key, definition.Key, StringComparison.OrdinalIgnoreCase)))
                    .Select(definition => new ObjectListCellViewModel(
                        definition.Key,
                        definition.ValueAccessor(item)))
                    .ToArray()))
            .ToArray();

        SelectedRow = selectedItem is null
            ? null
            : Rows.FirstOrDefault(row => EqualityComparer<TItem>.Default.Equals(row.Item, selectedItem));
    }
}
