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

# Copy Directory.Packages.props to the build directory which is two levels up
COPY ["Directory.Packages.props", "./"]

# Copy the project files and restore dependencies
COPY ["/Agents/dotnet/LightingAgent/LightingAgent.csproj", "Agents/dotnet/LightingAgent/"]
COPY ["/Agents/dotnet/Shared/Shared.csproj", "Agents/dotnet/Shared/"]
RUN dotnet restore "Agents/dotnet/LightingAgent/LightingAgent.csproj"

# Copy the remaining files and publish the project
COPY config.json .
COPY /Plugins ./Plugins
COPY /Agents/dotnet/LightingAgent ./Agents/dotnet/LightingAgent
COPY /Agents/dotnet/Shared ./Agents/dotnet/Shared
WORKDIR "/src/Agents/dotnet/LightingAgent"
RUN dotnet publish "LightingAgent.csproj" -c Release -o /app/publish

# Build the final image using the .NET 8 ASP.NET base image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LightingAgent.dll"]
