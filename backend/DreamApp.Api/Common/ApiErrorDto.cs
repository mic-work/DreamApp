namespace DreamApp.Api.Common;

/// <summary>Consistent JSON shape for error responses (400/401/403/404/409/500).</summary>
public class ApiErrorDto
{
    public string Message { get; set; } = string.Empty;

    public ApiErrorDto() { }

    public ApiErrorDto(string message) => Message = message;
}
