using System.ComponentModel;

namespace Pipe_Hub.UI.Helpers;

/// <summary>
/// BindingList<T> hỗ trợ sort tự động khi user click header DataGridView.
/// </summary>
public class SortableBindingList<T> : BindingList<T>
{
    private bool _isSorted;
    private ListSortDirection _sortDirection;
    private PropertyDescriptor? _sortProperty;

    public SortableBindingList() : base() { }
    public SortableBindingList(IList<T> list) : base(list) { }

    protected override bool SupportsSortingCore => true;
    protected override bool IsSortedCore => _isSorted;
    protected override ListSortDirection SortDirectionCore => _sortDirection;
    protected override PropertyDescriptor? SortPropertyCore => _sortProperty;

    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
    {
        _sortProperty = prop;
        _sortDirection = direction;

        if (Items is not List<T> list)
        {
            return;
        }

        var comparer = new PropertyComparer(prop, direction);
        list.Sort(comparer);

        _isSorted = true;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    protected override void RemoveSortCore()
    {
        _isSorted = false;
        _sortProperty = null;
    }

    private class PropertyComparer : System.Collections.Generic.IComparer<T>
    {
        private readonly PropertyDescriptor _prop;
        private readonly ListSortDirection _direction;

        public PropertyComparer(PropertyDescriptor prop, ListSortDirection direction)
        {
            _prop = prop;
            _direction = direction;
        }

        public int Compare(T? x, T? y)
        {
            var valueX = _prop.GetValue(x);
            var valueY = _prop.GetValue(y);

            var result = System.Collections.Comparer.Default.Compare(valueX, valueY);

            return _direction == ListSortDirection.Ascending ? result : -result;
        }
    }
}
