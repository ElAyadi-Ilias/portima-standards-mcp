# 🎯 Recommandation Spécifique pour Portima

## 📋 Contexte de votre infrastructure

Basé sur les informations fournies :

- **👥 Taille équipe** : ~50 développeurs
- **🏗️ Type d'applications** : Principalement des APIs
- **☸️ Infrastructure** : Kubernetes (K8s) sur Azure
- **📦 Distribution interne** : NuGet privé avec templates de déploiement
- **🔧 Bibliothèques** : Librairies propriétaires Portima déployées en NuGet

---

## ✅ Solution OPTIMALE pour Portima

### Option recommandée : **NuGet Global Tool + Azure Container**

Combinaison hybride qui tire parti de votre infrastructure existante :

#### 🎯 Pourquoi cette solution ?

✅ **Vous avez déjà NuGet privé** → Réutilisez cette infrastructure  
✅ **Vous avez K8s** → Déployez un serveur centralisé facilement  
✅ **~50 développeurs** → Taille critique pour justifier du centralisé  
✅ **APIs sur K8s** → Pattern familier pour votre équipe  
✅ **Templates de déploiement** → Créez un template MCP réutilisable  

---

## 🏗️ Architecture recommandée

```
┌─────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE PORTIMA                     │
└─────────────────────────────────────────────────────────────┘

┌──────────────────┐         ┌──────────────────────────────┐
│  Développeurs    │         │   K8s Cluster (Azure AKS)    │
│  (~50 devs)      │────────▶│                              │
│                  │   HTTPS │   ┌──────────────────────┐   │
│  VS Code/Studio  │         │   │  MCP Server Pod      │   │
│  + Copilot       │         │   │  (3 replicas)        │   │
└──────────────────┘         │   │  + Service Account   │   │
                             │   │  + PAT d'entreprise  │   │
        ▲                    │   └──────────────────────┘   │
        │                    │                              │
        │ Config auto        │   ┌──────────────────────┐   │
        │                    │   │  Shared Cache        │   │
        │                    │   │  (Redis)             │   │
┌───────┴────────┐           │   └──────────────────────┘   │
│  NuGet Feed    │           └──────────────────────────────┘
│  (Portima)     │                        │
│                │                        │ API Calls
│  Portima.*.Mcp │                        ▼
│  CLI Tool      │           ┌──────────────────────────────┐
└────────────────┘           │   Azure DevOps               │
                             │   - sample-api               │
                             │   - Autres repos standards   │
                             └──────────────────────────────┘
```

---

## 📦 Phase 1 : Distribution via NuGet (Court terme)

### Pourquoi commencer par NuGet ?

Pour les ~50 développeurs, installer l'outil local d'abord permet :
- ✅ Validation du concept rapidement
- ✅ Feedback avant déploiement K8s
- ✅ Utilisation de votre infrastructure NuGet existante

### Setup NuGet Global Tool

#### 1. Modifier le .csproj pour NuGet Tool

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Package NuGet Global Tool -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>portima-mcp</ToolCommandName>
    <PackageId>Portima.Standards.Mcp</PackageId>
    <Version>1.0.0</Version>
    <Authors>Portima DevOps Team</Authors>
    <Company>Portima</Company>
    <Description>MCP Server pour accéder aux standards Portima (sample-api) depuis GitHub Copilot</Description>
    <PackageOutputPath>./nupkg</PackageOutputPath>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <!-- ... reste inchangé ... -->

  <ItemGroup>
    <None Include="../README.md" Pack="true" PackagePath="/" />
  </ItemGroup>

</Project>
```

#### 2. Modifier Program.cs pour configuration flexible

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PortimaStandardsMcp;

var builder = Host.CreateApplicationBuilder(args);

// Configuration multi-sources (ordre de priorité)
var configPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".portima-mcp",
    "appsettings.json"
);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile(configPath, optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "PORTIMA_MCP_"); // Pour K8s

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(PortimaDevOpsTools).Assembly);

builder.Services.AddSingleton<PortimaDevOpsService>();

// Optionnel : Cache partagé si configuré
if (!string.IsNullOrEmpty(builder.Configuration["Redis:ConnectionString"]))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["Redis:ConnectionString"];
    });
}
else
{
    builder.Services.AddMemoryCache();
}

await builder.Build().RunAsync();
```

#### 3. Publier sur votre NuGet privé

