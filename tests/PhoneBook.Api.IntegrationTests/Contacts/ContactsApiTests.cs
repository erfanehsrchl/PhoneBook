using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using PhoneBook.Api.Contracts;
using PhoneBook.Api.Contracts.Contacts;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Api.IntegrationTests.Contacts;

public class ContactsApiTests
{
    [Fact]
    public async Task Create_and_GetById_should_return_success_envelopes()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactRequest("Erfan", "Ahmadi", "0912-123-4567", "Coworker"),
            CancellationToken.None);
        ApiResponse<ContactResponse> createEnvelope =
            await ReadAsync<ApiResponse<ContactResponse>>(createResponse);
        ContactResponse created = createEnvelope.Data!;

        AssertSuccess(createResponse, createEnvelope, HttpStatusCode.Created);
        createEnvelope.Message.Should().Be("Contact created successfully.");
        createResponse.Headers.Location!.AbsolutePath.Should().Be($"/api/contacts/{created.Id}");
        created.PhoneNumber.Should().Be("+989121234567");
        created.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        created.UpdatedAtUtc.Should().BeNull();

        HttpResponseMessage getResponse = await client.GetAsync(
            createResponse.Headers.Location,
            CancellationToken.None);
        ApiResponse<ContactResponse> getEnvelope =
            await ReadAsync<ApiResponse<ContactResponse>>(getResponse);

        AssertSuccess(getResponse, getEnvelope, HttpStatusCode.OK);
        getEnvelope.Message.Should().Be("Contact retrieved successfully.");
        getEnvelope.Data!.Equals(created).Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Ahmadi", "09121234567", "Coworker", "firstName")]
    [InlineData("Erfan", "", "09121234567", "Coworker", "lastName")]
    [InlineData("Erfan", "Ahmadi", "invalid", "Coworker", "phoneNumber")]
    [InlineData("Erfan", "Ahmadi", "09121234567", "", "tag")]
    public async Task Create_with_invalid_input_should_return_400_error_envelope(
        string firstName,
        string lastName,
        string phoneNumber,
        string tag,
        string expectedProperty)
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactRequest(firstName, lastName, phoneNumber, tag),
            CancellationToken.None);

        ValidationApiResponse error = await ReadAsync<ValidationApiResponse>(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.StatusCode.Should().Be((int)response.StatusCode);
        error.Message.Should().Be("One or more validation errors occurred.");
        error.ErrorCode.Should().Be("Validation.Failed");
        error.Errors.Should().ContainKey(expectedProperty);
    }

    [Fact]
    public async Task Invalid_json_should_use_custom_model_binding_error_envelope()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        using StringContent content = new("{ invalid json", System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(
            "/api/contacts",
            content,
            CancellationToken.None);

        ApiResponse error = await AssertErrorAsync(
            response,
            HttpStatusCode.BadRequest,
            "Request.Invalid");
        error.Message.Should().Be("The request is invalid.");
    }

    [Fact]
    public async Task Equivalent_phone_number_should_return_409_error_envelope()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        await CreateAsync(client, new("First", "Contact", "09121234567", "Coworker"));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactRequest("Duplicate", "Contact", "00989121234567", "Coworker"),
            CancellationToken.None);

        await AssertErrorAsync(
            response,
            HttpStatusCode.Conflict,
            "Contact.PhoneNumberConflict");
    }

    [Fact]
    public async Task GetById_with_missing_contact_should_return_404_error_envelope()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        Guid missingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        HttpResponseMessage response = await client.GetAsync(
            $"/api/contacts/{missingId}",
            CancellationToken.None);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Contact.NotFound");
    }

    [Fact]
    public async Task GetAll_without_parameters_should_return_default_empty_page()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            "/api/contacts",
            CancellationToken.None);
        ApiResponse<PagedResponse<ContactResponse>> envelope =
            await ReadAsync<ApiResponse<PagedResponse<ContactResponse>>>(response);

        AssertSuccess(response, envelope, HttpStatusCode.OK);
        envelope.Message.Should().Be("Contacts retrieved successfully.");
        envelope.Data!.Items.Should().BeEmpty();
        envelope.Data.PageNumber.Should().Be(1);
        envelope.Data.PageSize.Should().Be(20);
        envelope.Data.TotalCount.Should().Be(0);
        envelope.Data.TotalPages.Should().Be(0);
        envelope.Data.HasPreviousPage.Should().BeFalse();
        envelope.Data.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_should_paginate_in_deterministic_order()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        ContactResponse first = await CreateAsync(
            client,
            new("First", "Contact", "09121234567", "Coworker"));
        ContactResponse second = await CreateAsync(
            client,
            new("Second", "Contact", "09357654321", "Family"));
        ContactResponse third = await CreateAsync(
            client,
            new("Third", "Contact", "09135554444", "Friend"));

        HttpResponseMessage response = await client.GetAsync(
            "/api/contacts?pageNumber=2&pageSize=2",
            CancellationToken.None);
        ApiResponse<PagedResponse<ContactResponse>> envelope =
            await ReadAsync<ApiResponse<PagedResponse<ContactResponse>>>(response);
        ContactResponse expected = new[] { first, second, third }
            .OrderBy(contact => contact.CreatedAtUtc)
            .ThenBy(contact => contact.Id)
            .Last();

        AssertSuccess(response, envelope, HttpStatusCode.OK);
        envelope.Data!.Items.Should().ContainSingle();
        envelope.Data.Items.Single().Equals(expected).Should().BeTrue();
        envelope.Data.PageNumber.Should().Be(2);
        envelope.Data.PageSize.Should().Be(2);
        envelope.Data.TotalCount.Should().Be(3);
        envelope.Data.TotalPages.Should().Be(2);
        envelope.Data.HasPreviousPage.Should().BeTrue();
        envelope.Data.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetByTag_should_use_dedicated_route_and_paginate_matches()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        await CreateAsync(client, new("First", "Contact", "09121234567", "Coworker"));
        await CreateAsync(client, new("Different", "Contact", "09357654321", "Family"));
        await CreateAsync(client, new("Second", "Contact", "09135554444", "coworker"));

        HttpResponseMessage response = await client.GetAsync(
            "/api/contacts/by-tag/COWORKER?pageNumber=1&pageSize=1",
            CancellationToken.None);
        ApiResponse<PagedResponse<ContactResponse>> envelope =
            await ReadAsync<ApiResponse<PagedResponse<ContactResponse>>>(response);

        AssertSuccess(response, envelope, HttpStatusCode.OK);
        envelope.Data!.Items.Should().ContainSingle();
        envelope.Data.Items.Single().Tag.Should().BeEquivalentTo("Coworker");
        envelope.Data.TotalCount.Should().Be(2);
        envelope.Data.TotalPages.Should().Be(2);
        envelope.Data.HasPreviousPage.Should().BeFalse();
        envelope.Data.HasNextPage.Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/contacts?pageNumber=0&pageSize=20", "Validation.Failed")]
    [InlineData("/api/contacts?pageNumber=1&pageSize=0", "Validation.Failed")]
    [InlineData("/api/contacts?pageNumber=1&pageSize=101", "Validation.Failed")]
    [InlineData("/api/contacts/by-tag/Friend?pageNumber=0&pageSize=20", "Validation.Failed")]
    public async Task List_endpoints_should_validate_pagination(
        string route,
        string expectedCode)
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync(route, CancellationToken.None);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, expectedCode);
    }

    [Fact]
    public async Task GetByTag_with_no_matches_should_return_successful_empty_page()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            "/api/contacts/by-tag/Missing",
            CancellationToken.None);
        ApiResponse<PagedResponse<ContactResponse>> envelope =
            await ReadAsync<ApiResponse<PagedResponse<ContactResponse>>>(response);

        AssertSuccess(response, envelope, HttpStatusCode.OK);
        envelope.Data!.Items.Should().BeEmpty();
        envelope.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_should_return_success_envelope_and_persist_changes()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        ContactResponse created = await CreateAsync(
            client,
            new("Erfan", "Ahmadi", "09121234567", "Coworker"));

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/contacts/{created.Id}",
            new UpdateContactRequest("Sara", "Karimi", "09357654321", "Friend"),
            CancellationToken.None);
        ApiResponse<ContactResponse> envelope =
            await ReadAsync<ApiResponse<ContactResponse>>(response);

        AssertSuccess(response, envelope, HttpStatusCode.OK);
        envelope.Message.Should().Be("Contact updated successfully.");
        envelope.Data!.FirstName.Should().Be("Sara");
        envelope.Data.PhoneNumber.Should().Be("+989357654321");
        envelope.Data.UpdatedAtUtc.Should().NotBeNull();
        (await GetAsync(client, created.Id)).Equals(envelope.Data).Should().BeTrue();
    }

    [Fact]
    public async Task Update_with_duplicate_phone_should_return_409_and_preserve_contacts()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        ContactResponse first = await CreateAsync(
            client,
            new("First", "Contact", "09121234567", "Coworker"));
        ContactResponse second = await CreateAsync(
            client,
            new("Second", "Contact", "09357654321", "Friend"));

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/contacts/{first.Id}",
            new UpdateContactRequest("Changed", "Contact", second.PhoneNumber, "Changed"),
            CancellationToken.None);

        await AssertErrorAsync(
            response,
            HttpStatusCode.Conflict,
            "Contact.PhoneNumberConflict");
        (await GetAsync(client, first.Id)).Equals(first).Should().BeTrue();
        (await GetAsync(client, second.Id)).Equals(second).Should().BeTrue();
    }

    [Fact]
    public async Task Update_with_missing_contact_should_return_404_error_envelope()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        Guid missingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/contacts/{missingId}",
            new UpdateContactRequest("Sara", "Karimi", "09357654321", "Friend"),
            CancellationToken.None);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Contact.NotFound");
    }

    [Fact]
    public async Task Delete_should_return_204_without_body_and_release_phone_number()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        ContactResponse contact = await CreateAsync(
            client,
            new("First", "Contact", "09121234567", "Coworker"));

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/contacts/{contact.Id}",
            CancellationToken.None);
        string body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        HttpResponseMessage reuseResponse = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactRequest("Reuse", "Contact", "09121234567", "Family"),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        body.Should().BeEmpty();
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Delete_with_missing_contact_should_return_404_error_envelope()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        Guid missingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/contacts/{missingId}",
            CancellationToken.None);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Contact.NotFound");
    }

    [Fact]
    public async Task Swagger_should_document_new_routes_and_response_contracts()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync(
            "/swagger/v1/swagger.json",
            CancellationToken.None);
        string document = await response.Content.ReadAsStringAsync(CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Should().Contain("/api/contacts");
        document.Should().Contain("/api/contacts/by-tag/{tag}");
        document.Should().Contain("ApiResponse");
        document.Should().Contain("PagedResponse");
        document.Should().Contain("ValidationApiResponse");
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        WebApplicationFactory<Program> configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureLogging(logging => logging.ClearProviders());
        });

        return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static async Task<ContactResponse> CreateAsync(
        HttpClient client,
        CreateContactRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            request,
            CancellationToken.None);
        response.EnsureSuccessStatusCode();
        ApiResponse<ContactResponse> envelope =
            await ReadAsync<ApiResponse<ContactResponse>>(response);
        return envelope.Data!;
    }

    private static async Task<ContactResponse> GetAsync(HttpClient client, Guid id)
    {
        HttpResponseMessage response = await client.GetAsync(
            $"/api/contacts/{id}",
            CancellationToken.None);
        response.EnsureSuccessStatusCode();
        ApiResponse<ContactResponse> envelope =
            await ReadAsync<ApiResponse<ContactResponse>>(response);
        return envelope.Data!;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static void AssertSuccess<T>(
        HttpResponseMessage response,
        ApiResponse<T> envelope,
        HttpStatusCode expectedStatus)
    {
        response.StatusCode.Should().Be(expectedStatus);
        envelope.StatusCode.Should().Be((int)response.StatusCode);
        envelope.ErrorCode.Should().BeNull();
        (envelope.Data is not null).Should().BeTrue();
    }

    private static async Task<ApiResponse> AssertErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        ApiResponse error = await ReadAsync<ApiResponse>(response);

        response.StatusCode.Should().Be(expectedStatus);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        error.StatusCode.Should().Be((int)response.StatusCode);
        error.Message.Should().NotBeNullOrWhiteSpace();
        error.ErrorCode.Should().Be(expectedCode);

        return error;
    }
}
