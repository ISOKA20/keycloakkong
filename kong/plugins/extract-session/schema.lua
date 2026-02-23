local typedefs = require "kong.db.schema.typedefs"

return {
  name = "extract-session",
  fields = {
    { consumer = typedefs.no_consumer },
    { config = {
        type = "record",
        fields = {
          { redis_host = { type = "string", default = "redis" } },
          { redis_port = { type = "number", default = 6379 } },
          { cookie_name = { type = "string", default = "rapidisimo_session" } },
          { key_prefix  = { type = "string", default = "session:" } },
        },
      },
    },
  },
}