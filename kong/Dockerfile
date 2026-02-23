# Usamos la imagen oficial de Kong Gateway 3.9
FROM kong/kong-gateway:3.9.0.0

USER root

# 1) Copiamos las dependencias de Lua (Redis)
# Asegúrate de que en tu carpeta local 'vendor/resty' esté el archivo redis.lua
COPY vendor/resty /usr/local/share/lua/5.1/resty

# 2) Copiamos tu plugin custom ya refactorizado (el que no tiene BasePlugin)
# Esto copiará handler.lua y schema.lua a la ruta donde Kong busca plugins
COPY plugins/extract-session /usr/local/share/lua/5.1/kong/plugins/extract-session

# 3) Habilitamos el plugin en las variables de entorno
# 'bundled' son los plugins que trae Kong por defecto, 'extract-session' es el tuyo
ENV KONG_PLUGINS=bundled,extract-session

# 4) Ajustamos permisos para que el usuario 'kong' pueda leer los archivos
RUN chown -R kong:root /usr/local/share/lua/5.1/resty /usr/local/share/lua/5.1/kong/plugins/extract-session \
    && chmod -R a+rX /usr/local/share/lua/5.1/kong/plugins/extract-session

# Volvemos al usuario no privilegiado por seguridad
USER kong