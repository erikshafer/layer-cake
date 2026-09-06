namespace LayerCake.CleanTemplate.Application.Common.Exceptions;

public class DuplicateCakeNameException : Exception
{
    public DuplicateCakeNameException(string name)
        : base($"A cake named \"{name}\" has already been published.")
    {
    }
}
