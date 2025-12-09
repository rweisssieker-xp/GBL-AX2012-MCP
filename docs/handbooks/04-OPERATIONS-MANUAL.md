---
title: GBL-AX2012-MCP Operations Manual
description: Deployment, monitoring, and troubleshooting guide for operations teams
author: Paige (Technical Writer)
date: 2025-12-07
version: 1.5.0
---

# GBL-AX2012-MCP Operations Manual

## Overview

This manual covers deployment, monitoring, maintenance, and troubleshooting for the GBL-AX2012-MCP server in production environments.

---

## Deployment

### Prerequisites

| Component | Requirement |
|-----------|-------------|
| Operating System | Windows Server 2019+ |
| Runtime | .NET 8.0 Runtime |
| Network | Access to AX 2012 R3 AOS |
| Authentication | Windows AD domain membership |
| Ports | 8080 (HTTP), 9090 (Metrics) |

### Deployment Options

#### Option 1: Windows Service

```powershell
# 1. Publish the application
dotnet publish -c Release -o C:\Services\GBL-AX2012-MCP

# 2. Create Windows Service
New-Service -Name "GBL-AX2012-MCP" `
  -BinaryPathName "C:\Services\GBL-AX2012-MCP\GBL.AX2012.MCP.Server.exe" `
  -DisplayName "GBL AX2012 MCP Server" `
  -StartupType Automatic `
  -Description "MCP Server for AX 2012 R3 integration"

# 3. Configure service account
sc.exe config GBL-AX2012-MCP obj= "DOMAIN\svc-mcp" password= "password"

# 4. Start service
Start-Service GBL-AX2012-MCP
```

### Configuration

#### Production Configuration

Create `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "GBL.AX2012.MCP": "Information"
    }
  },
  "McpServer": {
    "Transport": "stdio",
    "ServerName": "gbl-ax2012-mcp",
    "ServerVersion": "1.4.0"
  },
  "HttpTransport": {
    "Port": 8080,
    "Enabled": true,
    "AllowedOrigins": ["https://n8n.company.com"]
  },
  "AifClient": {
    "BaseUrl": "http://ax-prod-aos:8101/DynamicsAx/Services",
    "Timeout": "00:00:30",
    "Company": "PROD"
  },
  "WcfClient": {
    "BaseUrl": "http://ax-prod-aos:8101/DynamicsAx/Services/GblSalesOrderService",
    "Timeout": "00:00:30",
    "ServiceAccountUser": "svc-mcp",
    "ServiceAccountDomain": "COMPANY"
  },
  "BusinessConnector": {
    "ObjectServer": "ax-prod-aos:2712",
    "Company": "PROD",
    "Language": "en-us"
  },
  "RateLimiter": {
    "RequestsPerMinute": 100,
    "BurstSize": 20
  },
  "CircuitBreaker": {
    "FailureThreshold": 3,
    "OpenDuration": "00:01:00"
  },
  "HealthMonitor": {
    "CheckIntervalSeconds": 30,
    "ErrorRateThresholdPercent": 5,
    "Enabled": true
  },
  "Notifications": {
    "Enabled": true,
    "SlackWebhookUrl": "https://hooks.slack.com/services/xxx",
    "TeamsWebhookUrl": "https://company.webhook.office.com/xxx",
    "DefaultSlackChannel": "#mcp-alerts"
  },
  "Security": {
    "RequireAuthentication": true,
    "AllowedRoles": ["MCP_Read", "MCP_Write", "MCP_Admin"],
    "ApprovalThreshold": 50000
  }
}
```

#### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Production |
| `AifClient__BaseUrl` | AIF service URL | - |
| `AifClient__Company` | AX company | DAT |
| `WcfClient__BaseUrl` | WCF service URL | - |
| `Notifications__SlackWebhookUrl` | Slack webhook | - |
| `Notifications__TeamsWebhookUrl` | Teams webhook | - |

---

## Monitoring

### Health Endpoints

#### Basic Health Check

```http
GET http://localhost:8080/health
```

