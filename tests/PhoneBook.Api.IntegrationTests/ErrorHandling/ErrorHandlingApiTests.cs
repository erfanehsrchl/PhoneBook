using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PhoneBook.Api.Contracts.Contacts;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Domain.Contacts;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Api.IntegrationTests.ErrorHandling;

public class ErrorHandlingApiTests
{
    [Fact]
    public async Task Unexpected_exception_should_return_safe_500_problem_details()
    {
        using WebApplicationFactory<Program> factory = CreateFactory<ThrowingContactRepository>();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            ValidRequest(),
            CancellationToken.None);
        string body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        ProblemDetails problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        problem.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Title.Should().Be("Internal Server Error");
        problem.Detail.Should().Be("An unexpected error occurred.");
        problem.Instance.Should().Be("/api/contacts");
        problem.Extensions["code"]?.ToString().Should().Be("Server.Unexpected");
        body.Should().NotContain("sensitive repository failure");
        body.Contains("stack trace", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }

    [Fact]
    public async Task Business_failure_should_return_422_problem_details()
    {
        using WebApplicationFactory<Program> factory = CreateFactory<FailingContactRepository>();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            ValidRequest(),
            CancellationToken.None);
        ProblemDetails problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        problem.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problem.Extensions["code"]?.ToString().Should().Be("Contact.Persistence.Failure");
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

    private class ThrowingContactRepository : StubContactRepository
    {
        public override Task<Result> AddAsync(
            Contact contact,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("sensitive repository failure");
        }
    }

    private class FailingContactRepository : StubContactRepository
    {
        public override Task<Result> AddAsync(
            Contact contact,
            CancellationToken cancellationToken)
        {
            Error error = new(
                "Contact.Persistence.Failure",
                "The contact could not be persisted.",
                ErrorType.Failure);

            return Task.FromResult(Result.Failure(error));
        }
    }

    private abstract class StubContactRepository : IContactRepository
    {
        public abstract Task<Result> AddAsync(
            Contact contact,
            CancellationToken cancellationToken);

        public Task<Contact?> GetByIdAsync(
            ContactId id,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<Contact>> GetByTagAsync(
            Tag tag,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result> UpdateAsync(
            Contact contact,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(
            ContactId id,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
