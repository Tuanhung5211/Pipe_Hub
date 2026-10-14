namespace Pipe_Hub.UI.Helpers;

/// <summary>
/// Debounce timer — chờ N ms sau lần gọi cuối mới thực thi action.
/// Dùng cho tìm kiếm realtime: gõ phím liên tục chỉ trigger 1 lần query.
/// </summary>
public class DebounceTimer : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private Action? _pendingAction;

    public DebounceTimer(int intervalMs = 300)
    {
        _timer = new System.Windows.Forms.Timer { Interval = intervalMs };
        _timer.Tick += OnTick;
    }

    public void Debounce(Action action)
    {
        _pendingAction = action;
        _timer.Stop();
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        var action = _pendingAction;
        _pendingAction = null;
        action?.Invoke();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer.Dispose();
    }
}
