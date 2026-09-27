FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/Core/ shared/Core/
COPY shared/Infrastructure/ shared/Infrastructure/
COPY web-api/ web-api/
RUN dotnet restore web-api/TravelAdvisor.Api.csproj
RUN dotnet publish web-api/TravelAdvisor.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TravelAdvisor.Api.dll"]
