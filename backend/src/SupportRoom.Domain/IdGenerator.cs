namespace SupportRoom.Domain;

/// <summary>Mirrors src/utils/id.ts.</summary>
public static class IdGenerator
{
    public static string GenerateId(string prefix) => $"{prefix}-{Guid.NewGuid()}";

    public static string GeneratePublicToken() => Guid.NewGuid().ToString();
}
