# Changelog

Tutte le modifiche rilevanti di PasswordGen sono documentate qui.
Formato: [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) — versioni: [Semantic Versioning](https://semver.org/lang/it/).
Il numero di versione si imposta in un solo punto: `<Version>` in `Directory.Build.props`.

## [1.4.0] - 2026-10-08

### Aggiunto
- **App Android** (prima tappa, .NET MAUI, cartella `src/PasswordGen.Mobile`): le tre tipologie di password (parole italiane,
  sillabe, caratteri casuali), policy configurabile, numero di proposte regolabile, password attuale facoltativa e
  copia negli appunti segnalata come sensibile e cancellata dopo 30 secondi. L'APK è allegata alla release
  (Android 7.0 o successivo, nessun permesso). Storico, promemoria con notifiche e file di parole arriveranno nelle tappe successive.
- Il workflow di release compila e allega anche l'APK (firma con la chiave fissa se i secret `ANDROID_KEYSTORE_*` sono configurati).

### Modificato
- `PasswordGen.Core` ora è compilato sia per `net48` (app Windows e test) sia per `netstandard2.0` (app Android).
  `DpapiProtector` resta disponibile solo su Windows.

## [1.3.0] - 2026-10-08

### Aggiunto
- **File di parole personale** per le passphrase (card «Parole della passphrase»): una parola per riga, 4-9 lettere,
  `#` per i commenti. Le parole vengono portate in minuscolo, senza accenti e senza duplicati; l'app mostra quante ne ha
  accettate e perché ha scartato le altre.
- Tre modalità: solo la lista integrata, **aggiungi il mio file alla lista integrata** (basta una parola valida)
  oppure **solo il mio file** (almeno 300 parole valide).
- Riepilogo della lista in uso con il valore in bit di ogni parola e avviso quando la lista ha meno di 1000 parole.
- Con il file mancante, illeggibile o troppo corto l'app ripiega sulla lista integrata e lo segnala.
- Il percorso del file e la modalità scelta vengono ricordati tra un avvio e l'altro.

## [1.2.0] - 2026-10-08

### Aggiunto
- **Storico delle password** (facoltativo): premendo «Ho cambiato la password» l'app chiede quale proposta è stata usata
  (preselezionata l'ultima copiata) e la registra con numero progressivo, data e tipo. Ultime 12 voci.
- Scheda *Storico* con password mascherate: mostra/nascondi, copia (con cancellazione automatica degli appunti) ed elimina;
  pulsante per cancellare tutto lo storico.
- Le nuove proposte evitano le varianti di tutte le password dello storico, non solo dell'ultima.
- Il pulsante «Ho cambiato la password» è sempre visibile, anche con il promemoria disattivato.

### Modificato
- Le password dello storico sono salvate in `%AppData%\PasswordGen\history.dat`, cifrato con DPAPI (utente corrente).
  Disattivando lo storico il file viene cancellato (con conferma).

## [1.1.0] - 2026-10-08

### Aggiunto
- Numero di proposte regolabile dall'interfaccia (da 1 a 20, predefinito 6), salvato nelle preferenze.

## [1.0.0] - 2026-10-08

### Aggiunto
- Prima versione: generatore di password casuali, sicure e facili da ricordare per la rotazione mensile imposta dalle policy aziendali.
- Tre tipi di password: **parole italiane** (passphrase di 3-8 parole da una lista di oltre mille parole, con cifre e simbolo finali),
  **sillabe pronunciabili** e **caratteri casuali**.
- Policy configurabile (predefinita: almeno 10 caratteri con maiuscole, minuscole, numeri e caratteri speciali), con opzione
  per evitare i caratteri ambigui (0 O 1 l I) e limite ai caratteri uguali consecutivi.
- Sei proposte per volta, con entropia in bit e giudizio (Debole / Accettabile / Buona / Ottima).
- Password attuale facoltativa (solo in memoria): le nuove proposte sono sensibilmente diverse dalla precedente.
- Copia negli appunti con esclusione dalla cronologia di Windows e cancellazione automatica dopo 30 secondi o alla chiusura.
- Promemoria del cambio password: banner con i giorni alla scadenza, pulsante «Ho cambiato la password» e, a scelta,
  avvio con Windows che apre la finestra solo quando la password sta per scadere.
- Preferenze salvate in `%AppData%\PasswordGen\settings.json` (mai le password).
