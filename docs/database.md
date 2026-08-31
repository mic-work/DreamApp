# Database

## Motore

SQLite, un unico file (`dreamapp.db`), acceduto esclusivamente dal backend
tramite Entity Framework Core 8 (provider `Microsoft.EntityFrameworkCore.
Sqlite`). Vedi anche [`database/README.md`](../database/README.md) per
percorso e backup.

## Schema (da `backend/DreamApp.Api/Models` e `Data/AppDbContext.cs`)

### User

| Colonna       | Tipo     | Note                                   |
|---------------|----------|-----------------------------------------|
| Id            | int (PK) | autoincrementale                        |
| Username      | string   | **univoco** (indice unico)              |
| Email         | string   | **univoco** (indice unico)              |
| PasswordHash  | string   | hash PBKDF2, mai la password in chiaro  |
| CreatedAt     | datetime | UTC                                     |

### Wishlist

| Colonna   | Tipo     | Note                                          |
|-----------|----------|-------------------------------------------------|
| Id        | int (PK) |                                                  |
| OwnerId   | int (FK) | → User.Id, `OnDelete: Cascade`                  |
| Name      | string   |                                                  |
| CreatedAt | datetime | UTC                                             |

Nella prima versione ogni utente ha **esattamente una** wishlist
personale, creata automaticamente da `AuthController.Register` al momento
della registrazione (nome di default: "La mia lista"). Il modello non
impone questo limite a livello di schema: sarebbe possibile supportare
più wishlist per utente in futuro senza una migrazione distruttiva.

### Gift

| Colonna     | Tipo             | Note                                             |
|-------------|------------------|---------------------------------------------------|
| Id          | int (PK)         |                                                     |
| WishlistId  | int (FK)         | → Wishlist.Id, `OnDelete: Cascade`                 |
| Name        | string           | obbligatorio                                       |
| Description | string?          | opzionale                                          |
| Notes       | string?          | opzionale                                          |
| ImageUrl    | string?          | URL della foto (mai un binario), vedi architecture.md |
| ProductUrl  | string?          | link al prodotto                                   |
| Priority    | string           | `Low` \| `Medium` \| `High` (enum salvato come stringa) |
| CreatedAt   | datetime         | UTC                                                |
| UpdatedAt   | datetime         | UTC, aggiornato ad ogni modifica                   |

### WishlistAccess

| Colonna     | Tipo     | Note                                                    |
|-------------|----------|------------------------------------------------------------|
| Id          | int (PK) |                                                              |
| UserId      | int (FK) | → User.Id: l'utente a cui viene concesso l'accesso in lettura |
| WishlistId  | int (FK) | → Wishlist.Id: la wishlist (di un ALTRO utente) resa visibile |
| CreatedAt   | datetime | UTC                                                         |

Indice **unico composito** su `(UserId, WishlistId)`: uno stesso utente non
può avere due volte accesso alla stessa wishlist (niente duplicati, come
richiesto). Eliminare una riga qui rimuove solo il permesso di lettura:
non tocca mai la wishlist o i regali del proprietario.

## Relazioni

```
User ──owns──> Wishlist ──contains──> Gift
User ──canView (via WishlistAccess)──> Wishlist
```

## Migrazioni EF Core

Il repository include la configurazione EF Core (DbContext, Fluent API in
`OnModelCreating`, pacchetto `Microsoft.EntityFrameworkCore.Design`) ma
**non include ancora una migration generata**: l'ambiente di sviluppo
usato per preparare questo repository non aveva accesso alla rete NuGet
per eseguire gli strumenti EF Core (vedi limitazioni nel README). Vanno
generate una tantum, in locale:

```bash
cd backend/DreamApp.Api
dotnet tool install --global dotnet-ef   # una tantum, se non già installato
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Il primo comando genera i file in `Migrations/`; il secondo crea/aggiorna
`dreamapp.db` nel percorso configurato. Da quel momento in poi
`Program.cs` applica automaticamente le migration pendenti ad ogni avvio
(`db.Database.Migrate()`), quindi in produzione non serve eseguire
manualmente `dotnet ef database update` dopo ogni deploy — basta che le
migration compilate siano incluse nel pacchetto pubblicato.

Per aggiungere una migration successiva dopo aver modificato i modelli:

```bash
dotnet ef migrations add NomeDescrittivo
```

## Configurazione del percorso file

Vedi `database/README.md`: percorso di default
`backend/DreamApp.Api/App_Data/dreamapp.db`, sovrascrivibile con la variabile
d'ambiente `DATABASE_PATH`.
