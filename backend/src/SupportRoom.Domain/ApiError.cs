namespace SupportRoom.Domain;

public sealed class ApiErrorBody
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public object? Details { get; init; }
    public string? RequestId { get; init; }
}

public sealed class ApiErrorResponse
{
    public required ApiErrorBody Error { get; init; }
}
