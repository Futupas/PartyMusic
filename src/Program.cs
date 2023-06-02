using PartyMusic;
using PartyMusic.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<HelpersService>();
builder.Services.AddSingleton<YoutubeService>();
builder.Services.AddSingleton<WSConnectionsService>();
builder.Services.AddSingleton<SongsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();

app.UseWebSockets();

app.Run();


if (string.IsNullOrEmpty(app.Configuration["savingFolder"]))
{
    app.Configuration["savingFolder"] = "wwwroot/data";
}

if (app.Configuration.GetValue<bool>("deleteDataFolder"))
{
    Directory.Delete("wwwroot/data", true);
}
