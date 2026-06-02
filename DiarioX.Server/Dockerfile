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
# Strip the ProjectReference to the SPA esproj before restore/publish.
# The SPA is built separately in the frontend stage and copied into wwwroot,
# so we don't need the JavaScript SDK to evaluate it inside the .NET SDK image.
RUN sed -i -e '/<ProjectReference Include=".*diariox\.client\.esproj">/,/<\/ProjectReference>/d' \
           -e '/<ItemGroup>[[:space:]]*<\/ItemGroup>/d' \
           DiarioX.Server/DiarioX.Server.csproj
RUN dotnet restore DiarioX.Server/DiarioX.Server.csproj
COPY DiarioX.Server/ DiarioX.Server/
# Re-apply the strip after copying full source (the COPY above overwrites csproj).
RUN sed -i -e '/<ProjectReference Include=".*diariox\.client\.esproj">/,/<\/ProjectReference>/d' \
           -e '/<ItemGroup>[[:space:]]*<\/ItemGroup>/d' \
           DiarioX.Server/DiarioX.Server.csproj
# Copy frontend build output into wwwroot so ASP.NET serves the SPA
COPY --from=frontend /src/diariox.client/dist/ DiarioX.Server/wwwroot/
RUN dotnet publish DiarioX.Server/DiarioX.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Stage 3: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    PORT=8080
EXPOSE 8080
COPY --from=backend /app/publish ./
USER $APP_UID
# Honor the PORT env injected by hosts like Railway/Heroku, fallback to 8080.
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet DiarioX.Server.dll --urls http://0.0.0.0:${PORT:-8080}"]
