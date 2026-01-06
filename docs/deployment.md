// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Deployment Guide

Instructions for deploying dotnet-workflow-engine to various environments.

## Local Development

### Prerequisites
- .NET 10 SDK
- SQL Server LocalDB or SQLite
- Optional: Redis

### Setup

1. Clone the repository:
```bash
git clone https://github.com/Sarmkadan/dotnet-workflow-engine.git
cd dotnet-workflow-engine
```

2. Restore packages:
```bash
dotnet restore
```

3. Configure appsettings.Development.json:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=workflow.db"
  },
  "WorkflowEngine": {
    "CacheProvider": "InMemory"
  }
}
```

4. Run migrations:
```bash
dotnet ef database update
```

5. Start the application:
```bash
dotnet run
```

Visit http://localhost:5000/swagger for Swagger UI.

## Docker Deployment

### Using Docker Compose

1. Create `docker-compose.yml` (included in repo):
```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=WorkflowDb;...
    depends_on:
      - db
      - redis
  
  db:
    image: mcr.microsoft.com/mssql/server:latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
    volumes:
      - sql_data:/var/opt/mssql/data
  
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  sql_data:
```

2. Build and run:
```bash
docker-compose up -d
```

3. Access the application:
```
http://localhost:5000
```

### Using Pre-built Image

```bash
docker pull sarmkadan/dotnet-workflow-engine:latest
docker run -p 5000:80 \
  -e "ConnectionStrings__DefaultConnection=..." \
  sarmkadan/dotnet-workflow-engine:latest
```

## Kubernetes Deployment

### Create ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: workflow-engine-config
data:
  appsettings.json: |
    {
      "WorkflowEngine": {
        "DefaultExecutionMode": "Parallel",
        "MaxConcurrentActivities": 20
      }
    }
```

### Create Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: workflow-engine
spec:
  replicas: 3
  selector:
    matchLabels:
      app: workflow-engine
  template:
    metadata:
      labels:
        app: workflow-engine
    spec:
      containers:
      - name: api
        image: sarmkadan/dotnet-workflow-engine:latest
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        - name: WorkflowEngine__JwtSecret
          valueFrom:
            secretKeyRef:
              name: jwt-secret
              key: secret
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
```

### Create Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: workflow-engine-service
spec:
  type: LoadBalancer
  ports:
  - protocol: TCP
    port: 80
    targetPort: 80
  selector:
    app: workflow-engine
```

### Deploy to Kubernetes

```bash
# Create secrets
kubectl create secret generic db-secret --from-literal=connection-string="..."
kubectl create secret generic jwt-secret --from-literal=secret="..."

# Apply manifests
kubectl apply -f configmap.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml

# Check deployment
kubectl get pods
kubectl logs -f deployment/workflow-engine
```

## Azure App Service Deployment

### Prerequisites
- Azure subscription
- Azure CLI

### Deploy

1. Create App Service:
```bash
az appservice plan create \
  --name workflow-plan \
  --resource-group myResourceGroup \
  --sku B2 --is-linux

az webapp create \
  --resource-group myResourceGroup \
  --plan workflow-plan \
  --name workflow-engine-app \
  --runtime "DOTNET|10.0"
```

2. Configure Application Settings:
```bash
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name workflow-engine-app \
  --settings \
    "ConnectionStrings__DefaultConnection=..." \
    "WorkflowEngine__JwtSecret=..." \
    "WEBSITES_PORT=80"
```

3. Deploy code:
```bash
# Option A: From local Git
git remote add azure <git-clone-url>
git push azure main

# Option B: From GitHub
# Configure GitHub Actions in Azure Portal
```

## AWS Elastic Beanstalk Deployment

### Prerequisites
- AWS account
- AWS CLI

### Deploy

1. Install EB CLI:
```bash
pip install awsebcli
```

2. Initialize Elastic Beanstalk:
```bash
eb init -p "dotnet 10" workflow-engine
```

3. Create environment:
```bash
eb create production \
  --instance-type t3.medium \
  --scale 3
```

4. Configure environment variables:
```bash
eb setenv \
  ConnectionStrings__DefaultConnection="..." \
  WorkflowEngine__JwtSecret="..."
```

5. Deploy:
```bash
eb deploy
```