```bash
# Build et pack
dotnet pack -c Release

# Push vers votre feed NuGet Portima
dotnet nuget push ./PortimaStandardsMcp/nupkg/Portima.Standards.Mcp.1.0.0.nupkg \
  --source "https://pkgs.dev.azure.com/tfsportima/_packaging/PortimaPackages/nuget/v3/index.json" \
  --api-key az
```

#### 4. Installation développeur (1 commande)

```bash
# Installation
dotnet tool install -g Portima.Standards.Mcp

# Configuration (PAT personnel)
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

**Installation VS Code** : Une seule ligne dans `~/.vscode/mcp.json`

```json
{
  "servers": {
    "portima-standards": {
      "type": "stdio",
      "command": "portima-mcp"
    }
  }
}
```

---

## ☸️ Phase 2 : Déploiement K8s centralisé (Moyen terme)

### Pourquoi passer à K8s ?

Pour 50 développeurs :
- ✅ **Un seul PAT** (service account) au lieu de 50 PATs personnels
- ✅ **Cache partagé** (Redis) → Meilleure performance
- ✅ **Haute disponibilité** (3 replicas)
- ✅ **Monitoring centralisé** (Prometheus/Grafana)
- ✅ **Mises à jour instantanées** pour tous

### Architecture Kubernetes

#### 1. Créer un Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PortimaStandardsMcp/PortimaStandardsMcp.csproj", "PortimaStandardsMcp/"]
RUN dotnet restore "PortimaStandardsMcp/PortimaStandardsMcp.csproj"

COPY . .
WORKDIR "/src/PortimaStandardsMcp"
RUN dotnet build "PortimaStandardsMcp.csproj" -c Release -o /app/build
RUN dotnet publish "PortimaStandardsMcp.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "PortimaStandardsMcp.dll"]
```

#### 2. Modifier pour HTTP (MCP over SSE)

Créer `HttpProgram.cs` :

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PortimaStandardsMcp;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables(prefix: "PORTIMA_MCP_");

builder.Services
    .AddMcpServer()
    .WithSseServerTransport() // Server-Sent Events pour HTTP
    .WithToolsFromAssembly(typeof(PortimaDevOpsTools).Assembly);

builder.Services.AddSingleton<PortimaDevOpsService>();

// Cache Redis partagé
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapMcpEndpoint("/mcp");

await app.RunAsync();
```

#### 3. Manifests Kubernetes

**Namespace** : `portima-mcp-namespace.yaml`

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: portima-mcp
  labels:
    name: portima-mcp
    environment: production
```

**Secret** : `portima-mcp-secret.yaml`

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: portima-mcp-secret
  namespace: portima-mcp
type: Opaque
stringData:
  ado-pat: "VOTRE_SERVICE_ACCOUNT_PAT_ICI"
  redis-connection: "portima-redis:6379,password=REDIS_PASSWORD"
```

**ConfigMap** : `portima-mcp-config.yaml`

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: portima-mcp-config
  namespace: portima-mcp
data:
  appsettings.json: |
    {
      "AzureDevOps": {
        "OrganizationUrl": "https://dev.azure.com/tfsportima"
      },
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      }
    }
```

**Deployment** : `portima-mcp-deployment.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: portima-mcp
  namespace: portima-mcp
  labels:
    app: portima-mcp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: portima-mcp
  template:
    metadata:
      labels:
        app: portima-mcp
    spec:
      containers:
      - name: mcp-server
        image: acr.azurecr.io/portima/mcp-server:1.0.0
        ports:
        - containerPort: 8080
          name: http
        env:
        - name: PORTIMA_MCP_AzureDevOps__OrganizationUrl
          value: "https://dev.azure.com/tfsportima"
        - name: PORTIMA_MCP_AzureDevOps__PersonalAccessToken
          valueFrom:
            secretKeyRef:
              name: portima-mcp-secret
              key: ado-pat
        - name: PORTIMA_MCP_Redis__ConnectionString
          valueFrom:
            secretKeyRef:
              name: portima-mcp-secret
              key: redis-connection
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
```

**Service** : `portima-mcp-service.yaml`

```yaml
apiVersion: v1
kind: Service
metadata:
  name: portima-mcp-service
  namespace: portima-mcp
spec:
  type: ClusterIP
  ports:
  - port: 80
    targetPort: 8080
    protocol: TCP
    name: http
  selector:
    app: portima-mcp
```

