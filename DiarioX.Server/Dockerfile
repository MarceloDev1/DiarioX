# syntax=docker/dockerfile:1.7
# Multi-stage build: frontend (Vite) + backend (ASP.NET) in a single image.
# Build context must be the repository root.

# ---------- Stage 1: build frontend ----------
FROM node:22-alpine AS frontend
WORKDIR /src/diariox.client
COPY diariox.client/package*.json ./
RUN npm ci
COPY diariox.client/ ./
RUN npm run build

# ---------- Stage 2: build backend ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY DiarioX.Server/DiarioX.Server.csproj DiarioX.Server/
COPY diariox.client/diariox.client.esproj diariox.client/
RUN dotnet restore DiarioX.Server/DiarioX.Server.csproj
COPY DiarioX.Server/ DiarioX.Server/
# Copy frontend build output into wwwroot so ASP.NET serves the SPA
COPY --from=frontend /src/diariox.client/dist/ DiarioX.Server/wwwroot/
RUN dotnet publish DiarioX.Server/DiarioX.Server.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:BuildProjectReferences=false

# ---------- Stage 3: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
COPY --from=backend /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "DiarioX.Server.dll"]
