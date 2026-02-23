using pod.keycloak;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<KeycloakService>();

builder.Services.AddAuthentication("cookie")
    .AddCookie("cookie", opt =>
    {
        opt.Cookie.Name = "rapidisimo_session";
        opt.Cookie.HttpOnly = true;

        // CAMBIOS CLAVE AQUÍ:
        opt.Cookie.SameSite = SameSiteMode.Lax; // Lax permite el envío entre puertos en localhost
        opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // No fuerces SSL si estás en HTTP
        opt.Cookie.Path = "/"; // Asegura que sea válida para /api y /auth

        opt.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("spa", p =>
        p.WithOrigins("http://localhost:4200")
         .AllowCredentials()
         .AllowAnyHeader()
         .AllowAnyMethod());
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();

app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<SessionMiddleware>();

app.UseCors("spa");

app.Run();
