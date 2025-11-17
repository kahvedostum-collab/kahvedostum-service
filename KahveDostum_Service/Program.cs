using KahveDostum_Service.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Controller + Swagger servisleri
builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

// Swagger middleware (detayı extension'da)
app.UseSwaggerDocumentation();

// Controller endpoint'lerini haritala
app.MapControllers();

app.Run();