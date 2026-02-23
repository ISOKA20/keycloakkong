local redis = require "resty.redis"
local cjson = require "cjson"

local ExtractSession = {
    PRIORITY = 1500,
    VERSION = "3.1.0",
}

function ExtractSession:access(conf)
    local cookie_header = kong.request.get_header("cookie")
    if not cookie_header then
        return -- No hay cookies, nada que extraer
    end

    -- 1. Extraer el sessionId de la cookie 'rapidisimo_session'
    local session_id = cookie_header:match("rapidisimo_session=([^;%s]+)")
    if not session_id then return end

    -- 2. Conectar a Redis
    local red = redis:new()
    red:set_timeout(1000)
    local ok, err = red:connect(conf.redis_host, 6379)
    if not ok then
        kong.log.err("Fallo conexion Redis: ", err)
        return
    end

    -- 3. Buscar la clave que creó .NET (session:ID)
    local res, err = red:get("session:" .. session_id)
    if not res or res == ngx.null then
        kong.log.err("Sesion no encontrada en Redis para ID: ", session_id)
        return
    end

    -- 4. Parsear el JSON guardado por .NET y extraer el AccessToken
    local session_data = cjson.decode(res)
    local access_token = session_data["AccessToken"]

    if access_token then
        -- Inyectar el token en el header Authorization para el microservicio final
        kong.service.request.set_header("Authorization", "Bearer " .. access_token)
        
        -- Debug: Quita estas líneas en producción
        kong.service.request.set_header("X-Debug-Session-Found", "SI")
    end

    -- Cerrar conexion Redis
    red:set_keepalive(10000, 100)
end

return ExtractSession