using MediatR;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