**Ingress** : `portima-mcp-ingress.yaml`

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: portima-mcp-ingress
  namespace: portima-mcp
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - mcp.portima.internal
    secretName: portima-mcp-tls
  rules:
  - host: mcp.portima.internal
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: portima-mcp-service
            port:
              number: 80
```

#### 4. Redis pour cache partagé

**Redis Deployment** : `redis-deployment.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: portima-redis
  namespace: portima-mcp
spec:
  replicas: 1
  selector:
    matchLabels:
      app: portima-redis
  template:
    metadata:
      labels:
        app: portima-redis
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        ports:
        - containerPort: 6379
        resources:
          requests:
            memory: "128Mi"
            cpu: "50m"
          limits:
            memory: "256Mi"
            cpu: "100m"
---
apiVersion: v1
kind: Service
metadata:
  name: portima-redis
  namespace: portima-mcp
spec:
  ports:
  - port: 6379
    targetPort: 6379
  selector:
    app: portima-redis
```

#### 5. Déploiement

```bash
# Build et push image
docker build -t acr.azurecr.io/portima/mcp-server:1.0.0 .
docker push acr.azurecr.io/portima/mcp-server:1.0.0

# Déployer sur K8s
kubectl apply -f portima-mcp-namespace.yaml
kubectl apply -f portima-mcp-secret.yaml
kubectl apply -f portima-mcp-config.yaml
kubectl apply -f redis-deployment.yaml
kubectl apply -f portima-mcp-deployment.yaml
kubectl apply -f portima-mcp-service.yaml
kubectl apply -f portima-mcp-ingress.yaml

# Vérifier
kubectl get pods -n portima-mcp
kubectl logs -n portima-mcp -l app=portima-mcp
```

#### 6. Configuration développeur (HTTP)

```json
{
  "servers": {
    "portima-standards": {
      "type": "http",
      "url": "https://mcp.portima.internal/mcp"
    }
  }
}
```

---

## 🎯 Utilisation des Templates de Déploiement Portima

Vous avez déjà des templates de déploiement ! Réutilisez-les :

### 1. Créer un template Helm Chart

Structure suggérée :

```
portima-mcp-chart/
├── Chart.yaml
├── values.yaml
├── templates/
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── ingress.yaml
│   ├── secret.yaml
│   └── configmap.yaml
└── README.md
```

**values.yaml** :

```yaml
replicaCount: 3

image:
  repository: acr.azurecr.io/portima/mcp-server
  tag: "1.0.0"
  pullPolicy: IfNotPresent

service:
  type: ClusterIP
  port: 80
  targetPort: 8080

ingress:
  enabled: true
  className: nginx
  host: mcp.portima.internal
  tls:
    enabled: true

azureDevOps:
  organizationUrl: "https://dev.azure.com/tfsportima"
  # PAT in secret

redis:
  enabled: true
  # Ou utiliser Redis existant

resources:
  requests:
    memory: "256Mi"
    cpu: "100m"
  limits:
    memory: "512Mi"
    cpu: "500m"

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 5
  targetCPUUtilizationPercentage: 80
