# Portima Standards MCP

Serveur MCP permettant à GitHub Copilot d'accéder au repository `sample-api` pour consulter les standards de développement .NET Portima.

> **🎯 Nouveau !** Documentation complète disponible - Voir ci-dessous pour réponses à vos questions sur le projet et son déploiement.

## 📚 Documentation complète

### 🔥 Pour commencer (LISEZ CECI EN PREMIER)
- **[REPONSES_QUESTIONS.md](./REPONSES_QUESTIONS.md)** - **RÉPONSES DIRECTES** à vos questions sur le projet, le déploiement, et les recommandations

### 📖 Documentation détaillée
- **[DOCUMENTATION_PROJET.md](./DOCUMENTATION_PROJET.md)** - Analyse complète du projet, comparaison avec solutions existantes
- **[DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)** - Guide détaillé des options de déploiement (local, global tool, serveur centralisé, container)
- **[IMPROVEMENT_PLAN.md](./IMPROVEMENT_PLAN.md)** - Plan d'amélioration avec roadmap et priorités

## 🎯 Qu'est-ce que ce projet fait ?

Ce serveur MCP permet aux développeurs Portima de demander à GitHub Copilot :

```
"Comment sample-api implémente-t-il OpenTelemetry ?"
"Montre-moi la structure du Program.cs de sample-api"
"Quelles sont les bonnes pratiques de configuration dans sample-api ?"
```

Et Copilot **accède directement au code de sample-api** dans Azure DevOps pour répondre avec des exemples concrets et à jour.

### Avantages

✅ Documentation toujours à jour (le code est la source de vérité)
✅ Exemples concrets du vrai code Portima
✅ Intégré dans le workflow (directement dans VS Code/Visual Studio)
✅ Standardisation du code entre projets

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