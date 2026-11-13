# Pubblicazione di DreamApp (gratis)

Architettura: il **frontend** (Angular) sta su **GitHub Pages**, il **backend + database SQLite + foto** su una VM **Oracle Cloud Always Free**, raggiunta tramite un nome gratuito **DuckDNS** con HTTPS automatico (**Caddy**).

```
Utente -> https://<utente>.github.io/DreamApp/   (frontend, GitHub Pages)
              |
              v  chiamate API
          https://<nome>.duckdns.org/api          (Caddy -> backend su 127.0.0.1:5000)
                                                   dati in /var/lib/dreamapp (DB + foto)
```

## Cose da sapere prima
- Nessun servizio gratuito offre backup: la protezione dei dati e' `backup.ps1` (vedi sotto).
- Oracle puo' "reclamare" le VM Always Free inattive (7 giorni con CPU/rete/memoria sotto il 20%). Mitigazioni: backup regolari e, opzionalmente, passaggio a Pay As You Go con avviso di spesa a 1 EUR.
- La "home region" scelta in registrazione non si puo' cambiare.

## 1. Account Oracle Cloud
1. https://www.oracle.com/cloud/free/ -> crea l'account (carta vera, non prepagata/virtuale; blocco temporaneo di verifica).
2. Scegli come home region la piu' vicina (Milano o Francoforte).

## 2. Creare la VM
Compute -> Instances -> Create instance:
- Image: Canonical Ubuntu 22.04
- Shape: Ampere `VM.Standard.A1.Flex`, 1 OCPU, 6 GB RAM (alternativa se "out of host capacity": `VM.Standard.E2.1.Micro` e poi `.\buildscript.ps1 -Runtime linux-x64`)
- IP pubblico assegnato; "Generate a key pair" e **scarica la chiave privata** (non si puo' riscaricare)
- Annota l'IP pubblico.

## 3. Aprire le porte 80 e 443
1. Console Oracle: VCN -> Subnet -> Security List -> Add Ingress Rules: `0.0.0.0/0`, TCP, porta 80 e poi 443.
2. Sulla VM (dopo il collegamento SSH):
   ```bash
   sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
   sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
   sudo netfilter-persistent save
   ```

## 4. Nome DuckDNS
https://www.duckdns.org -> accesso con GitHub -> crea un sottodominio -> nel campo "current ip" metti l'IP della VM.
Poi imposta `apiUrl` in `frontend/dreamapp-web/src/environments/environment.prod.ts`:
`https://<nome>.duckdns.org/api`

## 5. Preparare la VM
```powershell
ssh -i C:\percorso\chiave.key ubuntu@IP_DELLA_VM
```
```bash
sudo apt update && sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https curl sqlite3
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt update && sudo apt install -y caddy
```

## 6. Pubblicare il backend
Dal PC, in `backend\DreamApp.Api`:
```powershell
.\buildscript.ps1
```
Poi segui `bin\publish-release\deploy\LEGGIMI.txt` (caricamento file, chiave segreta `Jwt__Key`, servizio systemd, Caddy).
Prova: `https://<nome>.duckdns.org/api/auth/me` deve rispondere 401.

## 7. Pubblicare il frontend
1. Il repository GitHub deve essere **pubblico** (Pages gratis).
2. Settings -> Pages -> Source: **GitHub Actions**.
3. Commit e push (incluso `environment.prod.ts`): il workflow `deploy-frontend.yml` esegue test, build e pubblicazione.

Link da condividere: `https://<utente>.github.io/DreamApp/`

## 8. Aggiornamenti
- Frontend: push su `main`.
- Backend: `.\backup.ps1 ...` (per sicurezza), `.\buildscript.ps1`, poi i comandi della sezione "AGGIORNAMENTI SUCCESSIVI" di `LEGGIMI.txt`. Database e foto in `/var/lib/dreamapp` non vengono toccati.

## 9. Backup e ripristino
Backup (dal PC, in `backend\DreamApp.Api`):
```powershell
.\backup.ps1 -Server IP_DELLA_VM -Key C:\percorso\chiave.key
```
Scarica una copia verificata del database e le foto in `DreamAppBackups` (cartella utente, fuori dal repository), tenendo gli ultimi 10 backup.

Ripristino su una VM nuova (dopo aver pubblicato il backend):
```powershell
scp -i CHIAVE.key dreamapp.db uploads.tgz ubuntu@IP_DELLA_VM:~/
```
```bash
sudo systemctl stop dreamapp
sudo cp ~/dreamapp.db /var/lib/dreamapp/dreamapp.db
sudo rm -f /var/lib/dreamapp/dreamapp.db-wal /var/lib/dreamapp/dreamapp.db-shm
sudo tar xzf ~/uploads.tgz -C /var/lib/dreamapp
sudo chown -R www-data:www-data /var/lib/dreamapp
sudo systemctl start dreamapp
```
