FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TripFlow.Domain/TripFlow.Domain.csproj TripFlow.Domain/
COPY TripFlow.Application/TripFlow.Application.csproj TripFlow.Application/
COPY TripFlow.Infrastructure/TripFlow.Infrastructure.csproj TripFlow.Infrastructure/
COPY TripFlow.Api/TripFlow.Api.csproj TripFlow.Api/
RUN dotnet restore TripFlow.Api/TripFlow.Api.csproj

COPY TripFlow.Domain/ TripFlow.Domain/
COPY TripFlow.Application/ TripFlow.Application/
COPY TripFlow.Infrastructure/ TripFlow.Infrastructure/
COPY TripFlow.Api/ TripFlow.Api/
RUN dotnet publish TripFlow.Api/TripFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TripFlow.Api.dll"]
