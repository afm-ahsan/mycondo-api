namespace MyCondo.Application.Common.Exceptions;

public sealed class NotFoundException : ApplicationException
{
    public string Resource { get; }
    public object Identifier { get; }

    public NotFoundException(string resource, object identifier)
        : base($"{resource} '{identifier}' was not found.")
    {
        Resource = resource;
        Identifier = identifier;
    }
}