```

### 2. Déployer avec Helm

```bash
helm install portima-mcp ./portima-mcp-chart -n portima-mcp
```

---

## 📊 Avantages pour Portima

### ✅ Cohérence avec votre stack

| Aspect | Portima actuel | MCP Server |
|--------|----------------|------------|
| **Distribution** | NuGet privé | ✅ NuGet global tool |
| **Déploiement** | K8s templates | ✅ Helm chart K8s |
| **Infra** | Azure AKS | ✅ AKS compatible |
| **CI/CD** | Azure Pipelines | ✅ Réutilisable |
| **Monitoring** | Prometheus/Grafana | ✅ Metrics exposées |
| **Logs** | Azure Monitor | ✅ Structured logging |

### ✅ Bénéfices pour les 50 développeurs

- **Installation** : 1 commande (`dotnet tool install`)
- **Configuration** : Automatique via serveur centralisé
- **Performance** : Cache Redis partagé
- **Standards** : Accès instantané à sample-api
- **Cohérence** : Même code pour tous (via K8s)

---

## 📅 Planning de déploiement

### Semaine 1-2 : NuGet Tool (Quick Win)

- [ ] Modifier .csproj pour global tool
- [ ] Modifier Program.cs pour config flexible
- [ ] Publier v1.0.0 sur NuGet Portima
- [ ] Tester avec 5 développeurs pilotes
- [ ] Documenter installation

**Résultat** : 50 devs peuvent installer en 2 minutes

### Semaine 3-4 : K8s Deployment

- [ ] Créer Dockerfile
- [ ] Créer manifests K8s (ou Helm chart)
- [ ] Déployer Redis
- [ ] Déployer MCP server (3 replicas)
- [ ] Configurer Ingress + TLS
- [ ] Tester load balancing

**Résultat** : Infrastructure centralisée haute dispo

### Semaine 5 : Rollout complet

- [ ] Migration config développeurs (local → centralisé)
- [ ] Session de formation
- [ ] Documentation finale
- [ ] Monitoring et alertes

**Résultat** : 50 devs sur serveur centralisé

---

## 💰 Coûts estimés

### NuGet Tool (Phase 1)

- **Infrastructure** : 0€ (réutilise NuGet existant)
- **Effort** : 2-3 jours

### K8s Centralisé (Phase 2)

- **AKS** : Déjà existant → 0€ additionnel
- **Pods MCP** : ~256Mi × 3 replicas = ~768Mi → Négligeable sur cluster existant
- **Redis** : ~128Mi → Négligeable
- **Effort** : 3-5 jours

**Total** : Essentiellement du temps de développement, pas de coûts infra additionnels

---

## 🔒 Sécurité

### Service Account PAT

Créer un compte de service Azure DevOps :

1. Créer utilisateur `svc-mcp-reader@portima.com`
2. Assigner uniquement permission **Code: Read**
3. Créer PAT avec expiration 1 an (max)
4. Stocker dans Azure Key Vault
5. Référencer dans K8s secret

### Network Policies

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: portima-mcp-netpol
  namespace: portima-mcp
spec:
  podSelector:
    matchLabels:
      app: portima-mcp
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
  egress:
  - to:
    - podSelector:
        matchLabels:
          app: portima-redis
  - to: # Azure DevOps
    - namespaceSelector: {}
    ports:
    - protocol: TCP
      port: 443
```

---

## 📈 Monitoring

### Prometheus Metrics

Ajouter dans le code :

```csharp
// Métriques custom
builder.Services.AddSingleton<IMcpMetrics, McpMetrics>();

public class McpMetrics
{
    private readonly Counter _requestsTotal;
    private readonly Histogram _requestDuration;
    private readonly Gauge _cacheHitRate;
    
    public void RecordRequest(string tool, double duration)
    {
        _requestsTotal.Inc();
        _requestDuration.Observe(duration);
    }
}
```

### Grafana Dashboard

Métriques clés :
- Requests par seconde
- Latence moyenne/p95/p99
- Cache hit rate
- Erreurs Azure DevOps
- Pods health

---

## ✅ Checklist finale

### Phase 1 : NuGet (Cette semaine)

- [ ] Modifier .csproj pour PackAsTool
- [ ] Modifier Program.cs pour config multi-sources
- [ ] Build et test local
- [ ] Publier sur NuGet Portima
- [ ] Documenter installation
- [ ] Tester avec 5 devs pilotes
- [ ] Rollout aux 50 devs

### Phase 2 : K8s (Semaine prochaine)

- [ ] Créer Dockerfile
- [ ] Créer HttpProgram.cs
- [ ] Créer manifests K8s
- [ ] Setup Redis
- [ ] Déployer sur AKS
- [ ] Configurer Ingress/TLS
- [ ] Setup monitoring
- [ ] Migrer développeurs vers serveur centralisé

---

## 🎉 Résultat attendu

**Aujourd'hui** :
- Installation manuelle compliquée
- 50 PATs à gérer
- Pas de cache
- Mises à jour difficiles

**Après Phase 1 (NuGet)** :
```bash
dotnet tool install -g Portima.Standards.Mcp
# C'est tout ! 2 minutes par dev
```

**Après Phase 2 (K8s)** :
- Serveur centralisé HA (3 replicas)
- 1 seul PAT (service account)
- Cache Redis partagé
- Monitoring Grafana
- Mises à jour instantanées
- Pattern familier (vos APIs existantes)

---

## 📞 Prochaines actions recommandées

1. **Cette semaine** : Implémenter Phase 1 (NuGet global tool)
2. **Semaine prochaine** : Déployer Phase 2 (K8s)
3. **Dans 2 semaines** : Formation et rollout complet

**Effort total estimé** : 1-2 semaines pour solution production-ready

Votre infrastructure existante (NuGet + K8s + Azure) rend ce projet parfaitement aligné avec votre stack technique. C'est une opportunité idéale ! 🚀
