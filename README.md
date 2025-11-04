# Portima Standards MCP

Serveur MCP permettant à GitHub Copilot d'accéder au repository `sample-api` pour consulter les standards de développement .NET Portima.

## 📚 Documentation complète

- **[DOCUMENTATION_PROJET.md](./DOCUMENTATION_PROJET.md)** - Analyse complète du projet, comparaison avec solutions existantes, et réponses à vos questions
- **[DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)** - Guide détaillé des différentes options de déploiement (local, global tool, serveur centralisé, container)
- **[IMPROVEMENT_PLAN.md](./IMPROVEMENT_PLAN.md)** - Plan d'amélioration avec roadmap et priorités

## Prérequis

- .NET 9.0 SDK
- PAT Azure DevOps avec permission **Code: Read**

## Installation

### 1. Cloner et builder

```bash
git clone https://github.com/ElAyadi-Ilias/portima-standards-mcp.git
cd portima-standards-mcp
dotnet build
```

### 2. Configurer le PAT

Créez un PAT sur https://dev.azure.com/tfsportima avec permission **Code: Read**.

Copiez `appsettings.template.json` vers `appsettings.json` et renseignez votre PAT :

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/tfsportima",
    "PersonalAccessToken": "VOTRE_PAT_ICI"
  }
}
```

### 3. Configuration VS Code

## Utilisation

Posez des questions à Copilot :
- "Comment implémenter OpenTelemetry selon sample-api ?"
- "Montre-moi la structure du Program.cs de sample-api"
- "Compare mon logging avec les standards Portima"
- "Quelles sont les dépendances utilisées dans sample-api ?"
- "Comment est configuré le logging dans sample-api ?"

## Outils disponibles

- `ListRepositoryFiles` : Liste les fichiers
- `GetRepositoryFileContent` : Récupère un fichier
- `GetAllRepositoryFilesContent` : Récupère tous les fichiers

## Sécurité

- ✅ Configurez le PAT dans `appsettings.json` (ignoré par Git)
- ✅ Configurez une expiration (90 jours max)
- ❌ Ne commitez jamais `appsettings.json`

## Dépannage

**Erreur "Personal Access Token is not configured"**  
→ Vérifiez que `appsettings.json` existe avec votre PAT

**Erreur "Unauthorized"**  
→ Vérifiez que le PAT a la permission Code: Read

**MCP ne démarre pas**  
→ Vérifiez le chemin absolu dans `.vscode/mcp.json`