# ADR-001: AX 2012 Integration Approach

**Status:** Accepted  
**Date:** 2025-12-06  
**Decision Makers:** Reinerw

## Context

We need to integrate the MCP Server with Microsoft Dynamics AX 2012 R3 CU13. AX 2012 offers multiple integration options:

1. **AIF (Application Integration Framework)** - Built-in SOAP web services
2. **Custom WCF Services** - Custom-built services deployed to AX
3. **Business Connector .NET** - Direct .NET integration via COM interop
4. **Direct SQL** - Database access (not recommended)

Each approach has different characteristics for performance, security, and maintenance.

## Decision

We will use a **hybrid approach**:

| Operation Type | Integration Method | Rationale |
|----------------|-------------------|-----------|
| **Read Operations** | AIF Standard Services | Stable, well-documented, no custom code in AX |
| **Write Operations** | Custom WCF Services | Full control over transaction handling, validation |
| **Admin/Health** | Business Connector .NET | Direct access for diagnostics, no service overhead |

### Detailed Mapping

| Tool | Method | Service |
|------|--------|---------|
| `ax_health_check` | BC.NET | Direct AX call |
| `ax_get_customer` | AIF | CustCustomerService |
| `ax_get_salesorder` | AIF | SalesSalesOrderService |
| `ax_check_inventory` | AIF | InventInventSumService |
| `ax_simulate_price` | Custom WCF | GBL_PriceSimulationService |
| `ax_create_salesorder` | Custom WCF | GBL_SalesOrderService |

## Consequences

### Positive

- **Stability:** AIF services are battle-tested and well-documented
- **Control:** Custom WCF gives us full control over write operations
- **Flexibility:** BC.NET allows direct diagnostics without service overhead
- **Separation:** Read/Write separation allows independent scaling and optimization

### Negative

- **Complexity:** Three different integration patterns to maintain
- **Custom Code:** WCF services require X++ development in AX
- **BC.NET Dependency:** Requires Business Connector installed on MCP server

### Risks

| Risk | Mitigation |
|------|------------|
| AIF performance issues | Circuit breaker, caching for repeated reads |
| WCF service failures | Idempotency keys, retry with exponential backoff |
| BC.NET connection issues | Health check, automatic reconnection |

## Alternatives Considered

### Option A: AIF Only
- **Pros:** Single integration pattern, no custom AX code
- **Cons:** Limited control over write operations, no custom validation
- **Rejected:** Insufficient control for complex order creation

### Option B: Custom WCF Only
- **Pros:** Full control, consistent pattern
- **Cons:** Requires custom service for every operation, more AX development
- **Rejected:** Too much custom code for simple read operations

### Option C: Direct SQL
- **Pros:** Fast, no service overhead
- **Cons:** Bypasses business logic, security risks, upgrade issues
- **Rejected:** Unacceptable risk and maintenance burden

## References

- [AX 2012 AIF Documentation](https://docs.microsoft.com/en-us/dynamicsax-2012/appuser-itpro/application-integration-framework-aif)
- [AX 2012 WCF Services](https://docs.microsoft.com/en-us/dynamicsax-2012/appuser-itpro/services-and-application-integration-framework-aif)
- [Business Connector .NET](https://docs.microsoft.com/en-us/dynamicsax-2012/appuser-itpro/net-business-connector)
