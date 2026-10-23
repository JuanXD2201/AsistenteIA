namespace AsistenteIA.Core.Models;

public enum AiErrorKind
{
    None,
    EmptyPrompt,
    PromptTooLong,
    MissingKey,
    MissingProvider,
    BadRequest,
    Unauthorized,
    NotFound,
    RateLimited,
    Timeout,
    NoConnection,
    ServerError,
    InvalidResponse,
    Cancelled,
    Unknown
}

public sealed record AiResult<T>(bool Success, T? Value, AiErrorKind Error, string Message, int? HttpStatus = null)
{
    public static AiResult<T> Ok(T value, int? status = 200) => new(true, value, AiErrorKind.None, string.Empty, status);

    public static AiResult<T> Fail(AiErrorKind kind, string message, int? status = null) => new(false, default, kind, message, status);
}
