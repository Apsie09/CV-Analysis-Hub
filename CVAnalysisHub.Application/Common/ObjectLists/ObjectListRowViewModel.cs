namespace CVAnalysisHub.Application.Common.ObjectLists;

public sealed record ObjectListRowViewModel<TItem>(
    TItem Item,
    IReadOnlyList<ObjectListCellViewModel> Cells);
