# ADR-002: Security Model

**Status:** Accepted  
**Date:** 2025-12-06  
**Decision Makers:** Reinerw

## Context

The MCP Server will perform read and write operations on AX 2012 data. We need a security model that:

1. Authenticates callers
2. Authorizes operations based on roles
3. Provides audit trail for compliance
4. Protects against common attack vectors

## Decision

### Authentication

| Layer | Method |
|-------|--------|
| MCP Client → MCP Server | Windows Authentication (Kerberos) |
| MCP Server → AIF | Windows Authentication (Kerberos) |
| MCP Server → WCF | Service Account (Windows Auth) |
| MCP Server → BC.NET | Service Account (Windows Auth) |

### Authorization

Three-tier role model mapped to AX security:

| MCP Role | AX Security Role | Permissions |
|----------|------------------|-------------|
| `MCP_Read` | SalesClerk | Read customer, order, inventory, price |
| `MCP_Write` | SalesManager | All read + create/update orders |
| `MCP_Admin` | ITAdmin | All + health check, audit access |

### Role Mapping

```
AD Group                    → MCP Role      → AX Role
─────────────────────────────────────────────────────
CORP\MCP-Users-Read         → MCP_Read      → SalesClerk
CORP\MCP-Users-Write        → MCP_Write     → SalesManager
CORP\MCP-Admins             → MCP_Admin     → ITAdmin
```

### Approval Workflow

High-value operations require additional approval:

| Condition | Approval Required |
|-----------|-------------------|
| Order total > €50,000 | Finance Manager |
| New customer creation | Credit Manager |
| Credit limit change | Finance Director |

### Audit Trail

Every operation is logged with:

```json
{
  "timestamp": "2025-12-06T14:30:00Z",
  "user_id": "CORP\\jsmith",
  "tool": "ax_create_salesorder",
  "input": { "customer_account": "CUST-001", "...": "..." },
  "output": { "sales_id": "SO-2025-001234", "...": "..." },
  "success": true,
  "duration_ms": 1234,
  "client_ip": "10.0.1.50",
  "correlation_id": "abc-123-def"
}
```

Audit logs are stored:
- **Write operations:** SQL Database (90 days retention)
- **Read operations:** File logs (30 days retention)
- **All operations:** Event stream for real-time monitoring

## Consequences

### Positive

- **Single Sign-On:** Users authenticate once via Windows
- **Centralized Management:** AD groups control access
- **Compliance:** Full audit trail for SOX/GDPR
- **Defense in Depth:** Multiple layers of security

### Negative

- **AD Dependency:** Requires Active Directory infrastructure
- **Service Account:** Shared account for WCF (less granular audit)
- **Complexity:** Multiple role mappings to maintain

### Security Controls

| Threat | Control |
|--------|---------|
| Unauthorized access | Windows Authentication + Role check |
| Privilege escalation | Role-based authorization at tool level |
| Data tampering | Audit logging, idempotency keys |
| Injection attacks | Input validation, parameterized queries |
| DoS | Rate limiting (100 req/min/user) |
| Replay attacks | Idempotency keys with TTL |

## Alternatives Considered

### Option A: API Keys
- **Pros:** Simple, no AD dependency
- **Cons:** No user identity, key management burden
- **Rejected:** Insufficient for audit requirements

### Option B: OAuth 2.0 / JWT
- **Pros:** Modern, stateless, cross-platform
- **Cons:** Requires token server, doesn't integrate with AX auth
- **Rejected:** Adds complexity without benefit for Windows environment

### Option C: Certificate-based
- **Pros:** Strong authentication, no passwords
- **Cons:** Certificate management overhead, user experience
- **Rejected:** Too complex for internal tool

## References

- [AX 2012 Security Architecture](https://docs.microsoft.com/en-us/dynamicsax-2012/appuser-itpro/security-architecture)
- [Windows Authentication Best Practices](https://docs.microsoft.com/en-us/windows-server/security/kerberos/kerberos-authentication-overview)
