# RiskRadar

RiskRadar is a security-focused ASP.NET Core API that detects suspicious login activity, blocks abusive IPs, and produces a risk score based on recent behavior.

## Key Features
- JWT authentication (access token) + refresh token rotation
- Login attempt logging (IP, user-agent, correlation id)
- Brute-force protection:
  - Failed login threshold within a time window
  - IP blocking with expiry
- Risk scoring (0–100) with reasons
- Risk events stream + admin dashboard endpoints
- SQL Server persistence with EF Core migrations
- Background cleanup service for expired blocks/tokens

## Tech Stack
- .NET 8 Minimal APIs
- ASP.NET Core Identity (users/roles)
- EF Core + SQL Server (LocalDB/SQL Express)
- JWT Bearer Authentication

## How to Run
1. Update connection string in `RiskRadar.Api/appsettings.json`
2. Run migrations:
   - `Add-Migration ...`
   - `Update-Database`
3. Run the API and open Swagger



## Endpoints (high-level)
- Auth: `/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`
- Admin: `/api/admin/blocked-ips`, `/api/admin/unblock-ip/{ip}`, `/api/admin/promote/{email}`
- Risk: `/api/risk/my-ip-score`, `/api/risk/events`, `/api/risk/top-ips`


