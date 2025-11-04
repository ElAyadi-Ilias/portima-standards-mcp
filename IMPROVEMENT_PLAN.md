# Plan d'amélioration - Portima Standards MCP

## 🎯 Objectif

Améliorer le serveur MCP actuel pour le rendre plus robuste, plus performant et plus facile à utiliser avant le déploiement à grande échelle.

---

## 📊 Améliorations prioritaires

### Priorité HAUTE 🔴

#### 1. Étendre les types de fichiers supportés

**Problème actuel** : Seuls les fichiers `.cs` et `.config` sont récupérés

**Impact** : Les développeurs ne peuvent pas voir :
- Configuration (appsettings.json, launchSettings.json)
- Documentation (README.md, docs/)
- Build (*.csproj, Directory.Build.props)
- CI/CD (azure-pipelines.yml, Dockerfile)
- Tests (fichiers de test)

**Solution** :

```csharp
// Dans PortimaDevOpsService.cs
public async Task<List<string>> ListFilePathsAsync(string projectName, string repoName, string branchName = "dev")
{
    var gitClient = GetGitClient();
    var repo = await gitClient.GetRepositoryAsync(projectName, repoName);

    var items = await gitClient.GetItemsAsync(
        repo.Id,
        scopePath: "/",
        recursionLevel: VersionControlRecursionType.Full,
        versionDescriptor: new GitVersionDescriptor
        {
            Version = branchName,
            VersionType = GitVersionType.Branch
        });

    // Extensions à inclure
    var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Code
        ".cs", ".csproj", ".sln",
        // Configuration
        ".json", ".config", ".xml", ".yaml", ".yml",
        // Documentation
        ".md", ".txt",
        // Scripts
        ".ps1", ".sh", ".dockerfile",
        // Build
        ".props", ".targets"
    };

    // Fichiers spécifiques importants
    var allowedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Dockerfile", "docker-compose.yml", ".gitignore", ".editorconfig"
    };

    return items
        .Where(i => !i.IsFolder)
        .Where(i => 
            allowedExtensions.Contains(Path.GetExtension(i.Path)) ||
            allowedFileNames.Contains(Path.GetFileName(i.Path)))
        .Select(i => i.Path)
        .OrderBy(p => p)
        .ToList();
}
```

**Effort** : 1 heure
**Test** : Vérifier que tous les fichiers pertinents de sample-api sont visibles

---

#### 2. Ajouter un système de cache

**Problème actuel** : Chaque requête interroge Azure DevOps
- Latence élevée
- Consommation inutile de l'API
- Coût en termes de rate limiting

**Impact** : 
- Copilot peut être lent à répondre
- Risque de dépassement des limites API Azure DevOps

**Solution** : Cache en mémoire avec expiration

```csharp
using Microsoft.Extensions.Caching.Memory;

public class PortimaDevOpsService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);
    
    public PortimaDevOpsService(IConfiguration configuration, IMemoryCache cache)
    {
        _orgUrl = configuration["AzureDevOps:OrganizationUrl"] 
            ?? throw new Exception("Azure DevOps organization URL is not configured");
        _pat = configuration["AzureDevOps:PersonalAccessToken"] 
            ?? throw new Exception("Personal Access Token is not configured");
        _cache = cache;
    }
    
    public async Task<List<string>> ListFilePathsAsync(string projectName, string repoName, string branchName = "dev")
    {
        var cacheKey = $"files_{projectName}_{repoName}_{branchName}";
        
        if (_cache.TryGetValue(cacheKey, out List<string> cachedFiles))
        {
            return cachedFiles;
        }
        
        var files = await FetchFilePathsFromAzureDevOpsAsync(projectName, repoName, branchName);
        
        _cache.Set(cacheKey, files, _cacheDuration);
        
        return files;
    }
    
    public async Task<string> GetFileFromBranchAsync(string projectName, string repoName, string filePath, string branchName = "dev")
    {
        var cacheKey = $"content_{projectName}_{repoName}_{branchName}_{filePath}";
        
        if (_cache.TryGetValue(cacheKey, out string cachedContent))
        {
            return cachedContent;
        }
        
        var content = await FetchFileContentFromAzureDevOpsAsync(projectName, repoName, filePath, branchName);
        
        _cache.Set(cacheKey, content, _cacheDuration);
        
        return content;
    }
    
    // Méthodes privées pour fetch réel
    private async Task<List<string>> FetchFilePathsFromAzureDevOpsAsync(...) { /* code actuel */ }
    private async Task<string> FetchFileContentFromAzureDevOpsAsync(...) { /* code actuel */ }
}
```

