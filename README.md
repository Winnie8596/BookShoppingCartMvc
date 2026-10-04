

## Tech stack 🧑‍💻

   - Dotnet core mvc (.Net 10.0)
   - MS SQLServer 2025 (Database)
   - Entity Framework Core (ORM)
   - Identity Core (Authentication)
   - Bootstrap 5 (frontend)

## Tools I have used and their alternative (Updated versions)

- Visual Studio 2026 (Alternatives : .NET SDK + VS Code or .NET SDK + JetBrains Rider).
- Microsoft Sql Server Management Studio (Alternative : mssql extension for vscode / dbeaver).
- Instead of manually installing `sql server`, you can also used `sql server` which is spun up in `docker`.

## Book assistant (optional)

`/Assistant` lets customers ask about the catalog in plain English. Our own database finds the books, and Google Gemini only writes the answer. Without a key the rest of the shop works as normal and the assistant just says it's unavailable.

Get a free key from [Google AI Studio](https://aistudio.google.com/apikey), then:

```bash
cd BookShoppingCartMvcUI
dotnet user-secrets set "Gemini:ApiKey" "<your key>"
```

With docker, set `GEMINI_API_KEY` before `docker compose up`. Don't put the key in `appsettings.json`. The model, timeout and token limits are in the `Gemini` section there.

## Deploy to a server

Any Linux VPS with Docker and at least 4 GB RAM works (sql server needs about 2 GB). Point your domain's A record at the server first, so caddy can get an https certificate.

```bash
git clone <this repo> && cd <repo>
cp .env.example .env    # fill in DOMAIN, MSSQL_SA_PASSWORD, ADMIN_PASSWORD, GEMINI_API_KEY
docker compose -f compose.prod.yaml up -d --build
```

A one-off `migrate` container applies the EF migrations (`dotnet BookShoppingCartMvcUI.dll --migrate`) before the site starts, then the app seeds the database on first start. Log in with `ADMIN_EMAIL` (`admin@gmail.com` if you left it empty) and the `ADMIN_PASSWORD` you set. Uploaded book images, the database and login keys are kept in docker volumes, so `git pull` and running the same command again updates the app without losing data.

Only ports 80 and 443 are opened, sql server isn't reachable from outside.
