namespace TrangSach.BUS.Results;

public enum OperationStatus
{
    Success,
    Created,
    NotFound,
    Invalid,
    Conflict,
    Forbidden,
    Unauthorized
}

public sealed record OperationResult<T>(OperationStatus Status, T? Value = default, string? Message = null)
{
    public bool Succeeded => Status is OperationStatus.Success or OperationStatus.Created;

    public static OperationResult<T> Success(T? value = default) => new(OperationStatus.Success, value);
    public static OperationResult<T> Created(T value) => new(OperationStatus.Created, value);
    public static OperationResult<T> NotFound(string? message = null) => new(OperationStatus.NotFound, default, message);
    public static OperationResult<T> Invalid(string message) => new(OperationStatus.Invalid, default, message);
    public static OperationResult<T> Conflict(string message) => new(OperationStatus.Conflict, default, message);
    public static OperationResult<T> Forbidden(string? message = null) => new(OperationStatus.Forbidden, default, message);
    public static OperationResult<T> Unauthorized(string? message = null) => new(OperationStatus.Unauthorized, default, message);
}

public readonly record struct Unit;
