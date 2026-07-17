using FluentAssertions;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;
using PhoneBook.Infrastructure.Persistence;

namespace PhoneBook.Infrastructure.UnitTests.Persistence;

public class InMemoryContactRepositoryConcurrencyTests
{
    [Fact]
    public async Task Concurrent_AddAsync_should_atomically_enforce_phone_uniqueness()
    {
        const int operationCount = 8;
        InMemoryContactRepository repository = new();
        string[] equivalentNumbers =
        [
            "09121234567",
            "+989121234567",
            "989121234567",
            "00989121234567"
        ];
        Contact[] contacts = Enumerable.Range(1, operationCount)
            .Select(index => ContactTestData.CreateContact(
                CreateId(index),
                equivalentNumbers[(index - 1) % equivalentNumbers.Length],
                "ConcurrentAdd"))
            .ToArray();

        using Barrier barrier = new(operationCount + 1);
        Task<Result>[] tasks = contacts
            .Select(contact => Task.Run(() =>
            {
                barrier.SignalAndWait();
                return repository.AddAsync(contact, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }))
            .ToArray();

        barrier.SignalAndWait();
        Result[] results = await Task.WhenAll(tasks);

        results.Count(result => result.IsSuccess).Should().Be(1);
        results.Where(result => result.IsFailure)
            .Should()
            .OnlyContain(result => result.Error.Code == "Contact.PhoneNumber.AlreadyExists");

        IReadOnlyCollection<Contact> stored = await repository.GetByTagAsync(
            Tag.Create("ConcurrentAdd").Value,
            CancellationToken.None);
        stored.Should().ContainSingle();
        Contact winner = stored.Single();
        Contact? winnerById = await repository.GetByIdAsync(
            winner.Id,
            CancellationToken.None);
        (winnerById is not null).Should().BeTrue();

        Result usableResult = await repository.AddAsync(
            ContactTestData.CreateContact(CreateId(100), "09357654321", "Usable"),
            CancellationToken.None);
        usableResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Concurrent_UpdateAsync_should_atomically_assign_target_phone_to_one_contact()
    {
        const int operationCount = 4;
        const string targetPhone = "09359999999";
        InMemoryContactRepository repository = new();
        Contact[] originals = Enumerable.Range(1, operationCount)
            .Select(index => ContactTestData.CreateContact(
                CreateId(index),
                $"0912100000{index}",
                "ConcurrentUpdate"))
            .ToArray();

        foreach (Contact original in originals)
        {
            await repository.AddAsync(original, CancellationToken.None);
        }

        Contact[] candidates = new Contact[operationCount];

        for (int index = 0; index < operationCount; index++)
        {
            candidates[index] = (await repository.GetByIdAsync(
                originals[index].Id,
                CancellationToken.None))!;
            candidates[index].Update(
                $"Candidate{index + 1}",
                "Updated",
                targetPhone,
                "Target",
                ContactTestData.UpdatedAtUtc.AddMinutes(index));
        }

        using Barrier barrier = new(operationCount + 1);
        Task<Result>[] tasks = candidates
            .Select(candidate => Task.Run(() =>
            {
                barrier.SignalAndWait();
                return repository.UpdateAsync(candidate, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }))
            .ToArray();

        barrier.SignalAndWait();
        Result[] results = await Task.WhenAll(tasks);

        results.Count(result => result.IsSuccess).Should().Be(1);
        results.Where(result => result.IsFailure)
            .Should()
            .OnlyContain(result => result.Error.Code == "Contact.PhoneNumber.AlreadyExists");

        Contact[] persisted = new Contact[operationCount];

        for (int index = 0; index < operationCount; index++)
        {
            persisted[index] = (await repository.GetByIdAsync(
                originals[index].Id,
                CancellationToken.None))!;
        }

        persisted.Count(contact => contact.PhoneNumber.Value == "+989359999999")
            .Should()
            .Be(1);

        int winnerIndex = Array.FindIndex(results, result => result.IsSuccess);

        for (int index = 0; index < operationCount; index++)
        {
            if (index == winnerIndex)
            {
                persisted[index].UpdatedAtUtc.Should().Be(
                    ContactTestData.UpdatedAtUtc.AddMinutes(index));
                continue;
            }

            persisted[index].PhoneNumber.Equals(originals[index].PhoneNumber)
                .Should()
                .BeTrue();
            persisted[index].UpdatedAtUtc.Should().BeNull();

            Result retainedIndexResult = await repository.AddAsync(
                ContactTestData.CreateContact(
                    CreateId(20 + index),
                    originals[index].PhoneNumber.Value,
                    "IndexCheck"),
                CancellationToken.None);
            retainedIndexResult.IsFailure.Should().BeTrue();
        }

        Result releasedWinnerNumber = await repository.AddAsync(
            ContactTestData.CreateContact(
                CreateId(50),
                originals[winnerIndex].PhoneNumber.Value,
                "Released"),
            CancellationToken.None);
        Result targetStillOwned = await repository.AddAsync(
            ContactTestData.CreateContact(CreateId(51), targetPhone, "TargetCheck"),
            CancellationToken.None);

        releasedWinnerNumber.IsSuccess.Should().BeTrue();
        targetStillOwned.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Concurrent_reads_and_updates_should_return_only_complete_independent_snapshots()
    {
        const int writerCount = 4;
        const int readerCount = 12;
        InMemoryContactRepository repository = new();
        Contact original = ContactTestData.CreateContact(
            CreateId(1),
            "09121234567",
            "Initial");
        await repository.AddAsync(original, CancellationToken.None);

        Contact[] candidates = new Contact[writerCount];
        Dictionary<string, ExpectedState> expectedStates = new()
        {
            ["Erfan"] = new(
                "+989121234567",
                "Initial",
                null)
        };

        for (int index = 0; index < writerCount; index++)
        {
            int number = index + 1;
            string firstName = $"Writer{number}";
            string phoneNumber = $"0935000000{number}";
            string canonicalPhone = $"+98935000000{number}";
            string tag = $"State{number}";
            DateTime updatedAtUtc = ContactTestData.UpdatedAtUtc.AddMinutes(index);
            Contact candidate = (await repository.GetByIdAsync(
                original.Id,
                CancellationToken.None))!;
            candidate.Update(
                firstName,
                "Updated",
                phoneNumber,
                tag,
                updatedAtUtc);
            candidates[index] = candidate;
            expectedStates[firstName] = new(canonicalPhone, tag, updatedAtUtc);
        }

        using Barrier barrier = new(writerCount + readerCount + 1);
        Task<Result>[] writerTasks = candidates
            .Select(candidate => Task.Run(() =>
            {
                barrier.SignalAndWait();
                return repository.UpdateAsync(candidate, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }))
            .ToArray();
        Task<Contact?>[] readerTasks = Enumerable.Range(0, readerCount)
            .Select(_ => Task.Run(() =>
            {
                barrier.SignalAndWait();
                return repository.GetByIdAsync(original.Id, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }))
            .ToArray();

        barrier.SignalAndWait();
        Result[] writeResults = await Task.WhenAll(writerTasks);
        Contact?[] readResults = await Task.WhenAll(readerTasks);

        writeResults.Should().OnlyContain(result => result.IsSuccess);
        readResults.Should().OnlyContain(contact => contact != null);

        foreach (Contact contact in readResults.Cast<Contact>())
        {
            AssertCompleteState(contact, expectedStates);
        }

        for (int first = 0; first < readResults.Length; first++)
        {
            for (int second = first + 1; second < readResults.Length; second++)
            {
                ReferenceEquals(readResults[first], readResults[second]).Should().BeFalse();
            }
        }

        Contact final = (await repository.GetByIdAsync(
            original.Id,
            CancellationToken.None))!;
        AssertCompleteState(final, expectedStates);
        final.CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
        final.UpdatedAtUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
        (final.UpdatedAtUtc >= final.CreatedAtUtc).Should().BeTrue();
    }

    private static void AssertCompleteState(
        Contact contact,
        IReadOnlyDictionary<string, ExpectedState> expectedStates)
    {
        expectedStates.TryGetValue(contact.FirstName.Value, out ExpectedState? expected)
            .Should()
            .BeTrue();
        (expected is not null).Should().BeTrue();
        contact.PhoneNumber.Value.Should().Be(expected!.PhoneNumber);
        contact.Tag.Value.Should().Be(expected.Tag);
        contact.UpdatedAtUtc.Should().Be(expected.UpdatedAtUtc);
        contact.CreatedAtUtc.Should().Be(ContactTestData.CreatedAtUtc);
    }

    private static string CreateId(int value)
    {
        return $"00000000-0000-0000-0000-{value:D12}";
    }

    private record ExpectedState(
        string PhoneNumber,
        string Tag,
        DateTime? UpdatedAtUtc);
}
