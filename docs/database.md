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
| Id            | Guid (PK) | generato dal client EF Core alla creazione |
| Username      | string   | **univoco** (indice unico)              |
| Email         | string   | **univoco** (indice unico)              |
| PasswordHash  | string   | hash PBKDF2, mai la password in chiaro  |
| CreatedAt     | datetime | UTC                                     |

### Wishlist

| Colonna   | Tipo     | Note                                          |
|-----------|----------|-------------------------------------------------|
| Id        | Guid (PK) |                                                  |
| OwnerId   | Guid (FK) | → User.Id, `OnDelete: Cascade`                  |
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
| Id          | Guid (PK)        |                                                     |
| WishlistId  | Guid (FK)        | → Wishlist.Id, `OnDelete: Cascade`                 |
| Name        | string           | obbligatorio                                       |
| Description | string?          | opzionale                                          |
| Notes       | string?          | opzionale                                          |
| ImageUrl    | string?          | **deprecato**: URL della singola foto pre multi-foto, vedi GiftPhoto |
| ProductUrl  | string?          | link al prodotto                                   |
| Priority    | string           | `Low` \| `Medium` \| `High` (enum salvato come stringa) |
| CreatedAt   | datetime         | UTC                                                |
| UpdatedAt   | datetime         | UTC, aggiornato ad ogni modifica                   |
| VisibleToAll | bool            | `true` (default) = visibile a tutti gli amici con accesso alla wishlist, anche quelli aggiunti in futuro; `false` = solo agli utenti in `GiftVisibility` |

### WishlistAccess

| Colonna     | Tipo     | Note                                                    |
|-------------|----------|------------------------------------------------------------|
| Id          | Guid (PK) |                                                              |
| UserId      | Guid (FK) | → User.Id: l'utente a cui viene concesso l'accesso in lettura |
| WishlistId  | Guid (FK) | → Wishlist.Id: la wishlist (di un ALTRO utente) resa visibile |
| CreatedAt   | datetime | UTC                                                         |

Indice **unico composito** su `(UserId, WishlistId)`: uno stesso utente non
può avere due volte accesso alla stessa wishlist (niente duplicati, come
richiesto). Eliminare una riga qui rimuove solo il permesso di lettura:
non tocca mai la wishlist o i regali del proprietario.

### GiftPhoto

| Colonna   | Tipo     | Note                                                   |
|-----------|----------|----------------------------------------------------------|
| Id        | Guid (PK) |                                                            |
| GiftId    | Guid (FK) | → Gift.Id, `OnDelete: Cascade`                            |
| Url       | string   | URL della foto (mai un binario), vedi architecture.md     |
| SortOrder | int      | ordine di visualizzazione nella galleria (0-based)         |
| CreatedAt | datetime | UTC                                                        |

Un regalo può avere più foto, ognuna nella propria riga: sostituiscono
`Gift.ImageUrl` (deprecato, mantenuto solo per i regali salvati prima di
questa funzionalità). In creazione/modifica il client invia la lista
completa di URL (già ottenuti da `POST /api/images`) e il server
sostituisce interamente le righe precedenti.

### GiftVisibility

| Colonna   | Tipo     | Note                                                        |
|-----------|----------|----------------------------------------------------------------|
| Id        | Guid (PK) |                                                                  |
| GiftId    | Guid (FK) | → Gift.Id, `OnDelete: Cascade`                                  |
| UserId    | Guid (FK) | → User.Id (un amico), `OnDelete: Restrict`                      |
| CreatedAt | datetime | UTC                                                             |

Indice **unico composito** su `(GiftId, UserId)`. Le righe contano solo
quando `Gift.VisibleToAll` è `false`: il regalo è allora visibile solo
agli utenti elencati (oltre al proprietario, che vede sempre tutto), e
senza nessuna riga **non lo vede nessuno tranne il proprietario**. Se
`Gift.VisibleToAll` è `true` non ci sono righe e il regalo è visibile a
chiunque abbia accesso alla wishlist. (La migrazione `AddGiftVisibleToAll`
imposta `VisibleToAll = false` solo per i regali che avevano già almeno
una riga, così i regali esistenti mantengono la visibilità di prima.) Gli id accettati in scrittura sono sempre filtrati lato
server sugli utenti che il PROPRIETARIO ha aggiunto come amico (cioè le cui
wishlist il proprietario può vedere) — **non** su chi ha semplicemente
aggiunto il proprietario. Questo permette di assegnare un regalo a una
persona specifica anche prima che quella persona abbia aggiunto a sua
volta il proprietario (in tal caso però non vede comunque nulla della sua
wishlist, finché non lo fa).

### GiftReception

| Colonna        | Tipo     | Note                                                       |
|----------------|----------|----------------------------------------------------------------|
| Id             | Guid (PK) |                                                                  |
| GiftId         | Guid (FK) | → Gift.Id, `OnDelete: Cascade`                                  |
| MarkedByUserId | Guid (FK) | → User.Id: l'amico che ha registrato questo stato, `OnDelete: Restrict` |
| GivenByUserId  | Guid (FK) | → User.Id: chi ha effettivamente regalato l'oggetto, `OnDelete: Restrict` |
| ReceivedAt     | datetime | UTC                                                             |

Indice **unico composito** su `(GiftId, MarkedByUserId)`: ogni persona ha
al più una riga per regalo. Due ruoli, non simmetrici:

- La riga del **proprietario** (`MarkedByUserId` = proprietario) è la
  conferma **definitiva e immediata**: "l'ho già ricevuto, da
  `GivenByUserId`". Basta da sola a far scattare lo stato "ricevuto" per
  `GivenByUserId` senza bisogno che quest'ultimo faccia nulla.
- La riga di un **amico** (`MarkedByUserId` = quell'amico) è sempre
  un'autocandidatura (`GivenByUserId` == `MarkedByUserId`): "credo di aver
  già regalato questo". È **privata** e resta **provvisoria** finché la
  riga del proprietario non nomina lo stesso amico — da sola non conferma
  mai nulla, indipendentemente da chi dei due agisce per primo. Il
  proprietario vede comunque l'elenco di chi si è autocandidato (mai il
  dettaglio delle righe altrui) come suggerimento per scegliere il vero
  donatore.

Segnare "non ricevuto"/ritirare la segnalazione rimuove semplicemente la
riga di chi chiama.

## Relazioni

```
User ──owns──> Wishlist ──contains──> Gift
User ──canView (via WishlistAccess)──> Wishlist
Gift ──has many──> GiftPhoto
Gift ──restricted to (via GiftVisibility)──> User
Gift ──received/given status (via GiftReception: owner's row = definitive, friend's row = pending self-claim)──> User (GivenByUserId)
```

## Migrazioni EF Core

Tutte le chiavi primarie ed esterne dello schema sono `Guid` (non più
`int` autoincrementali): EF Core le genera lato client alla `Add()`,
quindi ogni riga ha già il proprio Id prima ancora di `SaveChangesAsync`.
Questo ha richiesto di ricreare lo schema da zero: le vecchie migration
(basate su chiavi `int`) e il file `dreamapp.db` locale sono stati
eliminati e vanno rigenerati.

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
