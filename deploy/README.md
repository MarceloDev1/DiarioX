# Deploy do DiarioX

Este diretório contém artefatos para colocar o DiarioX em produção.
A API ASP.NET Core já serve o frontend (Vite) a partir de `wwwroot`,
então o deploy é de **uma única aplicação** + PostgreSQL.

## Conteúdo

- `../DiarioX.Server/Dockerfile` – build multi-stage (frontend + backend) em uma imagem.
- `../infra/docker-compose.prod.yml` – stack de produção (api + postgres).
- `../infra/.env.example` – variáveis de ambiente necessárias.
- `diariox.service` – unit systemd para deploy "bare metal" (sem Docker).
- `nginx-diariox.conf` – proxy reverso Nginx com HTTPS.

---

## ⚠️ Antes de qualquer deploy

1. **Rotacione segredos expostos no repositório:**
   - `DiarioX.Server/appsettings.Development.json` contém credenciais SMTP reais.
     Troque a senha no provedor (Brevo) imediatamente.
   - `DiarioX.Server/appsettings.json` contém uma `Jwt:Key` placeholder; gere uma nova.
2. **Não** edite `appsettings.json` para colocar segredos de produção. Use variáveis de ambiente
   (formato `Section__Key`, ex.: `Jwt__Key`, `ConnectionStrings__DefaultConnection`).
3. Considere migrar `db.Database.EnsureCreated()` para EF Migrations em
   `DiarioX.Server/Program.cs` antes de versionar o schema em produção.

---

## Opção A — Deploy com Docker Compose (recomendado)

Pré-requisitos no servidor: Docker Engine + Docker Compose plugin.

```bash
# 1. Clonar o repositório no servidor
git clone <url-do-repo> /opt/diariox
cd /opt/diariox/infra

# 2. Configurar variáveis
cp .env.example .env
chmod 600 .env
nano .env   # preencha POSTGRES_PASSWORD, JWT_KEY, SMTP_*, APP_URL

# 3. Build e subida
docker compose -f docker-compose.prod.yml --env-file .env up -d --build

# 4. Verificar
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f api
```

A API ficará escutando em `127.0.0.1:8080` no host.
Coloque o Nginx (ver Opção C) na frente para HTTPS e domínio público.

Atualizações:
```bash
git pull
docker compose -f docker-compose.prod.yml --env-file .env up -d --build
```

---

## Opção B — Deploy "bare metal" com systemd

Pré-requisitos no servidor (Ubuntu/Debian):
- ASP.NET Core Runtime 10
- PostgreSQL 16 (local ou gerenciado)
- Nginx

```bash
# Na máquina de build (Windows ou Linux):
cd diariox.client
npm ci
npm run build
cd ..

# Copia o build do SPA para o wwwroot do backend
rm -rf DiarioX.Server/wwwroot/*
cp -r diariox.client/dist/* DiarioX.Server/wwwroot/

dotnet publish DiarioX.Server/DiarioX.Server.csproj -c Release -o ./publish
```

No servidor:
```bash
sudo mkdir -p /var/www/diariox /etc/diariox
sudo rsync -av ./publish/ /var/www/diariox/
sudo chown -R www-data:www-data /var/www/diariox

# Variáveis de ambiente (segredos)
sudo nano /etc/diariox/diariox.env
sudo chmod 600 /etc/diariox/diariox.env
sudo chown www-data:www-data /etc/diariox/diariox.env
```

Conteúdo de `/etc/diariox/diariox.env` (sem aspas, formato KEY=VALUE):
```
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=diariox;Username=diariox;Password=...
AppUrl=https://diariox.example.com
Jwt__Key=...
Jwt__Issuer=DiarioX.Server
Jwt__Audience=DiarioX.Client
Jwt__ExpiresInMinutes=60
Smtp__Host=...
Smtp__Port=587
Smtp__Username=...
Smtp__Password=...
Smtp__FromEmail=no-reply@example.com
Smtp__FromName=Diário de Classe
```

Instale o serviço:
```bash
sudo cp deploy/diariox.service /etc/systemd/system/diariox.service
sudo systemctl daemon-reload
sudo systemctl enable --now diariox
sudo systemctl status diariox
journalctl -u diariox -f
```

---

## Opção C — Nginx + HTTPS (vale para A e B)

```bash
sudo cp deploy/nginx-diariox.conf /etc/nginx/sites-available/diariox
sudo sed -i 's/diariox.example.com/SEU_DOMINIO/g' /etc/nginx/sites-available/diariox
sudo ln -s /etc/nginx/sites-available/diariox /etc/nginx/sites-enabled/diariox
sudo nginx -t && sudo systemctl reload nginx

# TLS (Let's Encrypt)
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d SEU_DOMINIO
```

---

## Pós-deploy

1. Acesse `https://SEU_DOMINIO` e valide o login.
2. **Troque a senha do admin seed** (`admin@diariox.local` / `admin123`) gerado no
   primeiro start em `DiarioX.Server/Program.cs`. Idealmente, remova o seed antes
   de subir para produção.
3. Configure backups do volume `postgres_data` (Opção A) ou do diretório de dados
   do PostgreSQL (Opção B).
