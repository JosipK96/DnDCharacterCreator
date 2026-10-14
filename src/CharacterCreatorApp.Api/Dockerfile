# Builds the API together with the Blazor client it serves, into one image.
# Build context: the repository root (docker-compose.yml and Visual Studio both use it), because
# the build needs Directory.Build.props, Directory.Packages.props and analyzers/ from there.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /repo

# Project files first, so the restore layer is reused until a package reference changes
COPY ["Directory.Build.props", "Directory.Packages.props", ".editorconfig", "./"]
COPY ["analyzers/", "analyzers/"]
COPY ["src/CharacterCreatorApp.Api/CharacterCreatorApp.Api.csproj", "src/CharacterCreatorApp.Api/"]
COPY ["src/CharacterCreatorApp.Application/CharacterCreatorApp.Application.csproj", "src/CharacterCreatorApp.Application/"]
COPY ["src/CharacterCreatorApp.Client/CharacterCreatorApp.Client.csproj", "src/CharacterCreatorApp.Client/"]
COPY ["src/CharacterCreatorApp.Contracts/CharacterCreatorApp.Contracts.csproj", "src/CharacterCreatorApp.Contracts/"]
COPY ["src/CharacterCreatorApp.Domain/CharacterCreatorApp.Domain.csproj", "src/CharacterCreatorApp.Domain/"]
COPY ["src/CharacterCreatorApp.Infrastructure/CharacterCreatorApp.Infrastructure.csproj", "src/CharacterCreatorApp.Infrastructure/"]
RUN dotnet restore "src/CharacterCreatorApp.Api/CharacterCreatorApp.Api.csproj"

COPY ["src/", "src/"]

# The analyzers and warnings-as-errors gate every local build; here they would only make the
# image slower to build, and a newer SDK in the image must not fail it over a new warning.
RUN dotnet publish "src/CharacterCreatorApp.Api/CharacterCreatorApp.Api.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore \
    -p:UseAppHost=false -p:RunAnalyzers=false -p:TreatWarningsAsErrors=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CharacterCreatorApp.Api.dll"]