Ajouter dans `Program.cs` :

```csharp
builder.Services.AddMemoryCache();
```

**Effort** : 2 heures
**Test** : Vérifier que les requêtes répétées sont plus rapides

---

#### 3. Meilleure gestion des erreurs

**Problème actuel** : Les erreurs ne sont pas bien communiquées

**Solution** :

```csharp
public async Task<string> GetFileFromBranchAsync(string projectName, string repoName, string filePath, string branchName = "dev")
{
    try
    {
        var gitClient = GetGitClient();
        var repo = await gitClient.GetRepositoryAsync(projectName, repoName);

        using (var stream = await gitClient.GetItemContentAsync(
            repo.Id,
            path: filePath,
            versionDescriptor: new GitVersionDescriptor
            {
                Version = branchName,
                VersionType = GitVersionType.Branch
            }))
        using (var reader = new StreamReader(stream))
        {
            return await reader.ReadToEndAsync();
        }
    }
    catch (VssServiceException ex) when (ex.Message.Contains("404"))
    {
        return $"❌ Fichier non trouvé: {filePath} dans {projectName}/{repoName} (branche: {branchName})";
    }
    catch (VssServiceException ex) when (ex.Message.Contains("401") || ex.Message.Contains("403"))
    {
        return $"❌ Accès refusé. Vérifiez que votre PAT a les permissions Code: Read pour {projectName}/{repoName}";
    }
    catch (VssServiceException ex)
    {
        return $"❌ Erreur Azure DevOps: {ex.Message}";
    }
    catch (Exception ex)
    {
        return $"❌ Erreur inattendue: {ex.Message}";
    }
}
```

**Effort** : 1 heure
**Test** : Tester avec un fichier inexistant, un PAT invalide, etc.

---

### Priorité MOYENNE 🟡

#### 4. Ajouter un outil de recherche de fichiers

**Utilité** : Permettre à Copilot de chercher des fichiers par nom ou pattern

**Nouveau tool** :

```csharp
[McpServerTool, Description("Search for files by name pattern in a repository.")]
public async Task<List<string>> SearchRepositoryFiles(
    string projectName,
    string repoName,
    string searchPattern,
    string branch = "dev")
{
    var allFiles = await _portimaDevOpsService.ListFilePathsAsync(projectName, repoName, branch);
    
    // Support wildcards
    var regex = new Regex(
        "^" + Regex.Escape(searchPattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
        RegexOptions.IgnoreCase
    );
    
    return allFiles
        .Where(f => regex.IsMatch(Path.GetFileName(f)))
        .ToList();
}
```

**Exemple d'utilisation** :
```
Copilot: "Trouve tous les fichiers de configuration dans sample-api"
→ SearchRepositoryFiles("Portima DevOps", "sample-api", "appsettings*.json")
```

**Effort** : 1 heure

---

#### 5. Ajouter des métadonnées de fichiers

**Utilité** : Donner plus de contexte à Copilot (taille, date de modification, auteur)

**Modification** :

```csharp
public class FileInfo
{
    public string Path { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string LastCommitMessage { get; set; }
}

[McpServerTool, Description("List files with metadata (size, last modified, etc.)")]
public async Task<List<FileInfo>> ListRepositoryFilesWithMetadata(
    string projectName,
    string repoName,
    string branch = "dev")
{
    // Implémentation similaire à ListFilePathsAsync
    // mais retourne FileInfo avec métadonnées
}
```

**Effort** : 2 heures

---

#### 6. Support multi-repositories

**Utilité** : Comparer plusieurs repositories ou chercher dans tous les repos

**Nouveau tool** :

