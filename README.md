# Portima Standards MCP

Serveur MCP (Model Context Protocol) permettant aux développeurs Portima d'accéder au repository de référence `sample-api` pour consulter et appliquer les standards de développement .NET via GitHub Copilot.

## 🎯 Objectif

Ce MCP permet à GitHub Copilot d'interroger directement le repository Azure DevOps `sample-api` pour :
- Consulter les implémentations de référence (ex: OpenTelemetry, logging, etc.)
- Comparer votre code avec les standards Portima
- Obtenir des exemples concrets basés sur `sample-api`
- Assurer la conformité aux bonnes pratiques de l'entreprise

## 📋 Prérequis

- .NET 9.0 SDK ou supérieur
- Accès au Azure DevOps Portima (`https://dev.azure.com/tfsportima`)
- Personal Access Token (PAT) Azure DevOps avec permissions de lecture sur les repositories

## 🔧 Installation

### 1. Cloner le repository

```bash
git clone https://github.com/ElAyadi-Ilias/portima-standards-mcp.git
cd portima-standards-mcp
```

### 2. Configurer le Personal Access Token (PAT)

⚠️ **IMPORTANT - SÉCURITÉ** : Ne jamais hardcoder le PAT dans le code source !

#### Créer un PAT Azure DevOps

1. Allez sur Azure DevOps : https://dev.azure.com/tfsportima
2. Cliquez sur votre profil → **Personal Access Tokens**
3. Créez un nouveau token avec les permissions :
   - **Code** : Read
   - **Scope** : Full access ou spécifique au projet "Portima DevOps"
4. Copiez le token généré (vous ne pourrez plus le voir après)

#### Configurer la variable d'environnement

**Windows (PowerShell):**
```powershell
# Session courante
$env:AZURE_DEVOPS_PAT = "votre-pat-ici"

# Permanent (utilisateur)
[System.Environment]::SetEnvironmentVariable('AZURE_DEVOPS_PAT', 'votre-pat-ici', 'User')
```

**Windows (CMD):**
```cmd
set AZURE_DEVOPS_PAT=votre-pat-ici
```

**Linux/macOS:**
```bash
# Session courante
export AZURE_DEVOPS_PAT="votre-pat-ici"

# Permanent (ajoutez à ~/.bashrc ou ~/.zshrc)
echo 'export AZURE_DEVOPS_PAT="votre-pat-ici"' >> ~/.bashrc
source ~/.bashrc
```

### 3. Build le projet

```bash
dotnet build
```

### 4. Tester le serveur

```bash
dotnet run --project PortimaStandardsMcp/
```

## 🚀 Configuration avec GitHub Copilot

### VS Code

1. Créez ou modifiez le fichier `.vscode/mcp.json` dans votre projet :

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

2. Remplacez `/chemin/absolu/vers/` par le chemin complet vers votre clone du repository

3. Redémarrez VS Code

### Visual Studio

1. Configuration similaire dans les paramètres Copilot
2. Référez-vous à la documentation Microsoft pour Visual Studio MCP configuration

## 📚 Utilisation

Une fois configuré, vous pouvez interagir avec Copilot en lui posant des questions sur les standards Portima :

### Exemples de questions

```
"Comment est implémenté OpenTelemetry dans sample-api ?"
→ Copilot utilisera le MCP pour récupérer les fichiers pertinents de sample-api

"Montre-moi comment configurer le logging selon les standards Portima"
→ Consulte les fichiers de configuration et classes de logging dans sample-api

"Quels sont les fichiers de configuration dans sample-api ?"
→ Liste tous les fichiers .config du repository de référence

"Compare mon code de startup avec celui de sample-api"
→ Récupère le code de sample-api pour comparaison
```

## 🛠️ Outils MCP disponibles

Le serveur MCP expose 3 outils que Copilot peut utiliser automatiquement :

### 1. **ListRepositoryFiles**
Liste les fichiers d'un repository (.cs et .config par défaut)

**Paramètres:**
- `projectName` : Nom du projet Azure DevOps (ex: "Portima DevOps")
- `repoName` : Nom du repository (ex: "sample-api")
- `branch` : Branche (ex: "dev")

### 2. **GetRepositoryFileContent**
Récupère le contenu d'un fichier spécifique