## Self-Hosted Deployment

### Linux Server Setup

1. Install .NET 10:
```bash
# Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version 10.0
```

2. Create systemd service:
```ini
[Unit]
Description=DotNet Workflow Engine
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/opt/workflow-engine
Environment="ASPNETCORE_ENVIRONMENT=Production"
ExecStart=/usr/local/bin/dotnet /opt/workflow-engine/DotNetWorkflowEngine.dll
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

3. Start service:
```bash
sudo systemctl daemon-reload
sudo systemctl start workflow-engine
sudo systemctl enable workflow-engine
```

### Windows Server Setup

1. Install .NET 10 Runtime
2. Create IIS application pool
3. Configure Application:
```xml
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" ... />
    </handlers>
    <aspNetCore 
      processPath="C:\Program Files\dotnet\dotnet.exe"
      arguments="DotNetWorkflowEngine.dll" 
      stdoutLogEnabled="true" 
      stdoutLogFile=".\logs\stdout"
    />
  </system.webServer>
</configuration>
```

## Database Migration

### Production Database Setup

1. Create database:
```sql
CREATE DATABASE WorkflowEngine;
```

2. Run migrations:
```bash
dotnet ef database update --configuration Release
```

3. Create indexes:
```sql
CREATE INDEX idx_workflow_status ON Workflows(Status);
CREATE INDEX idx_instance_workflow ON WorkflowInstances(WorkflowId);
CREATE INDEX idx_audit_instance ON AuditLogEntries(InstanceId);
```

4. Backup database:
```sql
BACKUP DATABASE WorkflowEngine 
TO DISK = 'C:\Backups\WorkflowEngine.bak';
```

## Performance Tuning

### Configuration for Production

```json
{
  "WorkflowEngine": {
    "DefaultExecutionMode": "Parallel",
    "MaxConcurrentActivities": 20,
    "EnableMetrics": true
  },
  "Caching": {
    "Provider": "Redis",
    "DefaultExpiration": "06:00:00"
  },
  "Database": {
    "MaxPoolSize": 50,
    "CommandTimeout": 300
  }
}
```

### Load Balancing

Use load balancer (nginx, HAProxy) to distribute traffic:

```nginx
upstream workflow_backend {
    server app1:5000;
    server app2:5000;
    server app3:5000;
}

server {
    listen 80;
    server_name api.example.com;

    location / {
        proxy_pass http://workflow_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## Monitoring & Logging

### Enable Structured Logging

```csharp
builder.Logging.AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("logs/workflow-engine.log",
        rollingInterval: RollingInterval.Day)
    .WriteTo.Console()
    .CreateLogger());
```

### Application Insights Integration

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Health Checks

Configure health check endpoint:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## Backup and Recovery

### Automated Backups

```bash
# SQL Server backup script
sqlcmd -S server -U sa -P password -Q "BACKUP DATABASE WorkflowEngine TO DISK = 'backup.bak'"
```

### Point-in-Time Recovery

```sql
-- Restore from backup
RESTORE DATABASE WorkflowEngine 
FROM DISK = 'backup.bak'
WITH RECOVERY;
```

### Disaster Recovery Plan

1. Daily automated database backups
2. Test restore procedures monthly
3. Maintain backup offsite
4. Document recovery procedures
5. Keep application code versioned in Git

## Certificate & SSL/TLS

### Self-Signed Certificate

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Production Certificate

Use Let's Encrypt with Certbot (if using nginx):

```bash
sudo certbot certonly --nginx -d api.example.com
```

Configure in appsettings:

```json
"Kestrel": {
  "Endpoints": {
    "Https": {
      "Url": "https://0.0.0.0:443",
      "Certificate": {
        "Path": "/path/to/certificate.pfx",
        "Password": "..."
      }
    }
  }
}
```

## Checklist

Before production deployment:

- [ ] Database configured and migrated
- [ ] SSL/TLS certificate installed
- [ ] Environment variables configured
- [ ] Backups configured and tested
- [ ] Monitoring and logging enabled
- [ ] Load balancer configured
- [ ] Health checks working
- [ ] Rate limiting configured
- [ ] Authorization enabled
- [ ] Firewall rules configured
- [ ] Documentation updated
- [ ] Security scan completed
