
# Stage 1: ASP.NET Core runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Stage 2: Restore and publish the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SmartHelpDesk.API/SmartHelpDesk.API.csproj", "SmartHelpDesk.API/"]
COPY ["SmartHelpdesk.BLL/SmartHelpdesk.BLL.csproj", "SmartHelpdesk.BLL/"]
COPY ["SmartHelpdesk.Common/SmartHelpdesk.Common.csproj", "SmartHelpdesk.Common/"]
COPY ["SmartHelpdesk.DAL/SmartHelpdesk.DAL.csproj", "SmartHelpdesk.DAL/"]

RUN dotnet restore "SmartHelpDesk.API/SmartHelpDesk.API.csproj"

COPY . .
WORKDIR /src/SmartHelpDesk.API

RUN dotnet publish "SmartHelpDesk.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartHelpDesk.API.dll"]
