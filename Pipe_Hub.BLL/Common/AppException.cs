namespace Pipe_Hub.BLL.Common;

/// <summary>
/// Exception nghiệp vụ — ném khi business rule bị vi phạm.
/// UI sẽ bắt exception này qua RunSafeAsync → hiển thị MessageBox thân thiện.
/// KHÔNG dùng cho lỗi hệ thống (NullReference, IO...) — những lỗi đó để global handler.
/// </summary>
public class AppException : Exception
{
    public AppException(string message) : base(message) { }
    public AppException(string message, Exception innerException)
        : base(message, innerException) { }
}
