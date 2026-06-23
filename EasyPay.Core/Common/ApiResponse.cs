namespace EasyPay.Core.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new()
        {
            Success = true,
            Message = message,
            Data = data
        };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? new()
        };

    public static ApiResponse<T> Fail(string message, string error) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error }
        };
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ApiResponse Ok(string message = "Success") =>
        new()
        {
            Success = true,
            Message = message
        };

    public static ApiResponse Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };

    public static ApiResponse Fail(string message, string error) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error }
        };

    // ADD THIS OVERLOAD
    public static ApiResponse Fail(string message, List<string> errors) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors
        };
}