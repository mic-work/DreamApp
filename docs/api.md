# API

Base URL: `{apiUrl}` (in sviluppo `http://localhost:5000/api`, in
produzione l'URL configurato in `environment.prod.ts`). Tutte le richieste
e risposte sono JSON. Gli endpoint protetti richiedono l'header:

```
Authorization: Bearer <token>
```

Il token si ottiene da `/api/auth/register` o `/api/auth/login`. In
sviluppo, la UI Swagger (`http://localhost:5000/swagger`) permette di
testare le API autenticate: basta incollare il token nel pulsante
"Authorize" (senza il prefisso `Bearer `).

Le risposte di errore hanno sempre la forma `{ "message": "..." }`.

## Autenticazione

### `POST /api/auth/register`

Body:

```json
{
  "username": "michele",
  "email": "michele@example.com",
  "password": "password123",
  "confirmPassword": "password123"
}
```

- `201 Created` → `{ token, expiresAt, user: { id, username, email, createdAt } }`.
  Crea anche automaticamente la wishlist personale dell'utente.
- `400 Bad Request` → dati non validi (vedi vincoli sui DTO).
- `409 Conflict` → username o email già in uso.

### `POST /api/auth/login`

Body: `{ "username": "...", "password": "..." }`

- `200 OK` → stesso payload di register.
- `401 Unauthorized` → credenziali non corrette (messaggio generico,
  identico sia per username inesistente sia per password errata).

### `GET /api/auth/me` 🔒

Ritorna il profilo completo dell'utente autenticato (`{ id, username,
email, createdAt }`).

## Utenti

### `GET /api/users/{username}` 🔒

Cerca un utente per username (usato da "Aggiungi amico").

- `200 OK` → `{ id, username, wishlistId }` (mai l'email di un altro
  utente).
- `400 Bad Request` → stai cercando il tuo stesso username: non puoi
  aggiungere te stesso come amico.
- `404 Not Found` → nessun utente con questo username.

### `PUT /api/users/me` 🔒

Aggiorna i dati personali (username, email) dell'utente autenticato. Il
server usa sempre l'id preso dal token JWT: nessun id passato dal client.

Body:

```json
{
  "username": "michele",
  "email": "michele@example.com"
}
```

- `200 OK` → `{ id, username, email, createdAt }` aggiornato.
- `400 Bad Request` → dati non validi.
- `409 Conflict` → username o email già in uso da un altro utente.

## Wishlist

### `GET /api/wishlists/me` 🔒

La wishlist dell'utente autenticato, con tutti i regali (`isOwner` è
sempre `true`).

### `GET /api/wishlists/{username}` 🔒

La wishlist di un altro utente (o la propria, se `username` coincide).

- `200 OK` → stesso formato di `/me`, con `isOwner=false` se non è la
  propria — in tal caso il frontend nasconde modifica/eliminazione. I
  regali sono ordinati per priorità decrescente, poi per data di
  creazione decrescente, e (se non sei il proprietario) contengono solo
  quelli visibili a te — vedi "Visibilità di un regalo" più sotto.
- `403 Forbidden` → l'utente esiste ma non hai (ancora) accesso alla sua
  wishlist.
- `404 Not Found` → nessun utente con questo username.

### `GET /api/wishlists/friends` 🔒

Elenco delle wishlist a cui hai accesso (`WishlistAccess`), in forma
sintetica: `[{ id, ownerUsername, name, giftsCount }]`. Usato dalla Home
per la sezione "Liste dei miei amici".

### `GET /api/wishlists/me/friends` 🔒

Gli amici che **tu** hai aggiunto (cioè le cui wishlist puoi vedere) —
stessa lista mostrata in Home in "Liste dei miei amici", ma nella forma
`[{ id, username }]`. Usato dal form "nuovo/modifica desiderio" per
popolare il selettore "Chi può vedere questo desiderio?". Deliberatamente
**non** include chi ha aggiunto te senza che tu li abbia aggiunti a tua
volta: solo gli amici che hai scelto tu possono essere designati come
destinatari di un regalo, anche prima che ti abbiano aggiunto a loro
volta.

### `GET /api/wishlists/{wishlistId}/viewers` 🔒

Come sopra ma per una wishlist qualsiasi che tu possa leggere (proprietario
o `WishlistAccess`) — non solo la tua. Usato dalla pagina di un regalo di
un amico per popolare "Chi l'ha regalato?" quando lo segni come ricevuto.

- `403 Forbidden` → non hai accesso a questa wishlist.
- `404 Not Found` → wishlist inesistente.

### `POST /api/wishlists/{wishlistId}/access` 🔒

Concede a **te stesso** l'accesso in lettura alla wishlist `{wishlistId}`
(che deve appartenere a un altro utente). Usato dopo aver trovato un
amico con `GET /api/users/{username}`.

- `204 No Content` → accesso concesso.
- `400 Bad Request` → stai provando ad aggiungere la tua stessa wishlist.
- `404 Not Found` → wishlist inesistente.
- `409 Conflict` → hai già accesso a questa wishlist.

### `DELETE /api/wishlists/{wishlistId}/access` 🔒

Rimuove il TUO accesso a quella wishlist ("rimuovi amico"). Non tocca mai
la wishlist o i regali del proprietario.

- `204 No Content` → accesso rimosso.
- `404 Not Found` → non avevi accesso a questa wishlist.

## Regali

Un regalo (`GiftDto`) ha questa forma quando torna dall'API:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "wishlistId": "8f14e45f-ceea-467e-9a97-4b5e6f4d1a2b",
  "name": "AirPods",
  "description": "Colore nero",
  "notes": null,
  "imageUrl": null,
  "photos": ["/uploads/abc.jpg", "/uploads/def.jpg"],
  "productUrl": "https://...",
  "priority": "High",
  "createdAt": "...",
  "updatedAt": "...",
  "isOwner": true,
  "visibleToAll": true,
  "visibleToUserIds": [],
  "receivedByMe": false,
  "receivedFromUserId": null,
  "receivedFromUsername": null,
  "receivedAt": null,
  "myClaimPending": false,
  "claimedByUserIds": []
}
```

