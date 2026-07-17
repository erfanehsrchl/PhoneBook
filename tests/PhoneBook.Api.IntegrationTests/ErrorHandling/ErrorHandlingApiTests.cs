using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PhoneBook.Api.Contracts;
using PhoneBook.Api.Contracts.Contacts;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Common.Exceptions;
using PhoneBook.Domain.Contacts;

namespace PhoneBook.Api.IntegrationTests.ErrorHandling;

public class ErrorHandlingApiTests
{
    [Fact]
    public async Task Unexpected_exception_should_return_safe_500_api_response()
    {
        using WebApplicationFactory<Program> factory = CreateFactory<ThrowingContactRepository>();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            ValidRequest(),
            CancellationToken.None);
        string body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        ApiResponse error = (await response.Content.ReadFromJsonAsync<ApiResponse>())!;

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        error.StatusCode.Should().Be((int)response.StatusCode);
        error.Message.Should().Be("An unexpected error occurred.");
        error.ErrorCode.Should().Be("Server.UnexpectedError");
        body.Should().NotContain("sensitive repository failure");
    }

    [Fact]
    public async Task Business_rule_exception_should_return_422_api_response()
    {
        using WebApplicationFactory<Program> factory = CreateFactory<FailingContactRepository>();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            ValidRequest(),
            CancellationToken.None);
        ApiResponse error = (await response.Content.ReadFromJsonAsync<ApiResponse>())!;

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        error.StatusCode.Should().Be((int)response.StatusCode);
        error.Message.Should().Be("The contact could not be persisted.");
        error.ErrorCode.Should().Be("Contact.InvalidState");
    }

    private static WebApplicationFactory<Program> CreateFactory<TRepository>()
        where TRepository : class, IContactRepository
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IContactRepository>();
                services.AddSingleton<IContactRepository, TRepository>();
            });
        });
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static CreateContactRequest ValidRequest()
    {
        return new("Erfan", "Ahmadi", "09121234567", "Coworker");
    }

    private sealed class ThrowingContactRepository : StubContactRepository
    {
        public override Task AddAsync(Contact contact, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("sensitive repository failure");
        }
    }

    private sealed class FailingContactRepository : StubContactRepository
    {
        public override Task AddAsync(Contact contact, CancellationToken cancellationToken)
        {
            throw new BusinessRuleException(
                "Contact.InvalidState",
                "The contact could not be persisted.");
        }
    }

    private abstract class StubContactRepository : IContactRepository
    {
        public abstract Task AddAsync(Contact contact, CancellationToken cancellationToken);

        public Task<Contact?> GetByIdAsync(
            ContactId id,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PagedData<Contact>> GetAllAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PagedData<Contact>> GetByTagAsync(
            Tag tag,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task UpdateAsync(
            Contact contact,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task DeleteAsync(
            ContactId id,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
