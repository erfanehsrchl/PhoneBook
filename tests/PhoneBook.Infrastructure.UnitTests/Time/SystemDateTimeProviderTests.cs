using FluentAssertions;
using PhoneBook.Infrastructure.Time;

namespace PhoneBook.Infrastructure.UnitTests.Time;

public class SystemDateTimeProviderTests
{
    [Fact]
    public void UtcNow_should_return_utc_timestamp()
    {
        SystemDateTimeProvider provider = new();

        DateTime result = provider.UtcNow;

        result.Kind.Should().Be(DateTimeKind.Utc);
    }
}
