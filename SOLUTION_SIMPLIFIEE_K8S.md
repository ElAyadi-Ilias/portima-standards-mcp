# 🎯 Solution Simplifiée pour Portima - MCP comme API

## 💡 Votre suggestion est excellente !

Vous avez raison : **Si vous déployez déjà sur K8s, traitez le serveur MCP comme une API standard.**

Pas besoin de NuGet Global Tool → Déployez directement comme vos APIs existantes !

---

## ✅ Solution recommandée : MCP Server = API Standard Portima

### Architecture

```
┌────────────────────────────────────────────────────────┐
│         Même template que vos APIs existantes          │
└────────────────────────────────────────────────────────┘

Développeurs (50)                K8s Cluster (AKS)
       │                                │
       │                    ┌───────────────────────┐
       │                    │  portima-mcp-api      │
       │  HTTPS/SSE         │  (3 replicas)         │
       ├───────────────────►│  + Service Account    │
       │                    │  + PAT entreprise     │
       │                    │  + Redis cache        │
       │                    └───────────────────────┘
       │                                │
       │                                │
VS Code/Visual Studio          Azure DevOps API
+ Copilot                      (sample-api, etc.)
+ Config MCP pointant
  vers https://mcp-api.portima.internal
```

**C'est tout !** Une seule API à déployer, pas de NuGet tool intermédiaire.

---

## 🚀 Déploiement en 1 phase uniquement

### Étape 1 : Préparer le code pour HTTP/SSE

Le serveur MCP doit servir en HTTP (Server-Sent Events) au lieu de stdio.

**Modifier `Program.cs` :**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortimaStandardsMcp;

var builder = WebApplication.CreateBuilder(args);

// Configuration depuis variables d'environnement (K8s ConfigMap/Secret)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables(prefix: "PORTIMA_MCP_");

// Logging comme vos APIs
builder.Logging.AddConsole();
builder.Logging.AddAzureWebAppDiagnostics(); // Si vous utilisez Azure Monitor

// MCP Server avec transport SSE (HTTP)
builder.Services
    .AddMcpServer()
    .WithSseServerTransport() // SSE au lieu de stdio
    .WithToolsFromAssembly(typeof(PortimaDevOpsTools).Assembly);

builder.Services.AddSingleton<PortimaDevOpsService>();

// Cache Redis (comme vos autres APIs probablement)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

// Health checks (standard Portima)
builder.Services.AddHealthChecks()
    .AddCheck("mcp_ready", () => HealthCheckResult.Healthy());

var app = builder.Build();

// Endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready"); // Pour K8s readiness probe
app.MapMcpEndpoint("/mcp"); // Endpoint MCP principal

await app.RunAsync();
```

---

### Étape 2 : Réutiliser votre template de déploiement API

Vous avez déjà des templates pour déployer des APIs sur K8s. Utilisez le même !

#### Fichier de configuration template Portima

Si vous utilisez un format type `api-template.yaml` ou Helm, adaptez simplement :

**Exemple avec vos valeurs standards :**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: portima-mcp-api
  namespace: portima-tools  # Ou votre namespace standard
  labels:
    app: portima-mcp-api
    team: devops
    env: production
spec:
  replicas: 3  # HA comme vos APIs
  selector:
    matchLabels:
      app: portima-mcp-api
  template:
    metadata:
      labels:
        app: portima-mcp-api
    spec:
      containers:
      - name: mcp-api
        image: acr.portima.io/apis/mcp-server:1.0.0  # Votre ACR
        ports:
        - containerPort: 8080
          name: http
        env:
        # Config comme vos autres APIs
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
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
              name: portima-redis-secret  # Votre Redis existant
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        # Health checks standards
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: portima-mcp-api
  namespace: portima-tools
spec:
  type: ClusterIP
  ports:
  - port: 80
    targetPort: 8080
    protocol: TCP
  selector:
    app: portima-mcp-api
---
# Ingress comme vos autres APIs
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: portima-mcp-api
  namespace: portima-tools
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod  # Votre config
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - mcp-api.portima.internal
    secretName: portima-mcp-tls
  rules:
  - host: mcp-api.portima.internal
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: portima-mcp-api
            port:
              number: 80
```

---

### Étape 3 : Dockerfile (standard)

Comme vos autres APIs .NET :

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PortimaStandardsMcp/PortimaStandardsMcp.csproj", "PortimaStandardsMcp/"]
RUN dotnet restore "PortimaStandardsMcp/PortimaStandardsMcp.csproj"

