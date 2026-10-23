using System.Net.Sockets;
using AsistenteIA.Core.Models;

namespace AsistenteIA.Core.Services;

public static class AiErrorMapper
{
    public static AiResult<T> FromStatus<T>(int status) => status switch
    {
        400 => AiResult<T>.Fail(AiErrorKind.BadRequest, "Solicitud rechazada (400): la clave, el modelo o el contenido no son válidos para este proveedor.", status),
        401 or 403 => AiResult<T>.Fail(AiErrorKind.Unauthorized, "Clave inválida o sin permisos para este proveedor (" + status + ").", status),
        404 => AiResult<T>.Fail(AiErrorKind.NotFound, "Modelo o endpoint no encontrado (404).", status),
        408 => AiResult<T>.Fail(AiErrorKind.Timeout, "El proveedor tardó demasiado en responder (408).", status),
        429 => AiResult<T>.Fail(AiErrorKind.RateLimited, "Límite de uso o créditos agotados (429). Espera e inténtalo de nuevo.", status),
        >= 500 => AiResult<T>.Fail(AiErrorKind.ServerError, "El proveedor tiene un error temporal (" + status + ").", status),
        _ => AiResult<T>.Fail(AiErrorKind.Unknown, "Respuesta inesperada del proveedor (" + status + ").", status)
    };

    public static AiResult<T> FromException<T>(Exception ex, bool callerCancelled)
    {
        if (callerCancelled) return AiResult<T>.Fail(AiErrorKind.Cancelled, "Operación cancelada.");
        return ex switch
        {
            TaskCanceledException or TimeoutException or OperationCanceledException =>
                AiResult<T>.Fail(AiErrorKind.Timeout, "Se agotó el tiempo de espera."),
            HttpRequestException or SocketException =>
                AiResult<T>.Fail(AiErrorKind.NoConnection, "Sin conexión con el proveedor. Revisa tu red."),
            _ => AiResult<T>.Fail(AiErrorKind.Unknown, "Error inesperado.")
        };
    }
}
