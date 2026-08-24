namespace LayerCake.Application.Common.Exceptions;

/// <summary>
/// Thrown when publishing a cake whose name is already taken. Mapped to a
/// 409 in the WebApi layer.
/// </summary>
public sealed class DuplicateCakeNameException : Exception
{
    public DuplicateCakeNameException(string name)
        : base($"A cake named \"{name}\" has already been published.")
    {
    }
}