COPY . .
WORKDIR "/src/PortimaStandardsMcp"
RUN dotnet build "PortimaStandardsMcp.csproj" -c Release -o /app/build
RUN dotnet publish "PortimaStandardsMcp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Non-root user (best practice)
RUN addgroup --system --gid 1001 mcpuser && \
    adduser --system --uid 1001 --ingroup mcpuser mcpuser
USER mcpuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "PortimaStandardsMcp.dll"]
```

---

### Étape 4 : Configuration développeur (simple)

Chaque développeur configure VS Code **une seule fois** :

**~/.vscode/mcp.json** ou dans settings workspace :

```json
{
  "servers": {
    "portima-standards": {
      "type": "http",
      "url": "https://mcp-api.portima.internal/mcp"
    }
  }
}
```

**C'est tout !** Pas de PAT individuel, pas d'installation, juste une config.

---

## 📦 CI/CD - Réutilisez votre pipeline Azure DevOps

Exemple de pipeline (adaptez à votre template) :

```yaml
# azure-pipelines.yml

trigger:
  branches:
    include:
    - main
    - dev

pool:
  vmImage: 'ubuntu-latest'

variables:
  imageName: 'apis/mcp-server'
  acrName: 'acr.portima.io'

stages:
- stage: Build
  jobs:
  - job: BuildAndPush
    steps:
    - task: Docker@2
      displayName: 'Build Docker Image'
      inputs:
        containerRegistry: 'ACR-Portima'
        repository: $(imageName)
        command: 'build'
        Dockerfile: '**/Dockerfile'
        tags: |
          $(Build.BuildId)
          latest
    
    - task: Docker@2
      displayName: 'Push to ACR'
      inputs:
        containerRegistry: 'ACR-Portima'
        repository: $(imageName)
        command: 'push'
        tags: |
          $(Build.BuildId)
          latest

- stage: Deploy
  dependsOn: Build
  jobs:
  - deployment: DeployToAKS
    environment: 'Production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: KubernetesManifest@0
            displayName: 'Deploy to AKS'
            inputs:
              action: 'deploy'
              kubernetesServiceConnection: 'AKS-Portima'
              namespace: 'portima-tools'
              manifests: |
                k8s/deployment.yaml
                k8s/service.yaml
                k8s/ingress.yaml
              containers: |
                $(acrName)/$(imageName):$(Build.BuildId)
```

---

## 🎯 Avantages de cette approche simplifiée

### ✅ Cohérence totale avec vos pratiques

| Aspect | Vos APIs actuelles | MCP Server |
|--------|-------------------|------------|
| **Déploiement** | K8s via template | ✅ Même template |
| **CI/CD** | Azure Pipelines | ✅ Même pipeline |
| **Registry** | ACR Portima | ✅ Même ACR |
| **Networking** | Ingress nginx | ✅ Même Ingress |
| **Monitoring** | Prometheus/Grafana | ✅ Même stack |
| **Logs** | Azure Monitor | ✅ Même logging |
| **Cache** | Redis | ✅ Même Redis |
| **Secrets** | K8s Secrets | ✅ Même gestion |

### ✅ Simplicité maximale

**Avant (approche 2 phases) :**
1. Développer NuGet tool
2. Publier sur feed privé
3. Chaque dev installe
4. Puis migrer vers K8s plus tard

**Maintenant (approche directe) :**
1. Déployer API sur K8s (comme d'habitude)
2. Devs configurent URL (1 ligne JSON)
3. C'est tout !

### ✅ Maintenance réduite

- **1 seul déploiement** au lieu de 2 (NuGet + K8s)
- **1 seul pipeline** CI/CD
- **0 gestion de versions NuGet**
- Mises à jour instantanées pour tous les 50 devs

---

## 📅 Planning simplifié

### Semaine 1 : Développement

**Jour 1-2 :**
- [ ] Modifier `Program.cs` pour HTTP/SSE
- [ ] Créer `appsettings.json` pour config
- [ ] Tester localement

**Jour 3-4 :**
- [ ] Créer Dockerfile
- [ ] Adapter votre template K8s existant
- [ ] Créer pipeline Azure DevOps

**Jour 5 :**
- [ ] Build et push image vers ACR
- [ ] Test end-to-end local

### Semaine 2 : Déploiement

**Jour 1-2 :**
- [ ] Déployer sur AKS (env staging/dev)
- [ ] Créer service account PAT Azure DevOps
- [ ] Configurer secrets K8s
- [ ] Tester avec 2-3 devs pilotes

**Jour 3 :**
- [ ] Déploiement production
- [ ] Vérifier HA (3 replicas)
- [ ] Tests de charge

**Jour 4-5 :**
- [ ] Documentation pour les devs
- [ ] Rollout config VS Code aux 50 devs
- [ ] Support initial

**Total : 2 semaines** pour production-ready ✅

---

## 🔒 Sécurité (standard Portima)

### Service Account PAT

1. Créer `svc-mcp-reader@portima.com` dans Azure DevOps
2. Permission **Code: Read** uniquement
3. PAT avec expiration 1 an
4. Stocker dans K8s Secret :

```bash
kubectl create secret generic portima-mcp-secret \
  --from-literal=ado-pat='VOTRE_PAT_SERVICE_ACCOUNT' \
  -n portima-tools
