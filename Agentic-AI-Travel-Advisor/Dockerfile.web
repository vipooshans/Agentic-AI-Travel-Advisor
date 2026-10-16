FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/Core/ shared/Core/
COPY shared/Infrastructure/ shared/Infrastructure/
COPY web-app/ web-app/
RUN dotnet restore web-app/TravelAdvisor.Web.csproj
RUN dotnet publish web-app/TravelAdvisor.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TravelAdvisor.Web.dll"]
