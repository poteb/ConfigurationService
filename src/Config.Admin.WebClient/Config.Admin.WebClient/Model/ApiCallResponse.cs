namespace pote.Config.Admin.WebClient.Model
{
    public class ApiCallResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Response { get; set; } = default;
        public Exception Exception { get; set; } = null!;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}