```

### Network Policy

Si vous utilisez Network Policies :

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: portima-mcp-netpol
  namespace: portima-tools
spec:
  podSelector:
    matchLabels:
      app: portima-mcp-api
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
  egress:
  - to: # Azure DevOps
    ports:
    - protocol: TCP
      port: 443
  - to: # Redis
    - podSelector:
        matchLabels:
          app: redis
```

---

## 📊 Monitoring (comme vos APIs)

### Health Checks

Déjà inclus dans le code :
- `/health` → Liveness probe
- `/ready` → Readiness probe

### Métriques Prometheus

Si vous exposez déjà des métriques Prometheus sur vos APIs, ajoutez :

```csharp
// Dans Program.cs
builder.Services.AddPrometheusMetrics();

app.UseHttpMetrics(); // Prometheus middleware
app.MapMetrics(); // Endpoint /metrics
```

### Dashboard Grafana

Créez un dashboard (ou réutilisez un template API existant) avec :
- Requests per second
- Latency (p50, p95, p99)
- Error rate
- Cache hit rate (Redis)
- Pod health

---

## 💰 Coûts

**Infrastructure :**
- AKS : Déjà existant → **0€**
- Pods (3x256Mi) : Négligeable sur cluster existant → **~0€**
- Redis : Si partagé avec autres apps → **0€**
- ACR : Stockage image → **<1€/mois**

**Total : Essentiellement gratuit** (réutilise infra existante)

---

## 📝 Checklist de déploiement

### Développement
- [ ] Modifier `Program.cs` pour HTTP/SSE
- [ ] Ajouter health checks (`/health`, `/ready`)
- [ ] Configuration via variables d'environnement
- [ ] Dockerfile créé
- [ ] Tests locaux

### CI/CD
- [ ] Pipeline Azure DevOps créé (réutiliser template)
- [ ] Build Docker image
- [ ] Push vers ACR Portima
- [ ] Deploy vers AKS

### Infrastructure K8s
- [ ] Namespace créé (`portima-tools` ou autre)
- [ ] Secret pour PAT Azure DevOps
- [ ] Deployment (3 replicas)
- [ ] Service (ClusterIP)
- [ ] Ingress (nginx + TLS)
- [ ] Network Policy (si applicable)

### Configuration développeurs
- [ ] Documentation config VS Code
- [ ] URL endpoint : `https://mcp-api.portima.internal/mcp`
- [ ] Rollout aux 50 devs

### Monitoring
- [ ] Prometheus metrics activées
- [ ] Dashboard Grafana créé
- [ ] Alertes configurées (si nécessaire)
- [ ] Logs centralisés (Azure Monitor)

---

## 🎉 Résultat final

**Pour les développeurs :**
```json
// ~/.vscode/mcp.json (1 seule fois)
{
  "servers": {
    "portima-standards": {
      "type": "http",
      "url": "https://mcp-api.portima.internal/mcp"
    }
  }
}
```

**Pour l'équipe DevOps :**
- 1 API de plus sur K8s (comme les autres)
- Même templates, même pipelines
- Monitoring et logs standards
- Maintenance minimale

**Délai : 2 semaines** de dev + déploiement pour production-ready

---

## ✅ Conclusion

Votre suggestion est **parfaitement correcte** :

❌ ~~Pourquoi faire un NuGet si on déploie sur K8s ?~~  
✅ **Déployez directement comme une API standard !**

**Avantages :**
- Réutilise 100% vos templates existants
- Pattern familier pour toute l'équipe
- Moins de complexité
- Même stack monitoring/logging
- Déploiement plus rapide

**Cette approche est plus simple et mieux alignée avec vos pratiques actuelles.** 🚀

---

## 📞 Prochaines étapes

1. Modifier `Program.cs` (voir code ci-dessus)
2. Créer Dockerfile
3. Adapter votre template K8s API
4. Déployer !

Besoin d'aide sur une partie spécifique ? N'hésitez pas !
