using Microsoft.EntityFrameworkCore;
using Serilog;
using TransactionService.Data;
using TransactionService.Services;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Добавление сервисов в контейнер
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Transaction Service API", Version = "v1" });
});

// Настройка Entity Framework
builder.Services.AddDbContext<TransactionDbContext>(options =>
{
    if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("UseInMemoryDatabase", false))
    {
        // Использование InMemory базы данных для локальной разработки/тестирования
        options.UseInMemoryDatabase("TransactionServiceDB");
    }
    else
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    }
});

// Регистрация сервисов
builder.Services.AddScoped<ITransactionService, TransactionServiceImpl>();

// Настройка CORS при необходимости
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Настройка конвейера HTTP-запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Автоматическая миграция базы данных при запуске (пропуск для InMemory)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    try
    {
        if (!context.Database.IsInMemory())
        {
            context.Database.Migrate();
            Log.Information("Миграция базы данных выполнена успешно");
        }
        else
        {
            context.Database.EnsureCreated();
            Log.Information("InMemory база данных создана успешно");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Произошла ошибка при настройке базы данных");
    }
}

app.Run();