using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PhoneBook.Api.Contracts.Contacts;
using PhoneBook.Application.Contacts.Common;

namespace PhoneBook.Api.IntegrationTests.Contacts;

public class ContactsApiTests
{
    [Fact]
    public async Task Create_should_return_201_location_and_retrievable_canonical_response()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        CreateContactRequest request = new(
            "Erfan",
            "Ahmadi",
            "0912-123-4567",
            "Coworker");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            request,
            CancellationToken.None);
        ContactResponse created = (await response.Content.ReadFromJsonAsync<ContactResponse>())!;

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (response.Headers.Location is not null).Should().BeTrue();
        Uri location = response.Headers.Location!;
        location.AbsolutePath.Should().Be($"/api/contacts/{created.Id}");
        created.FirstName.Should().Be("Erfan");
        created.LastName.Should().Be("Ahmadi");
        created.PhoneNumber.Should().Be("+989121234567");
        created.Tag.Should().Be("Coworker");
        created.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        created.UpdatedAtUtc.Should().BeNull();

        HttpResponseMessage getResponse = await client.GetAsync(
            response.Headers.Location,
            CancellationToken.None);
        ContactResponse retrieved = (await getResponse.Content.ReadFromJsonAsync<ContactResponse>())!;

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        retrieved.Equals(created).Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Ahmadi", "09121234567", "Coworker", "Validation.FirstName")]
    [InlineData("Erfan", "", "09121234567", "Coworker", "Validation.LastName")]
    [InlineData("Erfan", "Ahmadi", "invalid", "Coworker", "Contact.PhoneNumber.Invalid")]
    [InlineData("Erfan", "Ahmadi", "09121234567", "", "Validation.Tag")]
    public async Task Create_with_invalid_input_should_return_400_problem_details(
        string firstName,
        string lastName,
        string phoneNumber,
        string tag,
        string expectedCode)
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactRequest(firstName, lastName, phoneNumber, tag),
            CancellationToken.None);
        ProblemDetails problem = await ReadProblemAsync(response);

        AssertProblem(
            response,
            problem,
            HttpStatusCode.BadRequest,
            expectedCode,
            "/api/contacts");
    }

    [Theory]
    [InlineData("+989121234567")]
    [InlineData("989121234567")]
    [InlineData("00989121234567")]
    public async Task Equivalent_phone_number_should_return_409_problem_details(
        string equivalentPhoneNumber)
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        await CreateAsync(
            client,
            new("First", "Contact", "09121234567", "Coworker"));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactRequest(
                "Duplicate",
                "Contact",
                equivalentPhoneNumber,
                "Coworker"),
            CancellationToken.None);
        ProblemDetails problem = await ReadProblemAsync(response);

        AssertProblem(
            response,
            problem,
            HttpStatusCode.Conflict,
            "Contact.PhoneNumber.AlreadyExists",
            "/api/contacts");
    }

    [Fact]
    public async Task GetById_should_return_404_for_missing_contact_with_code()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        Guid missingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        HttpResponseMessage response = await client.GetAsync(
            $"/api/contacts/{missingId}",
            CancellationToken.None);
        ProblemDetails problem = await ReadProblemAsync(response);

        AssertProblem(
            response,
            problem,
            HttpStatusCode.NotFound,
            "Contact.NotFound",
            $"/api/contacts/{missingId}");
    }

    [Fact]
    public async Task GetById_should_return_400_for_empty_id()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/contacts/{Guid.Empty}",
            CancellationToken.None);
        ProblemDetails problem = await ReadProblemAsync(response);

        AssertProblem(
            response,
            problem,
            HttpStatusCode.BadRequest,
            "Validation.ContactId",
            $"/api/contacts/{Guid.Empty}");
    }

    [Fact]
    public async Task GetByTag_should_filter_preserve_order_and_audit_values()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        ContactResponse first = await CreateAsync(
            client,
            new("First", "Contact", "09121234567", "Coworker"));
        await CreateAsync(
            client,
            new("Different", "Contact", "09357654321", "Family"));
        ContactResponse second = await CreateAsync(
            client,
            new("Second", "Contact", "09135554444", "coworker"));

        HttpResponseMessage response = await client.GetAsync(
            "/api/contacts?tag=COWORKER",
            CancellationToken.None);
        ContactResponse[] contacts =
            (await response.Content.ReadFromJsonAsync<ContactResponse[]>())!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid[] expectedOrder = new[] { first, second }
            .OrderBy(contact => contact.CreatedAtUtc)
            .ThenBy(contact => contact.Id)
            .Select(contact => contact.Id)
            .ToArray();
        contacts.Select(contact => contact.Id).Should().ContainInOrder(expectedOrder);
        contacts.Should().OnlyContain(contact =>
            string.Equals(contact.Tag, "Coworker", StringComparison.OrdinalIgnoreCase));
        contacts.Should().OnlyContain(contact => contact.CreatedAtUtc.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetByTag_with_no_matches_should_return_200_empty_array()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            "/api/contacts?tag=Missing",
            CancellationToken.None);
        ContactResponse[] contacts =
            (await response.Content.ReadFromJsonAsync<ContactResponse[]>())!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        contacts.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task GetByTag_with_invalid_tag_should_return_400(string tag)
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/contacts?tag={tag}",
            CancellationToken.None);
        ProblemDetails problem = await ReadProblemAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        GetCode(problem).Should().Be("Validation.Tag");
    }

    [Fact]
    public async Task Update_should_return_updated_response_and_persist_changes()
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
        ContactResponse updated = (await response.Content.ReadFromJsonAsync<ContactResponse>())!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.FirstName.Should().Be("Sara");
        updated.LastName.Should().Be("Karimi");
        updated.PhoneNumber.Should().Be("+989357654321");
        updated.Tag.Should().Be("Friend");
        updated.CreatedAtUtc.Should().Be(created.CreatedAtUtc);
        updated.UpdatedAtUtc.Should().NotBeNull();
        updated.UpdatedAtUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
        (updated.UpdatedAtUtc >= updated.CreatedAtUtc).Should().BeTrue();

        ContactResponse retrieved = await GetAsync(client, created.Id);
        retrieved.Equals(updated).Should().BeTrue();
    }

    [Fact]
    public async Task Update_missing_contact_should_return_404()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        Guid missingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/contacts/{missingId}",
            new UpdateContactRequest("Sara", "Karimi", "09357654321", "Friend"),
            CancellationToken.None);
        ProblemDetails problem = await ReadProblemAsync(response);

        AssertProblem(
            response,
            problem,
            HttpStatusCode.NotFound,
            "Contact.NotFound",
            $"/api/contacts/{missingId}");
    }

    [Fact]
    public async Task Duplicate_update_should_return_409_and_preserve_both_contacts()
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
        ProblemDetails problem = await ReadProblemAsync(response);
        ContactResponse storedFirst = await GetAsync(client, first.Id);
        ContactResponse storedSecond = await GetAsync(client, second.Id);

        AssertProblem(
            response,
            problem,
            HttpStatusCode.Conflict,
            "Contact.PhoneNumber.AlreadyExists",
            $"/api/contacts/{first.Id}");
        storedFirst.Equals(first).Should().BeTrue();
        storedSecond.Equals(second).Should().BeTrue();
    }

    [Fact]
    public async Task Successful_phone_update_should_release_old_and_reserve_new_number()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        ContactResponse original = await CreateAsync(
            client,
            new("Original", "Contact", "09121234567", "Coworker"));
        await client.PutAsJsonAsync(
            $"/api/contacts/{original.Id}",
            new UpdateContactRequest("Original", "Contact", "09357654321", "Coworker"),
            CancellationToken.None);

        HttpResponseMessage oldNumberReuse = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactRequest("Reuse", "Old", "09121234567", "Friend"),
            CancellationToken.None);
        HttpResponseMessage newNumberDuplicate = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactRequest("Duplicate", "New", "+989357654321", "Friend"),
            CancellationToken.None);

        oldNumberReuse.StatusCode.Should().Be(HttpStatusCode.Created);
        newNumberDuplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invalid_update_should_return_400()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        ContactResponse created = await CreateAsync(
            client,
            new("Erfan", "Ahmadi", "09121234567", "Coworker"));

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/contacts/{created.Id}",
            new UpdateContactRequest("", "Karimi", "09357654321", "Friend"),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_should_return_204_remove_only_target_and_release_phone()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        ContactResponse first = await CreateAsync(
            client,
            new("First", "Contact", "09121234567", "Coworker"));
        ContactResponse second = await CreateAsync(
            client,
            new("Second", "Contact", "09357654321", "Friend"));

        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/contacts/{first.Id}",
            CancellationToken.None);
        string deleteBody = await deleteResponse.Content.ReadAsStringAsync(
            CancellationToken.None);
        HttpResponseMessage deletedGet = await client.GetAsync(
            $"/api/contacts/{first.Id}",
            CancellationToken.None);
        HttpResponseMessage remainingGet = await client.GetAsync(
            $"/api/contacts/{second.Id}",
            CancellationToken.None);
        HttpResponseMessage reuseResponse = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactRequest("Reuse", "Contact", "09121234567", "Family"),
            CancellationToken.None);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deleteBody.Should().BeEmpty();
        deletedGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
        remainingGet.StatusCode.Should().Be(HttpStatusCode.OK);
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Delete_missing_contact_should_return_404_problem_details()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateClient(factory);
        Guid missingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/contacts/{missingId}",
            CancellationToken.None);
        ProblemDetails problem = await ReadProblemAsync(response);

        AssertProblem(
            response,
            problem,
            HttpStatusCode.NotFound,
            "Contact.NotFound",
            $"/api/contacts/{missingId}");
    }

    [Fact]
    public async Task Swagger_document_should_be_available_in_development()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using HttpClient client = CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            "/swagger/v1/swagger.json",
            CancellationToken.None);
        string document = await response.Content.ReadAsStringAsync(CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Should().Contain("/api/contacts");
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
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
        return (await response.Content.ReadFromJsonAsync<ContactResponse>())!;
    }

    private static async Task<ContactResponse> GetAsync(HttpClient client, Guid id)
    {
        HttpResponseMessage response = await client.GetAsync(
            $"/api/contacts/{id}",
            CancellationToken.None);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContactResponse>())!;
    }

    private static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
    }

    private static void AssertProblem(
        HttpResponseMessage response,
        ProblemDetails problem,
        HttpStatusCode expectedStatus,
        string expectedCode,
        string expectedInstance)
    {
        response.StatusCode.Should().Be(expectedStatus);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        problem.Status.Should().Be((int)expectedStatus);
        problem.Title.Should().NotBeNullOrWhiteSpace();
        problem.Detail.Should().NotBeNullOrWhiteSpace();
        problem.Instance.Should().Be(expectedInstance);
        GetCode(problem).Should().Be(expectedCode);
    }

    private static string GetCode(ProblemDetails problem)
    {
        return problem.Extensions["code"]?.ToString() ?? string.Empty;
    }
}
