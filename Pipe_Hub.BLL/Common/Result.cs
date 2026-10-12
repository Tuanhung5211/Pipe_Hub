namespace Pipe_Hub.BLL.Common;

/// <summary>
/// Kết quả trả về của service — không throw exception cho lỗi nghiệp vụ nhẹ.
/// Dùng cho các trường hợp: validate thất bại, business rule không thỏa.
/// Với lỗi nghiêm trọng hơn → dùng AppException.
/// </summary>
public class Result
{
    public bool IsSuccess { get; protected set; }
    public string? ErrorMessage { get; protected set; }

    protected Result(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string errorMessage) => new(false, errorMessage);
}

public class Result<T> : Result
{
    public T? Data { get; private set; }

    private Result(bool isSuccess, T? data, string? errorMessage)
        : base(isSuccess, errorMessage)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public static new Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
