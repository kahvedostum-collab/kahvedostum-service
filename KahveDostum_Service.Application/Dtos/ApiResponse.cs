namespace KahveDostum_Service.Application.Dtos;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;
    public T? Data { get; set; }
    public int StatusCode { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success", int statusCode = 200)
        => new()
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = statusCode
        };

    public static ApiResponse<T> FailResponse(string message, int statusCode)
        => new()
        {
            Success = false,
            Message = message,
            Data = default,
            StatusCode = statusCode
        };
}