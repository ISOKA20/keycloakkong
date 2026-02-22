FROM kong:3.4

USER root

RUN apt-get update && apt-get install -y \
    git luarocks gcc make unzip curl \
    libssl-dev libpcre3-dev

# librerías necesarias
RUN luarocks install lua-resty-openidc
RUN luarocks install lua-resty-session
RUN luarocks install lua-resty-http
RUN luarocks install lua-resty-jwt

# plugin OIDC community
RUN git clone https://github.com/nokia/kong-oidc.git \
    /usr/local/share/lua/5.1/kong/plugins/kong-oidc

ENV KONG_PLUGINS=bundled,kong-oidc

USER kong