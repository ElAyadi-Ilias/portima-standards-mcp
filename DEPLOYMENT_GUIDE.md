# Guide de Déploiement - Portima Standards MCP

## 🎯 Vue d'ensemble

Ce guide présente les différentes options de déploiement du serveur MCP Portima Standards, avec des instructions étape par étape pour chaque scénario.

---

## 📋 Prérequis communs

Quel que soit le mode de déploiement choisi :

- .NET 9.0 SDK
- Accès à Azure DevOps (https://dev.azure.com/tfsportima)
- Permission de créer un Personal Access Token (PAT) avec **Code: Read**
- GitHub Copilot configuré dans VS Code ou Visual Studio

---

## Option 1️⃣ : Installation locale (Développeur individuel)

### Pour qui ?
- Tests initiaux
- Développeurs isolés
- Prototypage rapide

### Étapes d'installation

#### 1. Cloner le repository

```bash
git clone https://github.com/ElAyadi-Ilias/portima-standards-mcp.git
cd portima-standards-mcp
```

#### 2. Créer un Personal Access Token (PAT)

1. Aller sur https://dev.azure.com/tfsportima/_usersSettings/tokens
2. Cliquer sur **"New Token"**
3. Configurer :
   - **Name**: `Portima MCP - [Votre Nom]`
   - **Organization**: `tfsportima`
   - **Expiration**: 90 jours (ou selon politique)
   - **Scopes**: **Code: Read** uniquement
4. Copier le token (vous ne le verrez qu'une fois !)

#### 3. Configurer l'application

```bash
# Copier le template
cp PortimaStandardsMcp/appsettings.template.json PortimaStandardsMcp/appsettings.json

# Éditer le fichier
nano PortimaStandardsMcp/appsettings.json
```

Remplacer `YOUR_PAT_HERE` par votre token :

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/tfsportima",
    "PersonalAccessToken": "VOTRE_TOKEN_ICI"
  }
}
```

#### 4. Build et test

```bash
cd PortimaStandardsMcp
dotnet build
dotnet run
```

Si tout fonctionne, le serveur MCP démarre et attend les connexions.

#### 5. Configurer VS Code

##### Option A : Configuration utilisateur (recommandé)

Créer/éditer `~/.vscode/mcp.json` (Linux/Mac) ou `%USERPROFILE%\.vscode\mcp.json` (Windows) :

```json
{
  "servers": {
    "portima-standards": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/chemin/absolu/vers/portima-standards-mcp/PortimaStandardsMcp/PortimaStandardsMcp.csproj"
      ]
    }
  }
}
```

**Important** : Remplacer `/chemin/absolu/vers/` par le chemin réel sur votre machine.

##### Option B : Configuration workspace (pour un projet spécifique)

Dans votre projet, créer `.vscode/mcp.json` avec le même contenu.

#### 6. Redémarrer VS Code

Pour que la configuration soit prise en compte.

#### 7. Tester avec Copilot

Ouvrir le chat Copilot et tester :

```
@workspace Comment sample-api implémente-t-il la configuration ?
```

ou

```
Montre-moi la structure du Program.cs dans sample-api
```

---

## Option 2️⃣ : .NET Global Tool

### Pour qui ?
- Déploiement à plusieurs développeurs
- Installation simplifiée
- Mises à jour centralisées

### Préparation du package (Administrateur)

#### 1. Modifier le .csproj pour global tool

Éditer `PortimaStandardsMcp/PortimaStandardsMcp.csproj` :

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Configuration pour .NET Global Tool -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>portima-mcp</ToolCommandName>
    <PackageId>Portima.Standards.Mcp</PackageId>
    <Version>1.0.0</Version>
    <Authors>Portima DevOps Team</Authors>
    <Company>Portima</Company>
    <Description>MCP Server pour accéder aux standards Portima depuis GitHub Copilot</Description>
    <PackageOutputPath>./nupkg</PackageOutputPath>
  </PropertyGroup>

  <!-- Reste du fichier inchangé -->
</Project>
```

#### 2. Créer le package

```bash
cd PortimaStandardsMcp
dotnet pack -c Release
```

Le package sera créé dans `PortimaStandardsMcp/nupkg/`

#### 3. Publier sur Azure Artifacts

```bash
# Configurer Azure Artifacts comme source (une seule fois)
dotnet nuget add source "https://pkgs.dev.azure.com/tfsportima/_packaging/PortimaTools/nuget/v3/index.json" \
  --name "PortimaTools" \
  --username "portima" \
  --password "VOTRE_PAT_AVEC_PACKAGING_WRITE"

# Publier le package
dotnet nuget push ./nupkg/Portima.Standards.Mcp.1.0.0.nupkg \
  --source "PortimaTools" \
  --api-key az
```

### Installation par les développeurs

#### 1. Configurer la source NuGet (une seule fois)

