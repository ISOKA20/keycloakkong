Para que este documento sea el manual definitivo de arquitectura para tu equipo, le he añadido las capas que faltaban: Seguridad Avanzada, el flujo de Infraestructura de Red, la Configuración de Angular y una matriz de Observabilidad.Aquí tienes el archivo README.md con esteroides:🛡️ Rapidísimo Stack: Kong + Keycloak + .NET + RedisArquitectura de Sesión Centralizada y Seguridad PerimetralEsta arquitectura implementa el patrón BFF (Backend For Frontend) Light, donde el Gateway gestiona la complejidad de los tokens, permitiendo que el Frontend solo maneje cookies seguras.1️⃣ Mapa de Conectividad (Puertos y Protocolos)ComponentePuertoProtocoloComunicaciónAngular4200HTTPOrigen de la petición.Kong (Proxy)8000HTTPPunto de entrada único para el cliente.Kong (Admin)8001HTTPConfiguración declarativa..NET Auth5152HTTPUpstream: Comunicación interna Gateway-Servicio.Keycloak8080HTTPIdentity Provider.Redis6379RESPAlmacén de sesiones volátiles.2️⃣ Docker Compose (Producción-Ready)YAMLversion: "3.9"

services:
  redis:
    image: redis:7-alpine
    container_name: redis
    command: redis-server --save 60 1 --loglevel warning
    networks: [rapidisimo-net]

  kong:
    image: kong/kong-gateway:3.9.0.0
    container_name: kong
    environment:
      KONG_DATABASE: "off"
      KONG_DECLARATIVE_CONFIG: /opt/kong/kong.yml
      KONG_PLUGINS: "bundled,extract-session"
      KONG_LUA_PACKAGE_PATH: "/opt/kong/plugins/?.lua;;"
      KONG_LOG_LEVEL: info
    volumes:
      - ./kong/kong.yml:/opt/kong/kong.yml:ro
      - ./kong/plugins/extract-session:/opt/kong/plugins/extract-session:ro
    ports:
      - "8000:8000" # Proxy
      - "8001:8001" # Admin API
    networks: [rapidisimo-net]

  # El backend .NET se asume corriendo en el Host o en otro contenedor
  # Si corre en el Host, Kong lo accede vía 'host.docker.internal'

networks:
  rapidisimo-net:
    driver: bridge
3️⃣ Plugin Custom: extract-sessionEste plugin desacopla el Token de la UI. El navegador envía la cookie, Kong pone el Bearer Token.📄 schema.luaLuareturn {
  name = "extract-session",
  fields = {
    { config = {
        type = "record",
        fields = {
          { redis_host = { type = "string", default = "redis" }, },
          { redis_port = { type = "number", default = 6379 }, },
        },
    }, },
  },
}
📄 handler.luaLualocal redis = require "resty.redis"
local cjson = require "cjson"

local ExtractSession = {
    PRIORITY = 1500, -- Superior a JWT (1450)
    VERSION = "1.0.0",
}

function ExtractSession:access(conf)
    local cookie_header = kong.request.get_header("cookie")
    if not cookie_header then return end

    local session_id = cookie_header:match("rapidisimo_session=([^;%s]+)")
    if not session_id then return end

    local red = redis:new()
    local ok, err = red:connect(conf.redis_host, conf.redis_port)
    if not ok then return kong.log.err("Redis error: ", err) end

    local res, err = red:get("session:" .. session_id)
    if not res or res == ngx.null then return end

    local data = cjson.decode(res)
    if data["AccessToken"] then
        -- Inyección de seguridad para Upstreams
        kong.service.request.set_header("Authorization", "Bearer " .. data["AccessToken"])
    end
    
    red:set_keepalive(10000, 100)
end

return ExtractSession
4️⃣ Configuración .NET Core (Program.cs)La clave es el orden de los middlewares y la configuración de las cabeceras reenviadas por Kong.C#var builder = WebApplication.CreateBuilder(args);

// 1. Configurar CORS para el puerto de Kong
builder.Services.AddCors(opt => {
    opt.AddPolicy("spa", p => p.WithOrigins("http://localhost:4200")
                               .AllowCredentials()
                               .AllowAnyHeader()
                               .AllowAnyMethod());
});

// 2. Cookie con SameSite Lax para permitir el flujo entre puertos locales
builder.Services.AddAuthentication("cookie").AddCookie("cookie", opt => {
    opt.Cookie.Name = "rapidisimo_session";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SameSite = SameSiteMode.Lax;
    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
    opt.Cookie.Path = "/";
});

var app = builder.Build();

// 3. Middlewares en orden estricto
app.UseForwardedHeaders(); // Detecta IP real tras Kong
app.UseCors("spa");

// IMPORTANTE: Comentar para evitar el bucle 307 Redirect en Local
// app.UseHttpsRedirection(); 

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
5️⃣ Configuración Kong Declarativa (kong.yml)YAML_format_version: "3.0"

services:
  - name: auth-srv
    url: http://host.docker.internal:5152
    routes:
      - name: auth-route
        paths: [/auth]
        strip_path: false

  - name: business-api
    url: http://httpbin:80
    routes:
      - name: api-route
        paths: [/api]
        strip_path: true
        plugins:
          - name: extract-session
            config: { redis_host: redis }
          - name: jwt
            config:
              key_claim_name: iss
              claims_to_verify: [exp]
              clock_skew: 60

consumers:
  - username: master-realm
    jwt_secrets:
      - key: http://localhost:8080/realms/master
        algorithm: RS256
        rsa_public_key: |
          -----BEGIN PUBLIC KEY-----
          MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...
          -----END PUBLIC KEY-----

plugins:
  - name: cors
    config:
      origins: [http://localhost:4200]
      credentials: true
      headers: [Accept, Content-Type, Authorization, Cookie]
      exposed_headers: [Set-Cookie]
6️⃣ Implementación en Angular (ApiService)Para que la cookie viaje a través de Kong, todas las peticiones deben incluir withCredentials.TypeScript@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = 'http://localhost:8000'; // SIEMPRE el puerto de Kong

  constructor(private http: HttpClient) {}

  post<T>(url: string, body: any) {
    return this.http.post<T>(this.base + url, body, { 
        withCredentials: true // <--- REQUERIDO PARA COOKIES
    });
  }
}
7️⃣ Matriz de TroubleshootingSíntomaCausa ProbableSoluciónStatus 307UseHttpsRedirection activo.Comentar línea en Program.cs.Error 401iss no coincide.El iss del token debe ser idéntico a la key del Consumer.Error 502Upstream caído o puerto mal.Revisar que Kong llegue al puerto 5152.No llega CookieCORS incorrecto.Verificar credentials: true en Kong y Angular.JWT ExpiredDesincronización de reloj.Aumentar clock_skew en el plugin JWT a 60s.