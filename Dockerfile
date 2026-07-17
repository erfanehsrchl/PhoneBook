FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app

FROM mcr.microsoft.com/dotnet/sdk:9.0.102 AS restore
WORKDIR /src
COPY ["PhoneBook.sln", "Directory.Build.props", "global.json", "NuGet.config", "./"]
COPY ["src/PhoneBook.Domain/PhoneBook.Domain.csproj", "src/PhoneBook.Domain/"]
COPY ["src/PhoneBook.Application/PhoneBook.Application.csproj", "src/PhoneBook.Application/"]
COPY ["src/PhoneBook.Infrastructure/PhoneBook.Infrastructure.csproj", "src/PhoneBook.Infrastructure/"]
COPY ["src/PhoneBook.Api/PhoneBook.Api.csproj", "src/PhoneBook.Api/"]
COPY ["tests/PhoneBook.Domain.UnitTests/PhoneBook.Domain.UnitTests.csproj", "tests/PhoneBook.Domain.UnitTests/"]
COPY ["tests/PhoneBook.Application.UnitTests/PhoneBook.Application.UnitTests.csproj", "tests/PhoneBook.Application.UnitTests/"]
COPY ["tests/PhoneBook.Infrastructure.UnitTests/PhoneBook.Infrastructure.UnitTests.csproj", "tests/PhoneBook.Infrastructure.UnitTests/"]
COPY ["tests/PhoneBook.Api.IntegrationTests/PhoneBook.Api.IntegrationTests.csproj", "tests/PhoneBook.Api.IntegrationTests/"]
RUN dotnet restore "PhoneBook.sln"

FROM restore AS build
COPY . .
RUN dotnet build "PhoneBook.sln" --configuration Release --no-restore

FROM build AS publish
RUN dotnet publish "src/PhoneBook.Api/PhoneBook.Api.csproj" --configuration Release --no-restore --no-build --output /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PhoneBook.Api.dll"]
