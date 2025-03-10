namespace RapidPay.Shared.Contracts.Messaging;

public record Result<T>(T? Data, string Error = "")
{
    public bool IsSuccess => Data is not null;
    public static Result<T> Success(T data) => new(data);
    public static Result<T> Failure(string error) => new(default, error);
}