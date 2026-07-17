namespace PhoneBook.Application.Common.Exceptions;

public sealed class NotFoundException : ApplicationExceptionBase
{
    public NotFoundException(string code, string message)
        : base(code, message)
    {
    }
}
