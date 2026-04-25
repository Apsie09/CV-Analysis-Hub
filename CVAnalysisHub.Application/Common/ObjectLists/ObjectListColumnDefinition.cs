namespace CVAnalysisHub.Application.Common.ObjectLists;

public sealed record ObjectListColumnDefinition<TItem>(
    string Key,
    string Header,
    Func<TItem, string> ValueAccessor);
