FROM node:22-alpine AS frontend
WORKDIR /app/client
COPY diariox.client/package*.json ./
RUN npm ci
COPY diariox.client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /app
COPY DiarioX.Server/*.csproj ./DiarioX.Server/
RUN dotnet restore DiarioX.Server/DiarioX.Server.csproj -p:BuildFrontend=false
COPY DiarioX.Server/ ./DiarioX.Server/
COPY --from=frontend /app/client/dist ./DiarioX.Server/wwwroot
RUN dotnet publish DiarioX.Server/DiarioX.Server.csproj -c Release -o /out -p:BuildFrontend=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend /out ./
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["dotnet", "DiarioX.Server.dll"]
