FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG BUILD_CONFIGURATION=Debug

COPY ["PhotoshopAutomation.Api/src/PhotoshopAutomation.Api/PhotoshopAutomationApi.csproj", "PhotoshopAutomation.Api/src/PhotoshopAutomation.Api/"]
COPY ["MockupWorkflow.Shared/src/MockupWorkflow.Shared/MockupWorkflow.Shared.csproj", "MockupWorkflow.Shared/src/MockupWorkflow.Shared/"]
RUN dotnet restore "PhotoshopAutomation.Api/src/PhotoshopAutomation.Api/PhotoshopAutomationApi.csproj"

COPY . .

RUN dotnet publish "PhotoshopAutomation.Api/src/PhotoshopAutomation.Api/PhotoshopAutomationApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PhotoshopAutomationApi.dll"]