# Local Development Port Registry

This machine runs several local projects side by side. Each project owns a fixed, non-overlapping
port pair (or trio, for apps with both HTTP and HTTPS backend ports) so they can all run
simultaneously without collisions. This file is the source of truth for that allocation; it is
duplicated verbatim in `mycondo-web/docs/local-development-ports.md` (same drift-risk trade-off the
convention library already accepts — see `docs/kickoff.md`).

## Reserved ports

| Application | Web | API HTTPS | API HTTP |
|---|---:|---:|---:|
| GolfClub | 4213 | 7213 | — |
| RajMango | 4215 | 7215 | — |
| SR Medical | 4217 | 7217 | — |
| **MyCondo** | **4219** | **7219** | **5219** |

MyCondo's local URLs:

| Component | URL |
|---|---|
| Web (Vite dev server) | `http://localhost:4219` |
| API (HTTPS, primary) | `https://localhost:7219` |
| API (HTTP, fallback) | `http://localhost:5219` |

## Env var overrides

Fixed ports are the default, but every one of them can be overridden without editing tracked files,
per-shell:

| Port | Override variable | Where it's read |
|---|---|---|
| Web dev server | `MYCONDO_WEB_PORT` | `mycondo-web/vite.config.ts` |
| API HTTPS/HTTP | `ASPNETCORE_URLS` (built-in ASP.NET Core mechanism) | overrides `applicationUrl` in `src/MyCondo.Api/Properties/launchSettings.json` |
| CORS allowed origins | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, … (built-in ASP.NET Core config binding) | overrides `Cors:AllowedOrigins` in `src/MyCondo.Api/appsettings.json` |

If you override the API port, also update `mycondo-web/.env`'s `VITE_MYCONDO_API_BASE_URL`, and if
you override the web port, override `Cors:AllowedOrigins` too (or the browser will get CORS errors).

## Conflict detection

`scripts/check-ports.ps1` checks all three MyCondo ports (4219 / 7219 / 5219), reports which process
holds a conflicting port, and exits non-zero with a remediation message rather than letting `dotnet
run` fail with an unhelpful bind error. Run it before `dotnet run --project src/MyCondo.Api`.
`mycondo-web` ships the equivalent check at `scripts/check-ports.mjs`, wired automatically into
`npm run dev` via the `predev` script.

## Allocating the next project

Following this workspace's convention (odd-numbered web/API pairs with matching suffixes):

1. Take the next unused odd suffix after the highest currently registered (currently `19`, i.e. `9`
   → `21`).
2. Concretely, the next allocation after MyCondo is **web `4221`, API HTTPS `7221`, API HTTP
   `5221`** (SR Medical/RajMango/GolfClub didn't need a separate HTTP port; add one only if the new
   project's backend needs it, same as MyCondo).
3. Register it here (and in the new project's own copy of this file) **before** development begins,
   so the allocation is reserved even before the new project's repo exists.
4. Never reuse or renumber an existing project's ports without updating this table and notifying
   whoever else runs these apps locally.