- `imageUrl` è deprecato (esiste solo per i regali salvati prima del
  supporto multi-foto): usare sempre `photos`.
- `visibleToAll` e `visibleToUserIds` sono valorizzati **solo per il
  proprietario** (vedi sotto); per chiunque altro sono sempre `false` e
  `[]`.
- `receivedByMe` / `receivedFromUserId` / `receivedFromUsername` /
  `receivedAt` / `myClaimPending` / `claimedByUserIds` — vedi "Stato
  ricevuto" più sotto. `receivedAt` è la data della conferma definitiva
  (valorizzato solo quando `receivedByMe` è `true`) e serve a ordinare le
  liste "già regalati"/"ricevuti" per data di conferma invece che per
  priorità.

### Visibilità di un regalo

Ogni regalo ha tre possibili livelli di visibilità, impostati in
creazione/modifica:

- `visibleToAll: true` (default se il campo è omesso): visibile a tutti gli
  amici con accesso alla wishlist, **compresi quelli aggiunti in futuro**;
  `visibleToUserIds` viene ignorato.
- `visibleToAll: false` con `visibleToUserIds` non vuoto: visibile solo agli
  amici elencati.
- `visibleToAll: false` con `visibleToUserIds` vuoto: **non lo vede nessuno
  tranne il proprietario**, che lo trova solo nella propria lista.

