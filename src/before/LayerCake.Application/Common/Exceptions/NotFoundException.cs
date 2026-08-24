namespace LayerCake.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist. Mapped to a 404 in the
/// WebApi layer.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}
