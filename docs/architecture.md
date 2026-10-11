# Architettura

## Panoramica

```
        INTERNET
            │
   ┌────────┴────────┐
   │                 │
iPhone 1          iPhone 2
   │                 │
   └────────┬────────┘
            │ HTTPS (Safari, PWA installata su Home)
            ▼
   ┌──────────────────┐
   │  Angular / PWA    │   frontend/dreamapp-web
   │  (statico)        │   pubblicato su GitHub Pages
   └────────┬──────────┘
            │ HTTPS, REST, JSON, JWT nell'header Authorization
            ▼
   ┌──────────────────┐
   │  ASP.NET Core API │   backend/DreamApp.Api
   │  (Controllers)    │   ospitato su un hosting compatibile ASP.NET Core
   └────────┬──────────┘
            │ Entity Framework Core (LINQ)
            ▼
   ┌──────────────────┐
   │  SQLite            │   file singolo, path configurabile
   │  dreamapp.db       │   (vedi docs/database.md)
   └──────────────────┘
```

Il frontend non parla mai direttamente con il database: passa sempre dalle
API REST del backend, che sono l'unico componente autorizzato a leggere/
scrivere `dreamapp.db` tramite EF Core.

## Perché questo stack

Il progetto è un'app personale per due utenti su iPhone. Si è
deliberatamente scelta un'architettura a tre livelli semplice (niente
microservizi, cache distribuite, message broker) perché:

- il carico è minimo (2 utenti, poche richieste al giorno);
- SQLite è più che sufficiente e azzera i costi/la complessità di un
  database server separato;
- una Web App + PWA evita la pubblicazione su App Store, i costi Apple
  Developer e la necessità di Xcode/Swift, mantenendo comunque
  un'esperienza "da app" su iPhone (icona in Home, schermo intero).

## Frontend — Angular 19 (standalone) + PWA

- **Componenti standalone**, nessun `NgModule`: ogni pagina è caricata in
  lazy loading tramite `loadComponent` (vedi `src/app/app.routes.ts`).
- **Struttura** (`src/app/`):
  - `core/services` — `AuthService`, `UserService`, `WishlistService`,
    `GiftService`, `ImageService`: unico punto di contatto con l'API.
  - `core/guards` — `authGuard` (blocca le pagine protette se non
    autenticati) e `guestGuard` (evita che un utente già loggato riveda
    login/registrazione).
  - `core/interceptors` — `authInterceptor` (aggiunge il JWT alle
    richieste verso l'API), `errorInterceptor` (su 401 forza il logout e
    il redirect al login).
  - `models` — interfacce TypeScript che rispecchiano i DTO del backend.
  - `pages` — una cartella per schermata (login, register, home,
    my-wishlist, wishlist-detail, add-gift, add-friend, profile).
  - `shared/components` — componenti riusabili (`gift-card`,
    `priority-badge`, `confirm-dialog`).
- **Stato di sessione**: `AuthService` mantiene l'utente corrente con un
  Angular `signal` e salva token + utente in `localStorage`, così la PWA
  resta loggata tra un lancio e l'altro dalla Home.
- **PWA**: `@angular/service-worker` (config in `ngsw-config.json`),
  `public/manifest.webmanifest`, icone in `public/icons/`, meta tag
  `apple-mobile-web-app-*` in `src/index.html` per il comportamento
  corretto su Safari/iOS quando l'app viene aggiunta alla schermata Home.
- **Configurazione ambiente**: `src/environments/environment.ts`
  (sviluppo, `apiUrl` verso `http://localhost:5000/api`) e
  `environment.prod.ts` (produzione: l'URL reale del backend va impostato
  qui prima del deploy, vedi README).

## Backend — ASP.NET Core 8 Web API

- **Program.cs**: configura EF Core + SQLite, autenticazione JWT Bearer,
  CORS, Swagger (con supporto al Bearer token per testare le API
  autenticate dalla UI di Swagger), file statici per le foto caricate
  (`/uploads`), e applica automaticamente le migration EF Core all'avvio
  (`db.Database.Migrate()`).
- **Controllers** (`Controllers/`): `AuthController`, `UsersController`,
  `WishlistsController`, `GiftsController`, `ImagesController`. Ogni
  endpoint protetto usa `[Authorize]`; l'id dell'utente autenticato viene
  letto dal claim JWT (`ClaimsPrincipalExtensions.GetUserId()`), mai da un
  parametro passato dal client.
- **Data/AppDbContext**: il `DbContext` EF Core. I controller dipendono
  dall'interfaccia più stretta `IAppDbContext` (stessa forma, ma
  esponendo `IQueryable<T>` invece di `DbSet<T>`) proprio per poter
  scrivere test unitari veloci con un finto contesto in memoria
  (`DreamApp.Api.Tests/Fakes/FakeAppDbContext.cs`) senza un vero database.
- **Services**:
  - `PasswordHasher` — hashing PBKDF2-HMAC-SHA256 (`System.Security.
    Cryptography`, nessuna libreria esterna necessaria).
  - `JwtTokenService` — emette il JWT firmato (HMAC-SHA256) dopo login/
    registrazione.
  - `IImageStorageService` / `LocalImageStorageService` — astrazione per
    il salvataggio delle foto dei regali (vedi sotto).
- **Autorizzazione sulle risorse**: ogni endpoint che modifica un `Gift`
  o una `WishlistAccess` ricarica la risorsa dal database usando SOLO
  l'id nella route, poi verifica che il proprietario coincida con
  l'utente autenticato (claim JWT). Un id "indovinato" o modificato a
  mano nella richiesta HTTP non permette mai di accedere a risorse altrui
  (vedi `GiftsController.Update/Delete`, `WishlistsController`).

## Foto dei regali

Il campo `Gift.ImageUrl` contiene solo una URL, mai un binario. La prima
implementazione (`LocalImageStorageService`) salva il file su disco locale
(percorso configurabile, stesso principio dello storage persistente del
database) e lo serve via `/uploads/<file>`. Per passare a uno storage
cloud (S3, Azure Blob, Cloudinary, ...) basta scrivere una nuova classe
che implementa `IImageStorageService` e registrarla in `Program.cs`: nessun
altro file cambia.

## Sicurezza

- Password mai in chiaro: solo hash PBKDF2 con salt casuale per utente.
- JWT firmato con chiave simmetrica **non committata**: va configurata
  con `dotnet user-secrets` in sviluppo o con la variabile d'ambiente
  `Jwt__Key` in produzione (vedi README).
- Limite ai tentativi su `POST /api/auth/login` e `/register`: per IP, al
  minuto (`RateLimiting:AuthPermitLimit`, default 20); oltre risponde `429`
  con `{ "message": ... }`.
- CORS ristretto alle origini configurate (`Cors:AllowedOrigins`), con
  fallback a `http://localhost:4200` solo in sviluppo.
- Validazione sia lato frontend (Reactive Forms + Validators) sia lato
  backend (Data Annotations sui DTO + controlli espliciti nei
  controller, es. username/email duplicati).
- Controllo ownership esplicito su ogni operazione di scrittura (vedi
  sopra).

## Cosa NON c'è (di proposito)

Come richiesto per un'app personale a basso traffico: nessun
microservizio, nessuna cache distribuita (Redis), nessun message broker,
nessuna orchestrazione container. Un singolo processo ASP.NET Core e un
singolo file SQLite sono sufficienti e facili da mantenere/deployare.