**Paramètres:**
- `projectName` : Nom du projet
- `repoName` : Nom du repository
- `filePath` : Chemin du fichier (ex: "/Program.cs")
- `branch` : Branche

### 3. **GetAllRepositoryFilesContent**
Récupère le contenu de tous les fichiers d'un repository

**Paramètres:**
- `projectName` : Nom du projet
- `repoName` : Nom du repository
- `branch` : Branche

## 🔐 Bonnes pratiques de sécurité

### ✅ À FAIRE

- ✅ Utiliser la variable d'environnement `AZURE_DEVOPS_PAT`
- ✅ Stocker le PAT dans un gestionnaire de secrets (Azure Key Vault, 1Password, etc.)
- ✅ Définir une date d'expiration sur le PAT (90 jours maximum recommandé)
- ✅ Donner uniquement les permissions minimales nécessaires (Read sur Code)
- ✅ Révoquer les PAT non utilisés

### ❌ À NE PAS FAIRE

- ❌ Hardcoder le PAT dans le code source
- ❌ Commiter le PAT dans Git
- ❌ Partager votre PAT avec d'autres personnes
- ❌ Utiliser un PAT avec trop de permissions
- ❌ Laisser un PAT sans expiration

## 📖 Architecture

```
portima-standards-mcp/
├── PortimaStandardsMcp/
│   ├── Program.cs                 # Point d'entrée du serveur MCP
│   ├── PortimaDevOpsService.cs    # Service Azure DevOps (gestion PAT, API calls)
│   ├── PortimaDevOpsTools.cs      # Outils MCP exposés à Copilot
│   └── PortimaStandardsMcp.csproj
├── .gitignore                     # Exclut bin/, obj/, secrets
└── README.md
```

## 🐛 Dépannage

### Erreur : "La variable d'environnement AZURE_DEVOPS_PAT n'est pas définie"

**Solution:** Configurez la variable d'environnement `AZURE_DEVOPS_PAT` avec votre Personal Access Token (voir section Installation)

### Erreur : "Unauthorized" ou "Access Denied"

**Causes possibles:**
- PAT expiré → Créez un nouveau PAT
- PAT sans permissions suffisantes → Vérifiez les permissions (Code: Read minimum)
- Mauvais PAT → Vérifiez que vous avez copié le bon token

### Le serveur MCP ne démarre pas dans VS Code

**Solutions:**
1. Vérifiez que le chemin dans `.vscode/mcp.json` est correct et absolu
2. Testez manuellement : `dotnet run --project PortimaStandardsMcp/`
3. Consultez les logs de Copilot dans VS Code (Output → GitHub Copilot)

### Copilot ne trouve pas les fichiers de sample-api

**Vérifications:**
- Le PAT a bien accès au projet "Portima DevOps"
- Le repository "sample-api" existe et la branche "dev" est accessible
- Votre compte Azure DevOps a les permissions de lecture

## 🚀 Déploiement pour toute l'entreprise

### Option 1: Package NuGet interne (Recommandé)

```bash
# Publier comme outil .NET global
dotnet pack
dotnet tool install --global --add-source ./nupkg PortimaStandardsMcp

# Les utilisateurs installent ensuite :
dotnet tool install -g PortimaStandardsMcp
```

### Option 2: Distribution via Git

1. Les développeurs clonent le repository
2. Configuration centralisée via documentation partagée
3. Chaque développeur configure son propre PAT

### Option 3: Serveur MCP centralisé HTTP

- Déployer sur un serveur interne
- Authentification centralisée
- Les clients se connectent via URL au lieu de STDIO

## 📝 Référence : sample-api

Le repository de référence utilisé par défaut :

```
Projet   : "Portima DevOps"
Repository: "sample-api"
Branche  : "dev"
URL      : https://dev.azure.com/tfsportima/Portima%20DevOps/_git/sample-api
```

Ce repository contient les standards et patterns de développement .NET pour Portima.

## 🤝 Contribution

Pour contribuer à ce MCP :

1. Forkez le repository
2. Créez une branche feature
3. Testez vos changements
4. Soumettez une Pull Request

## 📄 Licence

Propriété de Portima - Usage interne uniquement

## 📞 Support

Pour toute question ou problème :
- Ouvrez une issue sur GitHub
- Contactez l'équipe DevOps Portima