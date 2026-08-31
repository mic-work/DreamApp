# DreamApp 🎁

DreamApp è una web app personale per gestire e condividere wishlist/regali
tra due utenti (pensata per l'uso quotidiano da iPhone). Non è un'app
nativa iOS: è una **Angular Web App + PWA** installabile sulla schermata
Home tramite Safari ("Condividi → Aggiungi alla schermata Home"), che
parla con un backend **ASP.NET Core** che a sua volta usa **SQLite** come
unico database condiviso.

## Funzionalità

- Registrazione e login con JWT; sessione persistente sul dispositivo.
- Una wishlist personale per utente, creata automaticamente alla
  registrazione.
- Aggiunta, modifica ed eliminazione dei propri regali (nome,
  descrizione, note, foto, link al prodotto, priorità Bassa/Media/Alta).
- Ricerca di un altro utente per username e richiesta di accesso alla sua
  wishlist ("aggiungi amico"); la wishlist dell'amico è sempre di sola
  lettura (nessun pulsante modifica/elimina).
- Rimozione di una lista dai propri "amici" senza intaccare la wishlist o
  i regali del proprietario.
- Autorizzazione lato server su ogni operazione: un utente non può mai
  modificare un regalo o una wishlist che non gli appartiene, anche
  provando a manipolare gli id nelle richieste HTTP.
- PWA installabile su iPhone (manifest, service worker, icone, meta tag
  per Safari).

## Architettura

```
Angular / PWA  →  HTTPS/REST  →  ASP.NET Core API  →  Entity Framework Core  →  SQLite
```

Il frontend è pubblicato come sito statico su **GitHub Pages**; il backend
gira su un hosting compatibile ASP.NET Core (GitHub Pages ospita solo
frontend statico, mai il backend). Dettagli completi, scelte e motivazioni
in [`docs/architecture.md`](docs/architecture.md).

## Struttura del repository

```
DreamApp/
├── frontend/dreamapp-web/     Angular 19 (standalone) + PWA
├── backend/
│   ├── DreamApp.Api/          ASP.NET Core 8 Web API (Controllers, Models, DTOs, Data, Services)
│   └── DreamApp.Api.Tests/    Test xUnit (unit + integration)
├── database/                  Documentazione database (il file .db NON è nel repo)
├── docs/                      architecture.md, database.md, api.md
└── .github/workflows/         deploy-frontend.yml (CI/CD verso GitHub Pages)
```

## Requisiti

- **Node.js** 20+ e npm (frontend). Angular CLI non è richiesto
  globalmente: si usa tramite `npx`/`npm run`.
- **.NET SDK 8** (backend).
- **dotnet-ef** (tool EF Core), solo per generare/applicare le migration:
  `dotnet tool install --global dotnet-ef`.

## Installazione frontend

```bash
cd frontend/dreamapp-web
npm install
```

## Installazione backend

```bash
cd backend/DreamApp.Api
dotnet restore
```

## Database e migration

SQLite, un unico file condiviso, mai nel repository (vedi
[`.gitignore`](.gitignore) e [`database/README.md`](database/README.md)).
Schema completo in [`docs/database.md`](docs/database.md).

Prima del primo avvio, genera e applica la migration iniziale:

```bash
cd backend/DreamApp.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Da quel momento il backend applica automaticamente le migration pendenti
ad ogni avvio.

> **Nota sull'ambiente in cui è stato preparato questo repository:** il
> codice è stato scritto e revisionato in un ambiente sandbox senza
> accesso alla rete NuGet (solo npm/PyPI erano raggiungibili), quindi
> `dotnet restore`/`build`/`test` sul backend **non hanno potuto essere
> eseguiti automaticamente qui** e la migration `InitialCreate` non è
> stata generata. Il frontend, invece, è stato installato, compilato
> (`ng build`, sia in configurazione normale sia con `--base-href` come
> farà la pipeline GitHub Pages) e testato (`ng test`, 16 test, tutti
> superati) in questo stesso ambiente. Sul tuo computer (che ha già
> Visual Studio configurato) esegui i comandi sopra e poi
> `dotnet build` / `dotnet test` per completare la verifica del backend.

## Configurazione (variabili d'ambiente)

Il backend non ha **nessun segreto** hardcoded. Configurazione richiesta:

| Variabile         | Obbligatoria | Descrizione                                                                 |
|-------------------|:---:|----------------------------------------------------------------------------|
| `Jwt__Key`        | ✅  | Chiave di firma JWT. In sviluppo: `dotnet user-secrets set "Jwt:Key" "<stringa-lunga-casuale>"` dentro `backend/DreamApp.Api`. In produzione: variabile d'ambiente. |
| `DATABASE_PATH`   | no  | Percorso del file SQLite. Default: `App_Data/dreamapp.db` (relativo al backend). In produzione deve puntare a storage persistente. |
| `IMAGES_PATH`     | no  | Cartella delle foto caricate. Default: `App_Data/uploads`.                     |
| `Jwt__Issuer` / `Jwt__Audience` / `Jwt__ExpiryMinutes` | no | Default già sensati in `appsettings.json`. |
| `Cors__AllowedOrigins__0`, `__1`, ... | in produzione | Origini autorizzate a chiamare l'API (es. l'URL di GitHub Pages). In sviluppo il default è `http://localhost:4200`. |

Il frontend ha l'URL dell'API in `src/environments/environment.ts`
(sviluppo: `http://localhost:5000/api`) e `environment.prod.ts`
(produzione: **da impostare** con l'URL reale del backend prima del
deploy — il repository non inventa un URL).

## Avvio in locale

Backend (porta 5000):

```bash
cd backend/DreamApp.Api
dotnet run
```

Swagger disponibile su `http://localhost:5000/swagger` in sviluppo.

Frontend (porta 4200), in un altro terminale:

```bash
cd frontend/dreamapp-web
npm start
```

Apri `http://localhost:4200`.

### Test

```bash
# Backend
cd backend/DreamApp.Api.Tests
dotnet test

# Frontend
cd frontend/dreamapp-web
npm test -- --watch=false --browsers=ChromeHeadlessCI
```

## GitHub Pages

Il workflow [`.github/workflows/deploy-frontend.yml`](.github/workflows/deploy-frontend.yml)
compila il frontend e lo pubblica automaticamente ad ogni push su `main`
che tocchi `frontend/**`. Per attivarlo la prima volta:

1. Nel repository GitHub: **Settings → Pages → Source → GitHub Actions**.
2. Fai un push su `main` (o lancia il workflow manualmente da
   **Actions → Deploy Frontend to GitHub Pages → Run workflow**).
3. L'URL finale sarà del tipo:

   ```
   https://<TUO-USERNAME-GITHUB>.github.io/<NOME-REPOSITORY>/
   ```

   (per questo repository, a giudicare dal remote configurato, sarebbe
   `https://mic-work.github.io/DreamApp/` — verificalo comunque tu
   nelle impostazioni Pages del repository).

Il workflow imposta automaticamente il `--base-href` corretto usando il
nome del repository, copia `index.html` in `404.html` (necessario perché
GitHub Pages non supporta il rewrite lato server richiesto dal routing di
Angular) e aggiunge `.nojekyll`.

**Prima del deploy**, imposta l'URL reale del backend in
`frontend/dreamapp-web/src/environments/environment.prod.ts` e le origini
CORS consentite sul backend (`Cors__AllowedOrigins`), altrimenti il
frontend pubblicato non riuscirà a contattare le API.

## Hosting del backend

GitHub Pages ospita **solo** il frontend statico. Il backend ASP.NET Core
va eseguito su un hosting compatibile (qualunque servizio che supporti
.NET 8: es. un VPS con systemd, Azure App Service, Railway, Fly.io, un
container Docker su un host con storage persistente, ecc. — nessuno di
questi è prescritto: scegli quello che preferisci). Requisiti minimi
dell'hosting:

- Runtime **.NET 8** (o SDK, se fai build sul server).
- **HTTPS** (necessario perché una PWA su iPhone/Safari chiami un backend
  in produzione — i browser bloccano le richieste "miste" HTTP da una
  pagina HTTPS).
- **Filesystem persistente** per `DATABASE_PATH` e `IMAGES_PATH`: molti
  hosting "serverless"/container hanno un filesystem effimero che viene
  azzerato ad ogni deploy o riavvio. Verifica che l'hosting scelto offra
  un volume/disco persistente e puntaci quelle due variabili.

## Accesso da iPhone

1. Apri l'URL del frontend (GitHub Pages) in **Safari** su iPhone.
2. Tocca **Condividi** (l'icona con la freccia verso l'alto).
3. Scegli **Aggiungi alla schermata Home**.

DreamApp comparirà come un'icona sulla Home e si aprirà a schermo intero,
senza le barre di Safari.

## Documentazione tecnica

- [`docs/architecture.md`](docs/architecture.md) — architettura, scelte,
  sicurezza.
- [`docs/database.md`](docs/database.md) — schema, relazioni, migration.
- [`docs/api.md`](docs/api.md) — tutti gli endpoint REST.
