# ─── Stage 1: Build ────────────────────────────────────────────────────────────
# Use the exact SDK version referenced in global.json so the SDK resolver
# never complains about a version mismatch inside the container.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore NuGet packages (layer-cached when .csproj files don't change)
COPY ["GradeManagementSystem.Api/GradeManagementSystem.Api.csproj",       "GradeManagementSystem.Api/"]
COPY ["GradeManagementSystem.Core/GradeManagementSystem.Core.csproj",     "GradeManagementSystem.Core/"]
COPY ["GradeManagementSystem.Repository/GradeManagementSystem.Repository.csproj", "GradeManagementSystem.Repository/"]
COPY ["GradeManagementSystem.Services/GradeManagementSystem.Services.csproj",   "GradeManagementSystem.Services/"]

RUN dotnet restore "GradeManagementSystem.Api/GradeManagementSystem.Api.csproj"

# Copy full source and publish
COPY . .

# Remove the global.json SDK pin so the container's installed SDK is used.
# (The pinned version 8.0.128 may not match the SDK shipped in this image.)
RUN rm -f global.json

WORKDIR "/src/GradeManagementSystem.Api"
RUN dotnet publish "GradeManagementSystem.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ─── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render.com (and most PaaS) handles TLS at the edge and forwards plain HTTP
# to containers, so we listen on plain HTTP port 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GradeManagementSystem.Api.dll"]
