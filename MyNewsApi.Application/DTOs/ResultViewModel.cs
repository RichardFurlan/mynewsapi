namespace MyNewsApi.Application.DTOs;

public record ResultViewModel(bool IsSuccess = true, string Message = "")
{
    public static ResultViewModel Success()
        => new ();

    public static ResultViewModel Error(string message)
        => new(false, message);
};

public record ResultViewModel<T> : ResultViewModel
{
    public ResultViewModel(T? data, bool isSuccess = true, string message = "") : base(isSuccess, message)
    {
        Data = data;
    }
    public T? Data { get; init; }

    public static ResultViewModel<T> Success(T data)
        => new (data);

    public static ResultViewModel<T> Error(string message)
        => new (default, false, message);
};