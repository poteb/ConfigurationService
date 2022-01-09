using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Services;

public abstract class ApiServiceBase
{
    protected static ApiCallResponse<T> DefaultExceptionResponse<T>(T response, string errorMessage, Exception ex)
    {
        return new ApiCallResponse<T> { Response = response, ErrorMessage = errorMessage, Exception = ex };
    }

    protected static ApiCallResponse<T> DefaultUnsuccessfullResponse<T>(T response, string errorMessage)
    {
        return new ApiCallResponse<T> { Response = response, ErrorMessage = errorMessage };
    }

    protected static ApiCallResponse<T> DefaultUnsuccessfullResponse<T>(T response, int statusCode)
    {
        return new ApiCallResponse<T> { Response = response, ErrorMessage = $"Call was unsuccessfull, error code: {statusCode}" };
    }
}