```csharp
[McpServerTool, Description("Search content across multiple repositories in a project.")]
public async Task<Dictionary<string, List<string>>> SearchAcrossRepositories(
    string projectName,
    string[] repositoryNames,
    string searchTerm,
    string branch = "dev")
{
    var results = new Dictionary<string, List<string>>();
    
    foreach (var repoName in repositoryNames)
    {
        var files = await _portimaDevOpsService.ListFilePathsAsync(projectName, repoName, branch);
        var matchingFiles = new List<string>();
        
        foreach (var file in files)
        {
            var content = await _portimaDevOpsService.GetFileFromBranchAsync(projectName, repoName, file, branch);
            if (content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                matchingFiles.Add(file);
            }
        }
        
        if (matchingFiles.Any())
        {
            results[repoName] = matchingFiles;
        }
    }
    
    return results;
}
```

**Effort** : 3 heures

---

### Priorité BASSE 🟢

#### 7. Statistiques et monitoring

**Utilité** : Comprendre l'utilisation du serveur MCP

**Implémentation** :

```csharp
public class McpUsageStats
{
    private static int _totalRequests = 0;
    private static int _cacheHits = 0;
    private static Dictionary<string, int> _toolUsage = new();
    
    public static void RecordRequest(string toolName)
    {
        Interlocked.Increment(ref _totalRequests);
        lock (_toolUsage)
        {
            _toolUsage.TryGetValue(toolName, out var count);
            _toolUsage[toolName] = count + 1;
        }
    }
    
    public static void RecordCacheHit()
    {
        Interlocked.Increment(ref _cacheHits);
    }
}

[McpServerTool, Description("Get MCP server usage statistics")]
public Task<object> GetUsageStats()
{
    return Task.FromResult<object>(new
    {
        TotalRequests = McpUsageStats.TotalRequests,
        CacheHitRate = McpUsageStats.CacheHitRate,
        ToolUsage = McpUsageStats.ToolUsage
    });
}
```

**Effort** : 2 heures

---

#### 8. Configuration dynamique

**Utilité** : Permettre de changer de repository sans redémarrer

**Configuration file** : `~/.portima-mcp/config.json`

```json
{
  "defaultProject": "Portima DevOps",
  "defaultRepo": "sample-api",
  "defaultBranch": "dev",
  "repositories": [
    {
      "alias": "sample",
      "project": "Portima DevOps",
      "repo": "sample-api",
      "branch": "dev"
    },
    {
      "alias": "common",
      "project": "Portima DevOps",
      "repo": "common-lib",
      "branch": "main"
    }
  ]
}
```

**Tool** :

```csharp
[McpServerTool, Description("List available repository aliases")]
public Task<List<string>> ListRepositoryAliases()
{
    // Lire depuis config.json
}

[McpServerTool, Description("Get file from repository using alias")]
public async Task<string> GetFileByAlias(
    string alias,
    string filePath)
{
    // Résoudre alias → project/repo/branch
    // Puis appeler GetFileFromBranchAsync
}
```

**Effort** : 3 heures

---

## 📅 Planning suggéré

### Sprint 1 (Semaine 1)
- [x] Fixer le bug de version de package ✅
- [ ] Amélioration 1 : Types de fichiers étendus 🔴
- [ ] Amélioration 2 : Système de cache 🔴
- [ ] Amélioration 3 : Gestion des erreurs 🔴
- [ ] Tests avec groupe pilote

### Sprint 2 (Semaine 2)
- [ ] Amélioration 4 : Recherche de fichiers 🟡
- [ ] Amélioration 5 : Métadonnées 🟡
- [ ] Documentation des nouveaux outils
- [ ] Collecte feedback groupe pilote

### Sprint 3 (Semaine 3)
- [ ] Amélioration 6 : Multi-repositories 🟡
- [ ] Choix et préparation du mode de déploiement
- [ ] Tests de performance
- [ ] Documentation déploiement

### Sprint 4 (Semaine 4)
- [ ] Améliorations basses priorités (optionnel) 🟢
- [ ] Déploiement beta
- [ ] Formation utilisateurs
- [ ] Support initial

---

## 🧪 Plan de test

### Tests unitaires

