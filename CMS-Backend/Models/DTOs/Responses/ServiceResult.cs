namespace CMS_Backend.Models.DTOs.Responses
{
    public enum ResultStatus
    {
        Success,
        NotFound,
        Conflict,
        Unauthorized,
        Failed
    }

    public class ServiceResult<T>
    {
        public ResultStatus Status { get; init; }
        public T? Data { get; init; }
        public List<string> Errors { get; init; } = new();

        public bool Succeeded => Status == ResultStatus.Success;

        public static ServiceResult<T> Ok(T data) =>
            new() { Status = ResultStatus.Success, Data = data };

        public static ServiceResult<T> NotFound(string error) =>
            new() { Status = ResultStatus.NotFound, Errors = new() { error } };

        public static ServiceResult<T> Conflict(string error) =>
            new() { Status = ResultStatus.Conflict, Errors = new() { error } };

        public static ServiceResult<T> Unauthorized(string error) =>
            new() { Status = ResultStatus.Unauthorized, Errors = new() { error } };

        public static ServiceResult<T> Failed(string error) =>
            new() { Status = ResultStatus.Failed, Errors = new() { error } };

        public static ServiceResult<T> Failed(IEnumerable<string> errors) =>
            new() { Status = ResultStatus.Failed, Errors = errors.ToList() };
    }
}