Il proprietario vede sempre tutto. Gli id accettati sono sempre
amici che il PROPRIETARIO ha aggiunto (vedi `GET /api/wishlists/me/friends`
sopra) — **non** chi ha semplicemente aggiunto il proprietario: questo
permette di assegnare un regalo a una persona specifica ("questo è per
Michele") anche se quella persona non ti ha ancora aggiunto a sua volta.
Finché non lo fa, però, non ha comunque nessun accesso alla tua wishlist
(vedi sopra), quindi non vedrà comunque questo o altri tuoi regali. Un
amico escluso da un regalo non lo vede né in `GET
/api/wishlists/{username}` né in `GET /api/gifts`/`GET /api/gifts/{id}`
(quest'ultimo risponde `403 Forbidden`).

### Stato ricevuto (conferma a due, non un'autocertificazione)

Il proprietario e i suoi amici possono segnare che un regalo è già stato
dato, e da chi (`PUT /api/gifts/{id}/reception`), ma i due ruoli non sono
simmetrici:

- Il **proprietario** segna, sul proprio regalo, "l'ho già ricevuto, da
  X" scegliendo X tra chi ha accesso alla propria wishlist (stesso elenco
  di `GET /api/wishlists/{wishlistId}/viewers`). Questo è **definitivo e
  immediato**: appena il proprietario lo conferma, X vede automaticamente
  quel regalo come "già regalato" (`receivedByMe=true`), senza dover fare
  nulla — vedi `receivedFromUserId`/`receivedFromUsername` sul regalo del
  proprietario.
- Un **amico** può solo segnalare di aver regalato lui stesso quel
  regalo — un solo click, senza scegliere nessuno (`givenByUserId` non
  serve e viene ignorato se inviato: per un amico è sempre se stesso).
  Questa segnalazione resta **privata e provvisoria**
  (`myClaimPending=true`): non basta da sola a far scattare
  `receivedByMe=true` per l'amico, qualunque cosa dica — serve sempre la
  conferma definitiva del proprietario, indipendentemente da chi dei due
  agisce per primo. Finché non arriva, l'amico può ritirare la propria
  segnalazione (`received: false`).

Il proprietario vede, per ogni proprio regalo, `claimedByUserIds`: gli id
degli amici che hanno segnalato (in modo provvisorio) di averlo già
regalato — un suggerimento per "chi l'ha regalato?", mai una conferma da
solo. Nessun altro vede questo elenco, né le segnalazioni provvisorie
altrui.

### `GET /api/gifts?wishlistId={id}` 🔒

Elenca i regali di una wishlist (parametro opzionale: se omesso, usa la
tua wishlist), ordinati per priorità decrescente poi data di creazione
decrescente. Stessa regola di accesso in lettura di `GET
/api/wishlists/{username}` (proprietario o `WishlistAccess` esistente),
altrimenti `403 Forbidden`. Se non sei il proprietario, i regali con
visibilità ristretta a cui non appartieni vengono omessi dall'elenco.

### `GET /api/gifts/{id}` 🔒

Un singolo regalo, con lo stesso controllo di accesso in lettura (incluso
il controllo di visibilità del singolo regalo).

### `POST /api/gifts` 🔒

Crea un regalo **nella propria wishlist** (non è possibile specificare la
wishlist di qualcun altro). Body:

```json
{
  "name": "AirPods",
  "description": "Colore nero",
  "notes": null,
  "imageUrls": ["/uploads/abc.jpg", "/uploads/def.jpg"],
  "productUrl": "https://...",
  "priority": "High",
  "visibleToAll": true,
  "visibleToUserIds": []
}
```

`priority` accetta `"Low" | "Medium" | "High"`. `imageUrls` sono URL già
ottenuti da `POST /api/images`, nell'ordine in cui vanno mostrati.
`visibleToUserIds` conta solo se `visibleToAll` è `false` e accetta solo id di persone che **tu** hai aggiunto come
amico (`GET /api/wishlists/me/friends`) — non chi ha aggiunto te: id non
validi vengono ignorati in silenzio, mai fidandosi ciecamente del client.
Risposta `201 Created` con il regalo creato.

### `PUT /api/gifts/{id}` 🔒

Aggiorna un regalo (stesso body di `POST`). Il server ricarica il regalo
dal database usando **solo** l'`{id}` nella route, poi verifica che la sua
wishlist appartenga all'utente autenticato — cambiare manualmente l'id
nella richiesta HTTP non permette di modificare un regalo altrui.
`imageUrls`, `visibleToAll` e `visibleToUserIds` sostituiscono
**interamente** la galleria foto e la visibilità precedenti.

- `200 OK` → regalo aggiornato.
- `403 Forbidden` → il regalo esiste ma non è tuo.
- `404 Not Found` → regalo inesistente.

### `DELETE /api/gifts/{id}` 🔒

Come sopra, ma elimina. `204 No Content` / `403 Forbidden` / `404 Not Found`.

### `PUT /api/gifts/{id}/reception` 🔒

Segna (o annulla/ritira) che questo regalo è già stato dato/ricevuto —
vedi "Stato ricevuto" sopra. Il comportamento dipende da chi chiama:

- **Proprietario**: body `{ "received": true, "givenByUserId": "8f14e45f-ceea-467e-9a97-4b5e6f4d1a2b" }`.
  `givenByUserId` è obbligatorio e deve essere qualcuno con accesso alla
  wishlist. Conferma definitiva e immediata.
- **Amico**: body `{ "received": true }` — `givenByUserId` non va
  indicato: il server lo ignora comunque e usa sempre l'id di chi chiama.
  Resta una segnalazione provvisoria finché il proprietario non conferma
  lo stesso amico come donatore.

Body per annullare/ritirare in entrambi i casi:

```json
{ "received": false }
```

Risposta `200 OK` con il regalo aggiornato (dal punto di vista di chi ha
chiamato).

- `400 Bad Request` → sei il proprietario, `received: true`, e non hai
  indicato un `givenByUserId` valido.
- `403 Forbidden` → non hai accesso alla wishlist, o (se non sei il
  proprietario) il regalo ha una visibilità ristretta che ti esclude.
- `404 Not Found` → regalo inesistente.

## Foto

### `POST /api/images` 🔒

`multipart/form-data` con un campo `file` (jpg/png/gif/webp, max 5&nbsp;MB).

- `200 OK` → `{ "imageUrl": "/uploads/<nome-generato>" }`. Il frontend
  chiama questo endpoint una volta per ogni foto selezionata e raccoglie
  gli URL ottenuti in `imageUrls` quando crea/aggiorna un regalo.
- `400 Bad Request` → file mancante, troppo grande o formato non
  supportato.

## Codici di stato usati

`200` letture/aggiornamenti riusciti · `201` creazione riuscita · `204`
operazione riuscita senza contenuto (delete, grant/revoke access) · `400`
richiesta non valida · `401` non autenticato / credenziali errate · `403`
autenticato ma non autorizzato su questa risorsa · `404` risorsa
inesistente · `409` conflitto (duplicati) · `500` errore imprevisto del
server.
