# Integración Kong Gateway + Keycloak (JWT RS256) – Guía Completa

Arquitectura local usando Docker Compose:

Client → Kong → Plugin JWT → Keycloak (firma) → Servicio (httpbin)

Kong valida el Access Token firmado por Keycloak usando la clave pública del realm.

---

## 1️⃣ Arquitectura

| Componente | Rol                                       |
| ---------- | ----------------------------------------- |
| Keycloak   | Identity Provider (OIDC + emisión de JWT) |
| Kong       | API Gateway + validación JWT              |
| Postgres   | Persistencia de Keycloak                  |
| httpbin    | API de prueba protegida                   |

---

## 2️⃣ Docker Compose

Archivo **docker-compose.yml**

```yaml
version: "3.9"

services:

  postgres:
    image: postgres:16
    container_name: keycloak-db
    restart: unless-stopped
    environment:
      POSTGRES_DB: keycloak
      POSTGRES_USER: keycloak
      POSTGRES_PASSWORD: keycloak
    volumes:
      - kc_db:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U keycloak -d keycloak"]
      interval: 3s
      timeout: 3s
      retries: 20
    networks:
      - kcnet

  keycloak:
    image: quay.io/keycloak/keycloak:24.0
    container_name: keycloak
    restart: unless-stopped
    command:
      - start-dev
      - --db=postgres
      - --db-url-host=postgres
      - --db-url-database=keycloak
      - --db-username=keycloak
      - --db-password=keycloak
      - --hostname-strict=false
    depends_on:
      postgres:
        condition: service_healthy
    environment:
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
    ports:
      - "8080:8080"
    networks:
      - kcnet

  httpbin:
    image: kennethreitz/httpbin
    container_name: httpbin
    restart: unless-stopped
    networks:
      - kcnet

  kong:
    image: kong/kong-gateway:3.9.0.0
    container_name: kong
    restart: unless-stopped
    environment:
      KONG_DATABASE: "off"
      KONG_DECLARATIVE_CONFIG: /opt/kong/kong.yml
      KONG_PLUGINS: "bundled"
      KONG_PROXY_ACCESS_LOG: /dev/stdout
      KONG_ADMIN_ACCESS_LOG: /dev/stdout
      KONG_ADMIN_LISTEN: 0.0.0.0:8001
      KONG_LOG_LEVEL: debug
    volumes:
      - ./kong/kong.yml:/opt/kong/kong.yml:ro
    ports:
      - "8000:8000"
      - "8001:8001"
    depends_on:
      - keycloak
      - httpbin
    networks:
      - kcnet

volumes:
  kc_db:

networks:
  kcnet:
    driver: bridge
```

---

## 3️⃣ Crear usuario y cliente en Keycloak

### Crear usuario

Realm → Users → Create

* username: test
* password: test
* Email verified: ON
* Temporary: OFF

### Crear cliente

Realm → Clients → Create

* Client ID: kong-test
* Client authentication: ON
* Standard flow: OFF
* Direct access grants: ON

Guardar el **Client Secret**

---

## 4️⃣ Obtener clave pública del Realm

```
http://localhost:8080/realms/master/protocol/openid-connect/certs
```

Tomar `x5c[0]` → convertir a PEM:

```
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A...
-----END PUBLIC KEY-----
```

---

## 5️⃣ Configurar Kong (kong.yml)

```yaml
_format_version: "3.0"

services:
  - name: httpbin
    url: http://httpbin:80
    routes:
      - name: api
        paths:
          - /api

consumers:
  - username: rapidisimo-realm
    jwt_secrets:
      - key: http://localhost:8080/realms/master
        algorithm: RS256
        rsa_public_key: |
          -----BEGIN PUBLIC KEY-----
          TU_CLAVE_PUBLICA_AQUI
          -----END PUBLIC KEY-----

plugins:
  - name: jwt
    service: httpbin
    config:
      key_claim_name: iss
      claims_to_verify:
        - exp
```

🔴 IMPORTANTE
El `iss` del token DEBE coincidir con:

```
http://localhost:8080/realms/master
```

---

## 6️⃣ Obtener Access Token

### PowerShell

```powershell
curl.exe -X POST "http://localhost:8080/realms/master/protocol/openid-connect/token" `
 -H "Content-Type: application/x-www-form-urlencoded" `
 -d "client_id=kong-test" `
 -d "client_secret=TU_SECRET" `
 -d "username=test" `
 -d "password=test" `
 -d "grant_type=password"
```

Respuesta:

```json
{
 "access_token": "eyJhbGciOiJSUzI1NiIs..."
}
```

---

## 7️⃣ Consumir API protegida

```powershell
curl.exe http://localhost:8000/api/get -H "Authorization: Bearer ACCESS_TOKEN"
```

Respuesta esperada: httpbin responde correctamente

---

## 8️⃣ Manejo de expiración (exp)

Kong valida automáticamente:

* exp → expiración
* iss → issuer
* firma RS256

Si expira:

```json
{"exp":"token expired"}
```

Esto es correcto ✔

---

## 9️⃣ Duración de sesión (Keycloak)

Realm Settings → Tokens

| Parámetro             | Valor | Significado                |
| --------------------- | ----- | -------------------------- |
| Access Token Lifespan | 300   | Token dura 5 min           |
| SSO Session Idle      | 900   | Sesión muere sin actividad |
| SSO Session Max       | 3600  | Máximo absoluto            |

---

## 🔟 Flujo real de autenticación

1. Login → Keycloak entrega access_token + refresh_token
2. API call → Kong valida firma
3. Expira access_token → frontend usa refresh_token
4. Expira refresh_token → login otra vez

Kong NO maneja sesión
Keycloak maneja sesión

---

## 1️⃣1️⃣ Errores comunes (y solución)

| Error                                   | Causa                           |
| --------------------------------------- | ------------------------------- |
| invalid key                             | PEM mal indentado               |
| failed conditional validation algorithm | algoritmo no coincide           |
| unauthorized                            | iss no coincide                 |
| token expired                           | correcto                        |
| invalid_grant                           | usuario sin password definitivo |
| realm does not exist                    | realm incorrecto                |
| invalid_client                          | client secret incorrecto        |

---

## 🧠 Concepto importante

**Kong es:** un verificador criptográfico de tokens
**Keycloak es:** el manejador de sesiones
