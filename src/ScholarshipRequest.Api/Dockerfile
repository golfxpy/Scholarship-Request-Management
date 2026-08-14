FROM mcr.microsoft.com/dotnet/sdk:10.0.202 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ScholarshipRequest.Shared/ScholarshipRequest.Shared.csproj src/ScholarshipRequest.Shared/
COPY src/ScholarshipRequest.Api/ScholarshipRequest.Api.csproj src/ScholarshipRequest.Api/
RUN dotnet restore src/ScholarshipRequest.Api/ScholarshipRequest.Api.csproj

COPY src/ScholarshipRequest.Shared/ src/ScholarshipRequest.Shared/
COPY src/ScholarshipRequest.Api/ src/ScholarshipRequest.Api/
RUN dotnet publish src/ScholarshipRequest.Api/ScholarshipRequest.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.6 AS runtime
WORKDIR /app

USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
RUN mkdir -p /var/lib/scholarship/keys \
    && chown -R app:app /app /var/lib/scholarship

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ScholarshipRequest.Api.dll"]