```bash
dotnet nuget add source "https://pkgs.dev.azure.com/tfsportima/_packaging/PortimaTools/nuget/v3/index.json" \
  --name "PortimaTools" \
  --username "VOTRE_EMAIL" \
  --password "VOTRE_PAT_AVEC_PACKAGING_READ"
```

#### 2. Installer le tool

```bash
dotnet tool install -g Portima.Standards.Mcp --add-source PortimaTools
```

#### 3. Configurer le PAT

Créer `~/.portima-mcp/appsettings.json` :

```bash
# Linux/Mac
mkdir -p ~/.portima-mcp
cat > ~/.portima-mcp/appsettings.json << 'EOF'
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/tfsportima",
    "PersonalAccessToken": "VOTRE_PAT_ICI"
  }
}
EOF
```

```powershell
# Windows PowerShell
New-Item -Path "$env:USERPROFILE\.portima-mcp" -ItemType Directory -Force
@"
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/tfsportima",
    "PersonalAccessToken": "VOTRE_PAT_ICI"
  }
}
"@ | Out-File -FilePath "$env:USERPROFILE\.portima-mcp\appsettings.json" -Encoding UTF8
```

#### 4. Modifier le code pour chercher la config dans le home directory

Dans `Program.cs`, modifier :

```csharp
var configPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".portima-mcp",
    "appsettings.json"
);

builder.Configuration
    .AddJsonFile(configPath, optional: false, reloadOnChange: true);
```

#### 5. Configurer VS Code

```json
{
  "servers": {
    "portima-standards": {
      "type": "stdio",
      "command": "portima-mcp",
      "args": []
    }
  }
}
```

#### 6. Mise à jour

```bash
dotnet tool update -g Portima.Standards.Mcp
```

---

## Option 3️⃣ : Serveur centralisé (Azure App Service)

### Pour qui ?
- Organisation avec beaucoup de développeurs
- Gestion centralisée souhaitée
- Service account disponible

### Architecture

```
Développeur → Copilot → MCP Client → HTTPS → Azure App Service → Azure DevOps
```

### Préparation

#### 1. Modifier pour supporter HTTP

Créer `PortimaStandardsMcp/HttpProgram.cs` :

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PortimaStandardsMcp;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables(); // Pour Azure App Service

builder.Services
    .AddMcpServer()
    .WithSseServerTransport() // Server-Sent Events pour HTTP
    .WithToolsFromAssembly(typeof(PortimaDevOpsTools).Assembly);

builder.Services.AddSingleton<PortimaDevOpsService>();

var app = builder.Build();

app.MapMcpEndpoint("/mcp");

await app.RunAsync();
```

#### 2. Déployer sur Azure App Service

```bash
# Créer le App Service
az group create --name rg-portima-mcp --location westeurope

az appservice plan create \
  --name plan-portima-mcp \
  --resource-group rg-portima-mcp \
  --sku B1 \
  --is-linux

az webapp create \
  --name portima-standards-mcp \
  --resource-group rg-portima-mcp \
  --plan plan-portima-mcp \
  --runtime "DOTNET|9.0"

# Configurer les variables d'environnement
az webapp config appsettings set \
  --name portima-standards-mcp \
  --resource-group rg-portima-mcp \
  --settings \
    AzureDevOps__OrganizationUrl="https://dev.azure.com/tfsportima" \
    AzureDevOps__PersonalAccessToken="SERVICE_ACCOUNT_PAT"

# Déployer
dotnet publish -c Release
cd bin/Release/net9.0/publish
zip -r ../deploy.zip *
az webapp deployment source config-zip \
  --resource-group rg-portima-mcp \
  --name portima-standards-mcp \
  --src ../deploy.zip
```

#### 3. Configuration développeur

```json
{
  "servers": {
    "portima-standards": {
      "type": "http",
      "url": "https://portima-standards-mcp.azurewebsites.net/mcp"
    }
  }
}
```

### Sécurité

#### Ajouter authentification Azure AD

```csharp
builder.Services.AddAuthentication("Bearer")
    .AddMicrosoftIdentityWebApi(builder.Configuration);

app.UseAuthentication();
app.UseAuthorization();
```

---

## Option 4️⃣ : Docker Container

### Pour qui ?
- Déploiement sur Kubernetes
- Environnement containerisé
- Infrastructure on-premise

### 1. Créer le Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PortimaStandardsMcp/PortimaStandardsMcp.csproj", "PortimaStandardsMcp/"]
RUN dotnet restore "PortimaStandardsMcp/PortimaStandardsMcp.csproj"

COPY . .
WORKDIR "/src/PortimaStandardsMcp"
RUN dotnet build "PortimaStandardsMcp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PortimaStandardsMcp.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .

ENV AzureDevOps__OrganizationUrl=""
ENV AzureDevOps__PersonalAccessToken=""

ENTRYPOINT ["dotnet", "PortimaStandardsMcp.dll"]
```

