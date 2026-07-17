using PhoneBook.Application.Abstractions.Time;

namespace PhoneBook.Infrastructure.Time;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
