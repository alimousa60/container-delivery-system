# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ContainerDelivery.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/ContainerDelivery.Core/ContainerDelivery.Core.csproj", "src/ContainerDelivery.Core/"]
COPY ["src/ContainerDelivery.Application/ContainerDelivery.Application.csproj", "src/ContainerDelivery.Application/"]
COPY ["src/ContainerDelivery.Infrastructure/ContainerDelivery.Infrastructure.csproj", "src/ContainerDelivery.Infrastructure/"]
COPY ["src/ContainerDelivery.Api/ContainerDelivery.Api.csproj", "src/ContainerDelivery.Api/"]

# Restore dependencies
RUN dotnet restore "src/ContainerDelivery.Api/ContainerDelivery.Api.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/src/ContainerDelivery.Api"
RUN dotnet publish "ContainerDelivery.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser

# Copy published output
COPY --from=build /app/publish .

# Make uploads directory writable for the runtime user
RUN mkdir -p /app/wwwroot/uploads/logo && chown -R appuser:appuser /app

USER appuser

# Expose port
EXPOSE 8080

# Health check (bash /dev/tcp - environment blocks apt/gpgv so curl is not installed)
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080 && printf "GET /health HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n" >&3 && read -t 5 -r line <&3 && [[ "$line" == *200* ]]'

# Entry point
ENTRYPOINT ["dotnet", "ContainerDelivery.Api.dll"]