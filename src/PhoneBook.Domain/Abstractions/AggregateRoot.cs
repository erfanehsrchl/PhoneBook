namespace PhoneBook.Domain.Abstractions;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull;