```json
{
  "status": "healthy",
  "timestamp": "2024-12-06T10:30:00Z",
  "uptime": "5d 12h 30m",
  "components": {
    "aif": "connected",
    "wcf": "connected",
    "businessConnector": "connected"
  }
}
```

#### Detailed Health Check

```http
GET http://localhost:8080/health?detailed=true
```

```json
{
  "status": "healthy",
  "timestamp": "2024-12-06T10:30:00Z",
  "components": {
    "aif": {
      "status": "connected",
      "latencyMs": 45,
      "lastCheck": "2024-12-06T10:29:30Z"
    },
    "wcf": {
      "status": "connected",
      "latencyMs": 32,
      "lastCheck": "2024-12-06T10:29:30Z"
    },
    "businessConnector": {
      "status": "connected",
      "company": "PROD",
      "aos": "ax-prod-aos:2712"
    }
  },
  "metrics": {
    "requestsToday": 5420,
    "errorRate": "0.8%",
    "avgLatencyMs": 125,
    "circuitBreakerState": "Closed"
  }
}
```

### Prometheus Metrics

Available at `http://localhost:9090/metrics`:

```prometheus
# HELP mcp_requests_total Total number of MCP requests
# TYPE mcp_requests_total counter
mcp_requests_total{tool="ax_get_customer",status="success"} 1250
mcp_requests_total{tool="ax_get_customer",status="error"} 12
mcp_requests_total{tool="ax_create_salesorder",status="success"} 450

# HELP mcp_request_duration_seconds Request duration in seconds
# TYPE mcp_request_duration_seconds histogram
mcp_request_duration_seconds_bucket{tool="ax_get_customer",le="0.1"} 1100
mcp_request_duration_seconds_bucket{tool="ax_get_customer",le="0.5"} 1240
mcp_request_duration_seconds_bucket{tool="ax_get_customer",le="1.0"} 1262

# HELP mcp_circuit_breaker_state Circuit breaker state (0=closed, 1=open, 2=half-open)
# TYPE mcp_circuit_breaker_state gauge
mcp_circuit_breaker_state 0

# HELP mcp_aos_connectivity AOS connectivity (1=connected, 0=disconnected)
# TYPE mcp_aos_connectivity gauge
mcp_aos_connectivity 1

# HELP mcp_rate_limit_rejections_total Rate limit rejections
# TYPE mcp_rate_limit_rejections_total counter
mcp_rate_limit_rejections_total{user="DOMAIN\\user1"} 5
```

### Key Metrics to Monitor

| Metric | Alert Threshold | Action |
|--------|-----------------|--------|
| `mcp_aos_connectivity` | = 0 | Check AX AOS |
| `mcp_circuit_breaker_state` | = 1 | Check AX health |
| Error Rate | > 5% | Investigate logs |
| p95 Latency | > 2s | Check AX performance |
| Rate Limit Rejections | > 10/min | Review user activity |

### Grafana Dashboard

Import the pre-built dashboard from `monitoring/grafana/dashboards/mcp-server.json`:

**Panels:**

1. **Server Uptime** - Current uptime
2. **Circuit Breaker State** - Open/Closed status
3. **Requests per Second** - Request rate
4. **Error Rate** - Percentage of failed requests
5. **Tool Calls by Type** - Breakdown by tool
6. **Latency Distribution** - p50, p95, p99

### Alerting

#### Prometheus Alerting Rules

```yaml
# prometheus-alerts.yml
groups:
  - name: mcp-alerts
    rules:
      - alert: MCPServerDown
        expr: up{job="mcp-server"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "MCP Server is down"
          
      - alert: AOSDisconnected
        expr: mcp_aos_connectivity == 0
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "AOS connection lost"
          
      - alert: HighErrorRate
        expr: rate(mcp_requests_total{status="error"}[5m]) / rate(mcp_requests_total[5m]) > 0.05
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Error rate above 5%"
          
      - alert: CircuitBreakerOpen
        expr: mcp_circuit_breaker_state == 1
        for: 1m
        labels:
          severity: warning
        annotations:
          summary: "Circuit breaker is open"
```

#### Slack/Teams Notifications

