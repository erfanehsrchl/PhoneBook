using PhoneBook.Application.Abstractions.Time;

namespace PhoneBook.Application.UnitTests.TestDoubles;

internal class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The fake clock requires a UTC value.", nameof(utcNow));
        }

        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }
}
