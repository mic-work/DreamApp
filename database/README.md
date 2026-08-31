# Database

DreamApp usa **SQLite**: un unico file (`dreamapp.db`) condiviso da tutti gli
utenti, letto e scritto esclusivamente dal backend ASP.NET Core tramite
Entity Framework Core. Nessun client (Angular) accede mai al file
direttamente.

Questa cartella non contiene il file `.db` (è escluso da Git, vedi
`.gitignore` nella root del repository): contiene solo questa
documentazione. Il file reale viene creato automaticamente dal backend al
primo avvio, nel percorso configurato (vedi sotto).

Per lo schema delle tabelle e le migrazioni EF Core, vedi
[`docs/database.md`](../docs/database.md).

## Dove si trova il file in sviluppo

Di default: `backend/DreamApp.Api/App_Data/dreamapp.db` (creato automaticamente,
percorso relativo alla cartella del progetto backend).

## Dove deve trovarsi in produzione

Il percorso è **configurabile** tramite la variabile d'ambiente
`DATABASE_PATH` (vedi `Program.cs`), ad esempio:

```
DATABASE_PATH=/persistent-data/dreamapp.db
```

**Importante:** molti servizi di hosting hanno un filesystem *effimero*
(il contenuto viene perso ad ogni riavvio/deploy). SQLite richiede uno
storage persistente: `DATABASE_PATH` deve puntare a un volume/disco
persistente fornito dall'hosting, altrimenti i dati verranno perduti.

## Backup

Essendo un singolo file, il backup è semplice:

1. Fermare (o mettere in manutenzione) il backend, oppure accettare una
   copia "a caldo" (SQLite gestisce comunque bene le scritture concorrenti
   grazie al WAL mode; per un'app personale con 2 utenti il rischio di
   inconsistenza è trascurabile).
2. Copiare il file `dreamapp.db` (ed eventuali `dreamapp.db-wal` /
   `dreamapp.db-shm` se presenti) in un'altra posizione — un altro disco,
   uno storage cloud (es. un bucket), o semplicemente scaricarlo via SFTP/
   pannello di controllo dell'hosting.
3. Per ripristinare: fermare il backend, sostituire il file, riavviare.

Non è richiesta nessuna infrastruttura di backup complessa per la prima
versione: è sufficiente uno script/cron che copi periodicamente il file su
un'altra posizione.