```csharp
// PortimaStandardsMcp.Tests/PortimaDevOpsServiceTests.cs

[TestClass]
public class PortimaDevOpsServiceTests
{
    [TestMethod]
    public async Task ListFilePathsAsync_ShouldIncludeJsonFiles()
    {
        // Arrange
        var service = CreateServiceWithMockClient();
        
        // Act
        var files = await service.ListFilePathsAsync("TestProject", "TestRepo", "main");
        
        // Assert
        Assert.IsTrue(files.Any(f => f.EndsWith(".json")));
    }
    
    [TestMethod]
    public async Task GetFileFromBranchAsync_ShouldReturnErrorMessage_WhenFileNotFound()
    {
        // Test que l'erreur 404 retourne un message clair
    }
    
    [TestMethod]
    public async Task Cache_ShouldReturnSameResult_OnSecondCall()
    {
        // Test du cache
    }
}
```

### Tests d'intégration

```csharp
[TestClass]
public class PortimaDevOpsIntegrationTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task RealAzureDevOpsConnection_ShouldWork()
    {
        // Test avec vraie connexion Azure DevOps
        // Nécessite PAT configuré
    }
}
```

### Tests manuels

1. **Test avec Copilot**
   - Demander "Montre-moi le Program.cs de sample-api"
   - Vérifier que le contenu est correct
   - Vérifier la vitesse de réponse

2. **Test de cache**
   - Même question 2x de suite
   - 2ème réponse doit être instantanée

3. **Test d'erreurs**
   - Demander un fichier inexistant
   - Message d'erreur doit être clair

---

## 📊 Métriques de succès

### Performance
- ✅ Temps de réponse < 2s pour un fichier (avec cache)
- ✅ Temps de réponse < 5s pour liste de fichiers
- ✅ Cache hit rate > 70% après 1 semaine d'utilisation

### Qualité
- ✅ 0 erreur non gérée
- ✅ Tous les types de fichiers pertinents inclus
- ✅ Messages d'erreur clairs et actionnables

### Adoption
- ✅ 80% du groupe pilote utilise quotidiennement
- ✅ Score de satisfaction > 4/5
- ✅ Au moins 5 cas d'usage documentés

---

## 🔄 Processus d'amélioration continue

### Collecte de feedback

**Formulaire à envoyer après 2 semaines** :

1. Utilisez-vous le serveur MCP ? (Oui/Non)
2. À quelle fréquence ? (Quotidien/Hebdomadaire/Rare)
3. Cas d'usage principaux ? (Texte libre)
4. Problèmes rencontrés ? (Texte libre)
5. Suggestions d'amélioration ? (Texte libre)
6. Score global : ⭐⭐⭐⭐⭐

### Roadmap future

**V2.0 (3-6 mois)**
- [ ] Support de recherche sémantique (vectorisation)
- [ ] Comparaison automatique avec sample-api
- [ ] Suggestions proactives de refactoring
- [ ] Dashboard d'analytics

**V3.0 (6-12 mois)**
- [ ] Extension VS Code dédiée
- [ ] Support multi-organisations
- [ ] Templates de code générés automatiquement
- [ ] Intégration CI/CD pour validation auto

---

## ✅ Checklist avant déploiement

### Code
- [ ] Toutes les améliorations haute priorité implémentées
- [ ] Tests unitaires passent à 100%
- [ ] Tests d'intégration passent
- [ ] Code review effectué
- [ ] Documentation à jour

### Infrastructure
- [ ] Mode de déploiement choisi
- [ ] Infrastructure provisionnée (si centralisé)
- [ ] Monitoring configuré
- [ ] Backup plan en place

### Documentation
- [ ] README à jour
- [ ] DEPLOYMENT_GUIDE complet
- [ ] FAQ créée
- [ ] Exemples d'utilisation documentés

### Support
- [ ] Canal support créé (Teams/Slack)
- [ ] Process d'escalation défini
- [ ] FAQ publié
- [ ] Formation planifiée

---

## 📞 Prochaines étapes

1. **Aujourd'hui** : Réviser ce plan et prioriser
2. **Cette semaine** : Implémenter améliorations priorité haute
3. **Semaine prochaine** : Tests avec groupe pilote
4. **Dans 2 semaines** : Décision go/no-go pour déploiement complet

Bon développement ! 🚀