### 2. Build et run

```bash
# Build
docker build -t portima-standards-mcp:1.0 .

# Run
docker run -d \
  -p 8080:8080 \
  -e AzureDevOps__OrganizationUrl="https://dev.azure.com/tfsportima" \
  -e AzureDevOps__PersonalAccessToken="VOTRE_PAT" \
  portima-standards-mcp:1.0
```

### 3. Déployer sur Azure Container Apps

```bash
az containerapp create \
  --name portima-mcp \
  --resource-group rg-portima-mcp \
  --environment portima-env \
  --image portima-standards-mcp:1.0 \
  --target-port 8080 \
  --ingress external \
  --secrets \
    ado-pat="VOTRE_PAT" \
  --env-vars \
    AzureDevOps__OrganizationUrl="https://dev.azure.com/tfsportima" \
    AzureDevOps__PersonalAccessToken=secretref:ado-pat
```

---

## 🔄 Tableau de comparaison

| Critère | Local | Global Tool | App Service | Container |
|---------|-------|-------------|-------------|-----------|
| **Facilité installation** | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **Facilité mise à jour** | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Coût** | Gratuit | Gratuit | ~€50/mois | ~€30/mois |
| **Performance** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Sécurité** | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Standardisation** | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Dépendances** | Aucune | Aucune | Internet | Internet |

---

## 🎯 Recommandation par contexte

### Petit équipe (< 10 développeurs)
→ **Global Tool** avec Azure Artifacts

### Moyenne équipe (10-50 développeurs)
→ **Global Tool** + **App Service** en option

### Grande équipe (50+ développeurs)
→ **App Service** ou **Container** centralisé

### Environnement sécurisé/on-premise
→ **Container** sur infrastructure interne

---

## 📝 Checklist de déploiement

### Avant le déploiement

- [ ] Définir la stratégie de déploiement
- [ ] Créer un service account Azure DevOps (si centralisé)
- [ ] Préparer la documentation utilisateur
- [ ] Identifier un groupe pilote

### Pendant le déploiement

- [ ] Configurer l'infrastructure (si centralisé)
- [ ] Tester avec le groupe pilote
- [ ] Collecter les feedbacks
- [ ] Ajuster la configuration

### Après le déploiement

- [ ] Former les développeurs
- [ ] Créer une FAQ
- [ ] Mettre en place un support
- [ ] Planifier les mises à jour

---

## 🆘 Support et troubleshooting

### Erreurs communes

#### "Personal Access Token is not configured"

**Cause** : Le fichier `appsettings.json` n'existe pas ou est mal configuré

**Solution** :
1. Vérifier que le fichier existe
2. Vérifier que le PAT est correctement copié
3. Vérifier les permissions du fichier

#### "Unauthorized"

**Cause** : Le PAT n'a pas les bonnes permissions ou a expiré

**Solution** :
1. Vérifier la date d'expiration du PAT
2. Vérifier que le scope **Code: Read** est bien activé
3. Créer un nouveau PAT si nécessaire

#### "MCP server not responding"

**Cause** : Le serveur ne démarre pas ou le chemin est incorrect

**Solution** :
1. Tester `dotnet run` manuellement
2. Vérifier les logs dans VS Code
3. Vérifier le chemin absolu dans `mcp.json`

### Logs et debugging

```bash
# Activer les logs détaillés
export DOTNET_LOGGING__LOGLEVEL__DEFAULT=Debug

# Vérifier la connexion Azure DevOps
curl -u :VOTRE_PAT https://dev.azure.com/tfsportima/_apis/projects?api-version=7.1
```

---

## 📞 Contact

Pour toute question sur le déploiement :
- **Email** : devops@portima.com
- **Teams** : Canal #portima-mcp
- **Documentation** : Voir DOCUMENTATION_PROJET.md

---

## 🔄 Versioning et mises à jour

### Stratégie de versioning

- **Major** (1.0.0 → 2.0.0) : Changements incompatibles
- **Minor** (1.0.0 → 1.1.0) : Nouvelles fonctionnalités
- **Patch** (1.0.0 → 1.0.1) : Corrections de bugs

### Communication des mises à jour

1. Annoncer sur Teams 1 semaine avant
2. Créer une note de version (CHANGELOG.md)
3. Planifier une fenêtre de maintenance (si serveur centralisé)
4. Tester en pré-production

---

## ✅ Prochaines étapes

Après avoir choisi votre option de déploiement :

1. [ ] Suivre les instructions de la section correspondante
2. [ ] Tester avec un utilisateur pilote
3. [ ] Documenter les cas d'usage spécifiques à votre équipe
4. [ ] Former les développeurs
5. [ ] Planifier la maintenance et les mises à jour

Bon déploiement ! 🚀
