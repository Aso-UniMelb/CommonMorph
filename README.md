# CommonMorph

CommonMorph is an open, collaborative platform for documenting, structuring, and expanding morphological resources, with a special focus on low-resource and endangered languages. It brings together native speakers, field linguists, rule-based computational grammars, active-learning neural models, and modern Large Language Models (LLMs) into a unified, participatory workflow.

---

## 📑 Table of Contents
- [Architecture Overview](#architecture-overview)
- [Project Directory Structure](#project-directory-structure)
- [Prerequisites](#prerequisites)
- [Backend Configuration (.NET Deep-Dive)](#backend-configuration-net-deep-dive)
- [Running in Development Mode](#running-in-development-mode)
- [Building & Publishing for Production](#building--publishing-for-production)
- [Optional: FastAPI Neural Service](#optional-fastapi-neural-service)
- [Citation & Publications](#-citation--publications)
- [License](#license)

---

## 🏛️ Architecture Overview

The system is organized into three primary layers:

1. **Frontend (`ClientApp/`)**:
   - Built with **Svelte 5**, **TypeScript**, and **Vite**.
   - Handles the entire Single-Page Application (SPA) experience: interactive paradigm matrix tables, layer designers, conversational elicitation/verification interfaces, dataset exploration, interactive dialect maps (Leaflet), and contributor dashboards.
   - Compiles down to static assets in `wwwroot/`.

2. **Core Backend (`ASP.NET Core 10 Web API`)**:
   - Serves as the primary API layer and static web host.
   - Built with **C# (.NET 10)** using **Entity Framework Core** and **Dapper** with **PostgreSQL** (`cmdb`).
   - Manages secure cookie authentication, role authorizations (*Admin*, *Linguist*, *Speaker*), data export/import, paradigm inheritance engines, and multi-provider LLM integrations (*OpenAI*, *Groq*, *Google AI Studio*).

3. **Active Learning Service (`FastAPI/`)** *(Optional/Secondary)*:
   - Python-based microservice for training neural morphological inflection models and calculating active-learning uncertainties.

```
       ┌────────────────────────────────────────────────────────┐
       │             Browser Client (Svelte 5 SPA)              │
       └───────────────────────────┬────────────────────────────┘
                                   │ HTTP / JSON API
                                   ▼
       ┌────────────────────────────────────────────────────────┐
       │           ASP.NET Core 10 Backend & Web Host           │
       │  (Static File Server, Cookie Auth, API Controllers)    │
       └──────────────┬──────────────────────────┬──────────────┘
                      │                          │
                      ▼                          ▼
         ┌─────────────────────────┐   ┌─────────────────────────┐
         │   PostgreSQL Database   │   │ External LLMs (OpenAI,  │
         │         (cmdb)          │   │  Groq, Google AI Studio)│
         └─────────────────────────┘   └─────────────────────────┘
```

---

## 📂 Project Directory Structure

```
CommonMorph/
│
├── ClientApp/                    # Frontend source (Svelte 5 + Vite + TypeScript)
│   ├── src/
│   │   ├── lib/
│   │   │   ├── api.ts            # Centralized fetch client with cookie auth & error handling
│   │   │   ├── components/       # Reusable UI components (Header, Drawer, Modal, Map, etc.)
│   │   │   └── stores/           # Svelte reactive stores (auth, language, i18n, router)
│   │   ├── routes/               # View routes (Home, Datasets, Linguist, Elicit, Check, etc.)
│   │   ├── app.css               # Design system tokens and global styles
│   │   ├── App.svelte            # Root application router component
│   │   └── main.ts               # Svelte app bootstrap entry point
│   ├── package.json              # Frontend dependencies and scripts
│   ├── vite.config.ts            # Vite build configuration and dev API proxy
│   └── tsconfig.json             # TypeScript configuration
│
├── Controllers/                  # ASP.NET Core REST API Controllers
│   ├── AffixController.cs        # Affixes, position slots, and feature values
│   ├── CheckController.cs        # Speaker verification and review workflows
│   ├── ElicitController.cs       # Conversational elicitation forms & batch workflows
│   ├── InflectionClassController.cs # Inflection classes and declension tables
│   ├── LangController.cs         # Language variety management & metadata
│   ├── LemmaController.cs        # Lexicon and lemma entries
│   ├── LLMController.cs          # Multi-model LLM generation (OpenAI, Groq, Google AI)
│   ├── MorphophonologyController.cs # Phonological replacement and sandhi rules
│   ├── StructureController.cs    # Paradigm structures and reusable layer inheritance
│   ├── SurveyController.cs       # Contributor surveys and research feedback
│   └── UserController.cs         # Authentication, registration, password resets & roles
│
├── Services/                     # C# Backend Services & Helpers
│   ├── CacheService.cs           # In-memory caching layer for language structures
│   └── EmailService.cs           # MailKit transaction email sender
│
├── Views/                        # Legacy/Razor fallback views (if applicable)
├── wwwroot/                      # Public static directory (contains compiled Svelte SPA)
│   ├── index.html                # Compiled SPA entry page
│   └── assets/                   # Compiled JS and CSS bundles
│
├── FastAPI/                      # Optional Python neural active learning microservice
├── Properties/
│   └── launchSettings.json       # Local dev profiles (ports, environment variables)
├── _DbContext.cs                 # Entity Framework Core DbContext and database models
├── appsettings.json              # Main application configuration (DB, API Keys, SMTP)
├── appsettings.Development.json  # Local development overrides
├── common-morph-backend.csproj   # .NET project file and NuGet dependencies
├── common-morph-backend.sln      # Visual Studio / .NET Solution file
└── Program.cs                    # ASP.NET Core pipeline initialization & middleware
```

---

## ⚙️ Prerequisites

Make sure the following tools are installed on your development machine:

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download)** (Required for backend).
   Verify by running:
   ```bash
   dotnet --version
   ```
2. **[Bun](https://bun.sh/)** (Recommended) or **[Node.js (v18+)](https://nodejs.org/)** (Required for frontend).
   Verify by running:
   ```bash
   bun --version
   ```
3. **[PostgreSQL (v14+)](https://www.postgresql.org/)** (Database).

---

## 🔧 Backend Configuration (.NET Deep-Dive)

For developers unfamiliar with .NET projects, ASP.NET Core configuration is handled via **`appsettings.json`**, optionally overridden by **`appsettings.Development.json`** or **Environment Variables**.

### 1. Understanding `appsettings.json`

Open `appsettings.json` in the root folder to configure database access and service keys:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=cmdb;Username=postgres;Password=YOUR_POSTGRES_PASSWORD;"
  },
  "MailKitOptions": {
    "Server": "smtp.your-server.com",
    "Port": 587,
    "SenderEmail": "noreply@commonmorph.org",
    "Account": "noreply@commonmorph.org",
    "Password": "YOUR_SMTP_PASSWORD",
    "SenderName": "CommonMorph"
  },
  "GoogleClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
  "GoogleClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
  "OpenAI": "sk-proj-YOUR_OPENAI_API_KEY",
  "Groq": "gsk_YOUR_GROQ_API_KEY",
  "GoogleAIStudio": "YOUR_GEMINI_API_KEY",
  "OpenRouter": "sk-or-YOUR_OPENROUTER_API_KEY",
  "AllowedHosts": "*"
}
```

### 2. Configuration Settings Breakdown

| Key | Description |
| :--- | :--- |
| **`ConnectionStrings:DefaultConnection`** | PostgreSQL connection string (`Host`, `Port`, `Database`, `Username`, `Password`). |
| **`OpenAI`** | API key for OpenAI models (e.g. `gpt-4o-mini`, `gpt-4o`). |
| **`Groq`** | API key for ultra-fast Llama-3 models on Groq. |
| **`GoogleAIStudio`** | API key for Google Gemini models (e.g. `gemini-2.0-flash`). |
| **`MailKitOptions`** | SMTP credentials used to send email verification & password reset links. |
| **`GoogleClientId` / `Secret`** | Optional Google OAuth 2.0 credentials for "Sign in with Google". |

> [!TIP]
> **Environment Variables**: Any configuration in `appsettings.json` can be overridden in production using standard environment variables (e.g. `CONNECTION_STRING`, `OpenAI`, `Groq`, `GoogleAIStudio`).

---

## 🚀 Running in Development Mode

During development, you run the **.NET Backend** and the **Svelte Frontend** concurrently in two separate terminal windows.

### Terminal 1: Start the .NET Backend
From the root folder (`CommonMorph/`):
```bash
dotnet run
```
* The backend will start on **`http://localhost:5041`** (and `https://localhost:7242`).
* To automatically rebuild on C# file changes, you can also use `dotnet watch`.

### Terminal 2: Start the Svelte Dev Server
From the root folder, navigate to `ClientApp/`:
```bash
cd ClientApp
bun install       # Install frontend packages (first time only)
bun run dev       # Start the Vite development server
```
* The frontend will start on **`http://localhost:5173`**.
* **Hot Module Replacement (HMR)** is enabled: your browser will update instantly as you edit `.svelte` and `.ts` files.
* Vite automatically proxies backend requests (`/User`, `/Lang`, `/Elicit`, `/LLM`, etc.) directly to `http://localhost:5041`.

Open **`http://localhost:5173`** in your browser to interact with the application.

---

## 📦 Building & Publishing for Production

When preparing a release to deploy to your production server:

### Step 1: Compile the Svelte Frontend
```bash
cd ClientApp
bun run build
```
* Vite will bundle all TypeScript and Svelte components into optimized, minified assets and place them directly into the backend static folder: `../wwwroot/`.

### Step 2: Publish the .NET Backend & Assets
Return to the project root and run the `dotnet publish` command:
```bash
cd ..
dotnet publish -c Release -o ./publish
```
* This compiles the C# codebase in `Release` mode and packages the executable binaries, runtime dependencies, `appsettings.json`, and the entire `wwwroot/` folder into the `./publish/` directory.

### Step 3: Deploy to Server
Copy the contents of `./publish/` to your server:
1. Ensure .NET 10 runtime and PostgreSQL are installed on the server.
2. Update `appsettings.json` on the server with your production database credentials and API keys.
3. Start the application:
   ```bash
   dotnet common-morph-backend.dll
   ```
4. Set up a reverse proxy (such as **Nginx**, **Caddy**, or **IIS**) to forward public HTTPS traffic to port `5000` or `5041`.

---

## 🧠 Optional: FastAPI Neural Service

If using the neural active-learning suggestion microservice:

```bash
cd FastAPI
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

---

## 📚 Citation & Publications

If you use CommonMorph in your research, field documentation, or NLP pipelines, please cite our LREC 2026 paper:

> Aso Mahmudi, Sina Ahmadi, Kemal Maulana Kurniawan, Rico Sennrich, Eduard H. Hovy, and Ekaterina Vylomova. 2026. **CommonMorph: Participatory Morphological Documentation Platform**. In *Proceedings of the Fifteenth Language Resources and Evaluation Conference*, pages 11735–11746, Palma de Mallorca, Spain. ELRA Language Resource Association. [doi:10.63317/5gqigwzjjv4b](https://doi.org/10.63317/5gqigwzjjv4b).

- **ACL Anthology**: [https://aclanthology.org/2026.lrec-1.919/](https://aclanthology.org/2026.lrec-1.919/)
- **LREC 2026**: [https://lrec.elra.info/lrec2026-main-919](https://lrec.elra.info/lrec2026-main-919)

### BibTeX
```bibtex
@inproceedings{mahmudi-etal-2026-commonmorph,
    title = "{C}ommon{M}orph: Participatory Morphological Documentation Platform",
    author = "Mahmudi, Aso  and
      Ahmadi, Sina  and
      Kurniawan, Kemal Maulana  and
      Sennrich, Rico  and
      Hovy, Eduard H.  and
      Vylomova, Ekaterina",
    booktitle = "Proceedings of the Fifteenth Language Resources and Evaluation Conference",
    month = may,
    year = "2026",
    address = "Palma de Mallorca, Spain",
    publisher = "ELRA Language Resource Association",
    url = "https://aclanthology.org/2026.lrec-1.919/",
    doi = "10.63317/5gqigwzjjv4b",
    pages = "11735--11746",
}
```

---

## 📄 License

This project is open-source under the [MIT License](LICENSE).
