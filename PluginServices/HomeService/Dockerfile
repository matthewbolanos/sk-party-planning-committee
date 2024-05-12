# Use the .NET 8 ASP.NET base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=http://+:80

# Install curl in the base image
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Use the .NET 8 SDK image for the build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy Directory.Packages.props to the build directory
COPY ["/Directory.Packages.props", "./"]

# Copy the project files and restore dependencies
COPY ["/PluginServices/HomeService/HomeService.csproj", "PluginServices/HomeService/"]
COPY ["/PluginServices/Shared/Shared.csproj", "PluginServices/Shared/"]
RUN dotnet restore "PluginServices/HomeService/HomeService.csproj"

# Copy the remaining files and publish the project

COPY /PluginServices/HomeService ./PluginServices/HomeService
COPY /PluginServices/Shared ./PluginServices/Shared
WORKDIR "/src/PluginServices/HomeService"
RUN dotnet publish "HomeService.csproj" -c Release -o /app/publish

# Build the final image using the .NET 8 ASP.NET base image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HomeService.dll"]
