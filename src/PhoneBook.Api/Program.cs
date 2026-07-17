using PhoneBook.Application;
using Microsoft.AspNetCore.Mvc;
using PhoneBook.Api.Contracts;
using PhoneBook.Api.ExceptionHandling;
using PhoneBook.Api.Mappings;
using PhoneBook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = _ =>
        new BadRequestObjectResult(
            new ApiResponse(
                StatusCodes.Status400BadRequest,
                "The request is invalid.",
                "Request.Invalid"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication(typeof(ApiMappingConfig).Assembly);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddExceptionHandler(options =>
{
    options.AllowStatusCode404Response = true;
    options.ExceptionHandler = async httpContext =>
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiResponse(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "Server.UnexpectedError"),
            httpContext.RequestAborted);
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

/// <summary>
/// Provides an entry point accessible to integration tests.
/// </summary>
public partial class Program;
