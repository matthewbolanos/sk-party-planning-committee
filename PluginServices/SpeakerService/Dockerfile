# Use the .NET 8 ASP.NET base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=http://+:80

# Use the .NET 8 SDK image for the build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project files and restore dependencies
COPY ["/SpeakerService/SpeakerService.csproj", "SpeakerService/"]
COPY ["/Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "SpeakerService/SpeakerService.csproj"

# Copy the remaining files and publish the project
COPY . .
WORKDIR "/src/SpeakerService"
RUN dotnet publish "SpeakerService.csproj" -c Release -o /app/publish

# Build the final image using the .NET 8 ASP.NET base image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SpeakerService.dll"]
