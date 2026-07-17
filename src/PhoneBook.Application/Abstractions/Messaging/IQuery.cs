using MediatR;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