The Health Monitor service sends automatic alerts:

- **AOS Disconnected** - When AOS connectivity is lost
- **Error Rate Spike** - When error rate exceeds threshold
- **Circuit Breaker Open** - When circuit breaker trips
- **Kill Switch Activated** - When emergency stop is engaged

---

## Maintenance

### Log Management

#### Log Location

| Deployment | Log Path |
|------------|----------|
| Windows Service | `C:\Services\GBL-AX2012-MCP\logs\` |

#### Log Rotation

Logs rotate daily with 30-day retention:

```
logs/
├── mcp-2024-12-06.log
├── mcp-2024-12-05.log
├── mcp-2024-12-04.log
└── ...
```

#### Log Format

```
2024-12-06 10:30:00.123 +01:00 [INF] Executing ax_get_customer {"User":"DOMAIN\\user","Company":"PROD"}
2024-12-06 10:30:00.245 +01:00 [INF] Customer found: Müller GmbH {"DurationMs":122}
```

### Audit Log

#### Query Audit Log

```http
POST /tools/call
Content-Type: application/json

{
  "tool": "ax_query_audit",
  "arguments": {
    "fromDate": "2024-12-01",
    "toDate": "2024-12-06",
    "toolName": "ax_create_salesorder",
    "maxResults": 100
  }
}
```

#### Audit Log Fields

| Field | Description |
|-------|-------------|
| `id` | Unique entry ID |
| `timestamp` | UTC timestamp |
| `toolName` | Tool that was called |
| `user` | Windows user |
| `company` | AX company |
| `success` | Success/failure |
| `durationMs` | Execution time |
| `inputHash` | Hash of input (for idempotency) |
| `errorMessage` | Error details (if failed) |

### Backup & Recovery

#### Configuration Backup

```powershell
# Backup configuration
Copy-Item C:\Services\GBL-AX2012-MCP\appsettings.*.json C:\Backups\mcp\

# Backup logs
Copy-Item C:\Services\GBL-AX2012-MCP\logs\* C:\Backups\mcp\logs\
```

#### Recovery Procedure

1. Stop the service
2. Restore configuration files
3. Verify AX connectivity
4. Start the service
5. Verify health endpoint

### Updates

#### Windows Service Update

```powershell
# Stop service
Stop-Service GBL-AX2012-MCP

# Backup current version
Copy-Item C:\Services\GBL-AX2012-MCP C:\Services\GBL-AX2012-MCP.backup -Recurse

# Deploy new version
dotnet publish -c Release -o C:\Services\GBL-AX2012-MCP

# Start service
Start-Service GBL-AX2012-MCP

# Verify health
Invoke-RestMethod http://localhost:8080/health
```

---

## Troubleshooting

### Common Issues

#### 1. AOS Connection Failed

**Symptoms:**
- Health check shows `aosConnected: false`
- Tools return `AX_ERROR`

**Diagnosis:**
```powershell
# Test AOS connectivity
Test-NetConnection -ComputerName ax-prod-aos -Port 2712

# Check AIF service
Invoke-WebRequest http://ax-prod-aos:8101/DynamicsAx/Services
```

**Resolution:**
1. Verify AOS is running
2. Check firewall rules
3. Verify service account permissions
4. Restart AOS if necessary

#### 2. Circuit Breaker Open

**Symptoms:**
- Requests return `CIRCUIT_OPEN` error
- Metrics show `mcp_circuit_breaker_state = 1`

**Diagnosis:**
```bash
# Check recent errors
grep "ERROR" logs/mcp-$(date +%Y-%m-%d).log | tail -20
```

**Resolution:**
1. Identify root cause in logs
2. Fix underlying issue (usually AX)
3. Wait for circuit breaker to reset (60s default)
4. Or restart service to force reset

#### 3. Rate Limit Exceeded

**Symptoms:**
- Requests return `RATE_LIMITED` error
- Metrics show high `mcp_rate_limit_rejections`

**Diagnosis:**
```bash
# Check which users are hitting limits
grep "Rate limit" logs/mcp-$(date +%Y-%m-%d).log | cut -d'"' -f4 | sort | uniq -c
```

**Resolution:**
1. Identify problematic user/integration
2. Review if legitimate traffic
3. Increase limits if justified
4. Optimize client-side caching

#### 4. Authentication Failed

**Symptoms:**
- Requests return `UNAUTHORIZED`
- Logs show authentication errors

**Diagnosis:**
```powershell
# Verify service account
whoami /all

