# Linux image for docker compose (Pukar.Usermanagement.Host)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Pukar.Usermanagement.sln ./
COPY Pukar.Shared/Pukar.Shared.csproj Pukar.Shared/
COPY Pukar.Usermanagement.Contracts/Pukar.Usermanagement.Contracts.csproj Pukar.Usermanagement.Contracts/
COPY Pukar.Usermanagement.Domain/Pukar.Usermanagement.Domain.csproj Pukar.Usermanagement.Domain/
COPY Pukar.Usermanagement.Application/Pukar.Usermanagement.Application.csproj Pukar.Usermanagement.Application/
COPY Pukar.Usermanagement.Infrastructure/Pukar.Usermanagement.Infrastructure.csproj Pukar.Usermanagement.Infrastructure/
COPY Pukar.Usermanagement.API/Pukar.Usermanagement.API.csproj Pukar.Usermanagement.API/
COPY Pukar.Usermanagement.Host/Pukar.Usermanagement.Host.csproj Pukar.Usermanagement.Host/
RUN dotnet restore Pukar.Usermanagement.Host/Pukar.Usermanagement.Host.csproj
COPY Pukar.Shared/ Pukar.Shared/
COPY Pukar.Usermanagement.Contracts/ Pukar.Usermanagement.Contracts/
COPY Pukar.Usermanagement.Domain/ Pukar.Usermanagement.Domain/
COPY Pukar.Usermanagement.Application/ Pukar.Usermanagement.Application/
COPY Pukar.Usermanagement.Infrastructure/ Pukar.Usermanagement.Infrastructure/
COPY Pukar.Usermanagement.API/ Pukar.Usermanagement.API/
COPY Pukar.Usermanagement.Host/ Pukar.Usermanagement.Host/
RUN dotnet publish Pukar.Usermanagement.Host/Pukar.Usermanagement.Host.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Pukar.Usermanagement.Host.dll"]
