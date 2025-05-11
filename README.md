# 🖼️ ImageFetcherService

Microservizio .NET per la ricerca di immagini relative a parole chiave da tre provider: **Pexels**, **Pixabay** e **Unsplash**.

## 🔍 Funzionalità

- Ricerca immagini tramite `/imagefetcher/search?query=...`
- Combina risultati da più provider
- Gestione del rate limit Unsplash
- Autenticazione tramite JWT
- Swagger attivo in ambiente `Development`

---

## ⚙️ Tecnologie

- ASP.NET Core 8.0
- JWT Bearer Auth
- Swagger (Swashbuckle)
- HttpClient
- Docker

---

## 📦 Variabili di ambiente (.env)

```env
# JWT
JWT__Key=questa-e-una-chiave-super-segreta-lunga
JWT__Issuer=CocktailDebacle
JWT__Audience=CocktailDebacleUsers

# API Keys
Pexels__ApiKey=your-pexels-api-key
Pixabay__ApiKey=your-pixabay-api-key
Unsplash__AccessKey=your-unsplash-access-key
```

> 🔐 **NB**: .env non pubblico

---

## 🐳 Docker

### Build e avvio standalone

```bash
docker build -t image-fetcher-service .
docker run -p 5087:80 --env-file .env image-fetcher-service
```

---

## 🔁 API Reference

### 🔐 `GET /imagefetcher/search?query=negroni`

Richiede token JWT con ruolo `user`.

#### Headers

```http
Authorization: Bearer <your-jwt-token>
```

#### Risposta `200 OK`

```json
[
  {
    "url": "https://...jpg",
    "source": "pexels",
    "photographer": "Nome",
    "photographerUrl": "https://...",
    "description": "cocktail, negroni"
  },
  ...
]
```

#### Risposta `204 No Content`

Tutti i provider hanno fallito o nessuna immagine trovata.

#### Risposta `401 Unauthorized`

Token mancante o invalido.

---

## 🔐 Autenticazione

JWT validato con parametri presenti in `appsettings.json` / `.env`.  
Tutti gli endpoint sono protetti da `[Authorize]`.

---

## 🧪 Test via `curl`

```bash
curl -X GET "http://localhost:5000/imagefetcher/search?query=negroni" \
  -H "Authorization: Bearer <jwt-token>"
```

---

## 🧭 Note tecniche

- Se Unsplash risponde con `403` (rate limit), il servizio ribilancia le immagini extra su Pexels e Pixabay.
- L'output totale è sempre ~12 immagini (quando possibile).
- In caso di errore su tutti i provider, restituisce `204`.

---

## 🧩 Dipendenze NuGet

- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Swashbuckle.AspNetCore`

---

## 📁 Struttura principale

```
ImageFetcherService/
├── Controllers/
│   └── ImageSearchController.cs
├── Services/
│   ├── PexelsClient.cs
│   ├── PixabayClient.cs
│   └── UnsplashClient.cs
├── Models/
│   └── ImageResultDto.cs
├── Program.cs
├── Dockerfile
└── appsettings.json
```

---

## 🛠️ Manutentore

Stefano Montuori – [GitHub](https://github.com/StefanoMontuori) – Firenze, Italia
