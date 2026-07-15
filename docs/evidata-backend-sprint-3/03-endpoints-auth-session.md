# Endpoints auth/session

## Productivos

| Método | Endpoint | Auth | CSRF | Uso |
|---|---|---|---|---|
| GET | `/auth/login` | No | No | inicia login Entra |
| GET | `/auth/callback` | No | No | callback Entra |
| POST | `/auth/logout` | Sí | Sí | cierra sesión |
| GET | `/api/session` | Cookie opcional | No | estado sesión |
| GET | `/api/me` | Sí | No | usuario actual |
| GET | `/api/permissions` | Sí | No | permisos normalizados |
| GET | `/api/csrf` | Cookie opcional | No | refresca token CSRF |

## Local/test

| Método | Endpoint | Uso |
|---|---|---|
| POST | `/dev/auth/login` | login local controlado |
| POST | `/dev/auth/logout` | logout local |
| GET | `/dev/auth/users` | perfiles locales |
| POST | `/dev/auth/expire-session` | simular expiración |
| POST | `/dev/auth/set-scenario` | simular denied/disabled/no-tenant |

## Prohibición

`/dev/*` no debe existir o debe fallar en staging/production.
