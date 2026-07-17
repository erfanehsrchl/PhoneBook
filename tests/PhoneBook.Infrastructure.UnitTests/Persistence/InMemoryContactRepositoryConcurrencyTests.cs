using FluentAssertions;
using PhoneBook.Application.Common.Exceptions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Infrastructure.Persistence;

namespace PhoneBook.Infrastructure.UnitTests.Persistence;

public class InMemoryContactRepositoryConcurrencyTests
{
    [Fact]
    public async Task Concurrent_add_should_atomically_allow_one_equivalent_phone()
    {
        const int operationCount = 8;
        InMemoryContactRepository repository = new();
        string[] numbers = ["09121234567", "+989121234567", "989121234567", "00989121234567"];
        Contact[] contacts = Enumerable.Range(1, operationCount)
            .Select(index => ContactTestData.CreateContact(
                CreateId(index),
                numbers[(index - 1) % numbers.Length],
                "Concurrent"))
            .ToArray();
        using Barrier barrier = new(operationCount + 1);

        Task<Exception?>[] tasks = contacts.Select(contact => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            try
            {
                await repository.AddAsync(contact, CancellationToken.None);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        })).ToArray();

        barrier.SignalAndWait();
        Exception?[] outcomes = await Task.WhenAll(tasks);

        outcomes.Count(exception => exception is null).Should().Be(1);
        outcomes.Count(exception => exception is ConflictException).Should().Be(operationCount - 1);
    }

    [Fact]
    public async Task Concurrent_updates_should_atomically_assign_target_phone_to_one_contact()
    {
        const int operationCount = 4;
        const string targetPhone = "09359999999";
        InMemoryContactRepository repository = new();
        Contact[] originals = Enumerable.Range(1, operationCount)
            .Select(index => ContactTestData.CreateContact(
                CreateId(index),
                $"0912100000{index}",
                "Concurrent"))
            .ToArray();
        foreach (Contact original in originals)
        {
            await repository.AddAsync(original, CancellationToken.None);
        }

        Contact[] updates = await Task.WhenAll(originals.Select(async original =>
        {
            Contact contact = (await repository.GetByIdAsync(original.Id, CancellationToken.None))!;
            contact.Update(
                contact.FirstName.Value,
                contact.LastName.Value,
                targetPhone,
                contact.Tag.Value,
                ContactTestData.UpdatedAtUtc);
            return contact;
        }));
        using Barrier barrier = new(operationCount + 1);
        Task<Exception?>[] tasks = updates.Select(contact => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            try
            {
                await repository.UpdateAsync(contact, CancellationToken.None);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        })).ToArray();

        barrier.SignalAndWait();
        Exception?[] outcomes = await Task.WhenAll(tasks);

        outcomes.Count(exception => exception is null).Should().Be(1);
        outcomes.Count(exception => exception is ConflictException).Should().Be(operationCount - 1);
        Contact?[] stored = await Task.WhenAll(originals.Select(original =>
            repository.GetByIdAsync(original.Id, CancellationToken.None)));
        stored.Count(contact => contact!.PhoneNumber.Value == "+989359999999").Should().Be(1);
    }

    private static string CreateId(int value)
    {
        return $"00000000-0000-0000-0000-{value:D12}";
    }
}