# Test Windows auth
klist
```

**Resolution:**
1. Verify service account is valid
2. Check AD group membership
3. Verify SPN configuration
4. Reset service account password if needed

#### 5. High Latency

**Symptoms:**
- p95 latency > 2s
- Users report slow responses

**Diagnosis:**
```bash
# Check latency distribution
curl -s http://localhost:9090/metrics | grep mcp_request_duration
```

**Resolution:**
1. Check AX AOS performance
2. Review slow queries in AX
3. Check network latency
4. Consider dedicated AOS for MCP

### Emergency Procedures

#### Kill Switch Activation

When you need to immediately stop all write operations:

```http
POST /tools/call
Content-Type: application/json

{
  "tool": "ax_kill_switch",
  "arguments": {
    "action": "activate",
    "scope": "writes",
    "reason": "Emergency maintenance - data issue detected"
  }
}
```

**Verify:**
```http
POST /tools/call
{
  "tool": "ax_kill_switch",
  "arguments": {
    "action": "status"
  }
}
```

**Deactivate:**
```http
POST /tools/call
{
  "tool": "ax_kill_switch",
  "arguments": {
    "action": "deactivate"
  }
}
```

#### Service Restart

```powershell
# Windows
Restart-Service GBL-AX2012-MCP
```

#### Rollback

```powershell
# Windows
Stop-Service GBL-AX2012-MCP
Remove-Item C:\Services\GBL-AX2012-MCP -Recurse
Rename-Item C:\Services\GBL-AX2012-MCP.backup C:\Services\GBL-AX2012-MCP
Start-Service GBL-AX2012-MCP
```

---

## Security

### Access Control

#### AD Groups

| Group | Role | Permissions |
|-------|------|-------------|
| `MCP_Read` | Read-only | Query tools only |
| `MCP_Write` | Standard user | Read + Write tools |
| `MCP_Admin` | Administrator | All tools + Kill Switch |

#### Audit Requirements

All operations are logged with:
- User identity (Windows account)
- Timestamp (UTC)
- Tool called
- Input parameters (hashed for sensitive data)
- Success/failure
- Duration

### Network Security

#### Firewall Rules

| Port | Protocol | Source | Destination |
|------|----------|--------|-------------|
| 8080 | TCP | n8n server, approved clients | MCP server |
| 9090 | TCP | Prometheus server | MCP server |
| 2712 | TCP | MCP server | AX AOS |
| 8101 | TCP | MCP server | AX AOS (AIF/WCF) |

#### TLS Configuration

For production, configure HTTPS:

```json
{
  "HttpTransport": {
    "Port": 8443,
    "UseTls": true,
    "CertificatePath": "/certs/mcp-server.pfx",
    "CertificatePassword": "${CERT_PASSWORD}"
  }
}
```

---

## Support

### Escalation Path

| Level | Contact | Response Time |
|-------|---------|---------------|
| L1 | IT Helpdesk | < 1 hour |
| L2 | AX Admin Team | < 4 hours |
| L3 | Development Team | < 1 business day |

### Useful Commands

```bash
# Check service status
systemctl status gbl-ax2012-mcp  # Linux
Get-Service GBL-AX2012-MCP       # Windows

# View recent logs
tail -f logs/mcp-$(date +%Y-%m-%d).log

# Check metrics
curl -s http://localhost:9090/metrics | grep mcp_

# Health check
curl http://localhost:8080/health

# List tools
curl http://localhost:8080/tools
```

---

## Appendix

### Configuration Reference

See `appsettings.json` schema in Developer Guide.

### Metric Reference

See Prometheus metrics section above.

### Error Code Reference

See API Reference document.

---

*Document Version: 1.4.0 | Last Updated: 2025-12-06*
