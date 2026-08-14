FROM mcr.microsoft.com/dotnet/sdk:10.0.202 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ScholarshipRequest.Shared/ScholarshipRequest.Shared.csproj src/ScholarshipRequest.Shared/
COPY src/ScholarshipRequest.Client/ScholarshipRequest.Client.csproj src/ScholarshipRequest.Client/
RUN dotnet restore src/ScholarshipRequest.Client/ScholarshipRequest.Client.csproj

COPY src/ScholarshipRequest.Shared/ src/ScholarshipRequest.Shared/
COPY src/ScholarshipRequest.Client/ src/ScholarshipRequest.Client/
RUN dotnet publish src/ScholarshipRequest.Client/ScholarshipRequest.Client.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM nginx:1.29-alpine AS runtime
COPY src/ScholarshipRequest.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
