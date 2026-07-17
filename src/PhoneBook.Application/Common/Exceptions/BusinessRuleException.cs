namespace PhoneBook.Application.Common.Exceptions;

public sealed class BusinessRuleException : ApplicationExceptionBase
{
    public BusinessRuleException(string code, string message)
        : base(code, message)
    {
    }
}
