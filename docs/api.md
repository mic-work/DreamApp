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
  propria — in tal caso il frontend nasconde modifica/eliminazione.
- `403 Forbidden` → l'utente esiste ma non hai (ancora) accesso alla sua
  wishlist.
- `404 Not Found` → nessun utente con questo username.

### `GET /api/wishlists/friends` 🔒

Elenco delle wishlist a cui hai accesso (`WishlistAccess`), in forma
sintetica: `[{ id, ownerUsername, name, giftsCount }]`. Usato dalla Home
per la sezione "Liste dei miei amici".

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

### `GET /api/gifts?wishlistId={id}` 🔒

Elenca i regali di una wishlist (parametro opzionale: se omesso, usa la
tua wishlist). Stessa regola di accesso in lettura di
`GET /api/wishlists/{username}` (proprietario o `WishlistAccess`
esistente), altrimenti `403 Forbidden`.

### `GET /api/gifts/{id}` 🔒

Un singolo regalo, con lo stesso controllo di accesso in lettura.

### `POST /api/gifts` 🔒

Crea un regalo **nella propria wishlist** (non è possibile specificare la
wishlist di qualcun altro). Body:

```json
{
  "name": "AirPods",
  "description": "Colore nero",
  "notes": null,
  "imageUrl": null,
  "productUrl": "https://...",
  "priority": "High"
}
```

`priority` accetta `"Low" | "Medium" | "High"`. Risposta `201 Created` con
il regalo creato.

### `PUT /api/gifts/{id}` 🔒

Aggiorna un regalo. Il server ricarica il regalo dal database usando
**solo** l'`{id}` nella route, poi verifica che la sua wishlist appartenga
all'utente autenticato — cambiare manualmente l'id nella richiesta HTTP
non permette di modificare un regalo altrui.

- `200 OK` → regalo aggiornato.
- `403 Forbidden` → il regalo esiste ma non è tuo.
- `404 Not Found` → regalo inesistente.

### `DELETE /api/gifts/{id}` 🔒

Come sopra, ma elimina. `204 No Content` / `403 Forbidden` / `404 Not Found`.

## Foto

### `POST /api/images` 🔒

`multipart/form-data` con un campo `file` (jpg/png/gif/webp, max 5&nbsp;MB).

- `200 OK` → `{ "imageUrl": "/uploads/<nome-generato>" }`. Il frontend
  passa questo valore come `imageUrl` quando crea/aggiorna un regalo.
- `400 Bad Request` → file mancante, troppo grande o formato non
  supportato.

## Codici di stato usati

`200` letture/aggiornamenti riusciti · `201` creazione riuscita · `204`
operazione riuscita senza contenuto (delete, grant/revoke access) · `400`
richiesta non valida · `401` non autenticato / credenziali errate · `403`
autenticato ma non autorizzato su questa risorsa · `404` risorsa
inesistente · `409` conflitto (duplicati) · `500` errore imprevisto del
server.
