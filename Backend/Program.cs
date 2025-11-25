using Backend.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Enable Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Root endpoint
app.MapGet("/", () => new
{
    message = "Tunnel Security Backend API",
    version = "1.0.0",
    endpoints = new
    {
        swagger = "/swagger",
        sensors = "/api/sensors",
        stations = "/api/stations",
        signalR = "/hubs/sensors"
    }
});

app.MapControllers();
app.MapHub<SensorHub>("/hubs/sensors");

app.Run();
