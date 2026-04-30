using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using CVAnalysisHub.Application.Common.Filters;

namespace CVAnalysisHub.Avalonia.Views;

public partial class FilterPanelView : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<FilterFieldViewModel>?> FieldsProperty =
        AvaloniaProperty.Register<FilterPanelView, IReadOnlyList<FilterFieldViewModel>?>(nameof(Fields));

    public static readonly StyledProperty<ICommand?> ApplyCommandProperty =
        AvaloniaProperty.Register<FilterPanelView, ICommand?>(nameof(ApplyCommand));

    public static readonly StyledProperty<ICommand?> ClearCommandProperty =
        AvaloniaProperty.Register<FilterPanelView, ICommand?>(nameof(ClearCommand));

    public FilterPanelView()
    {
        InitializeComponent();
    }

    public IReadOnlyList<FilterFieldViewModel>? Fields
    {
        get => GetValue(FieldsProperty);
        set => SetValue(FieldsProperty, value);
    }

    public ICommand? ApplyCommand
    {
        get => GetValue(ApplyCommandProperty);
        set => SetValue(ApplyCommandProperty, value);
    }

    public ICommand? ClearCommand
    {
        get => GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }
}
