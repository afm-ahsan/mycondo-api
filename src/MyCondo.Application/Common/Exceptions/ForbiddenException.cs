namespace MyCondo.Application.Common.Exceptions;

public sealed class ForbiddenException : ApplicationException
{
    public ForbiddenException(string message = "Forbidden") : base(message) { }
}
