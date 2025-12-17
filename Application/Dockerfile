# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project file
COPY ["Mockups/Mockups.csproj", "Mockups/"]

# Restore dependencies
RUN dotnet restore "Mockups/Mockups.csproj"

# Copy the entire project
COPY . .

# Build the application
RUN dotnet build "Mockups/Mockups.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Mockups/Mockups.csproj" -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Expose port (change if needed)
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Mockups.dll"]
