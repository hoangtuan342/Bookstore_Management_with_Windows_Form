namespace TrangSach.BUS
{
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

    public class OperationResult<T>
    {
        public OperationStatus Status { get; set; }
        public T? Value { get; set; }
        public string? Message { get; set; }

        public bool Succeeded
        {
            get { return Status == OperationStatus.Success || Status == OperationStatus.Created; }
        }

        public OperationResult()
        {
        }

        public OperationResult(OperationStatus status, T? value = default, string? message = null)
        {
            this.Status = status;
            this.Value = value;
            this.Message = message;
        }

        public static OperationResult<T> Success(T? value = default)
        {
            return new OperationResult<T>(OperationStatus.Success, value);
        }

        public static OperationResult<T> Created(T value)
        {
            return new OperationResult<T>(OperationStatus.Created, value);
        }

        public static OperationResult<T> NotFound(string? message = null)
        {
            return new OperationResult<T>(OperationStatus.NotFound, default, message);
        }

        public static OperationResult<T> Invalid(string message)
        {
            return new OperationResult<T>(OperationStatus.Invalid, default, message);
        }

        public static OperationResult<T> Conflict(string message)
        {
            return new OperationResult<T>(OperationStatus.Conflict, default, message);
        }

        public static OperationResult<T> Forbidden(string? message = null)
        {
            return new OperationResult<T>(OperationStatus.Forbidden, default, message);
        }

        public static OperationResult<T> Unauthorized(string? message = null)
        {
            return new OperationResult<T>(OperationStatus.Unauthorized, default, message);
        }
    }

    public struct Unit
    {
    }
}
