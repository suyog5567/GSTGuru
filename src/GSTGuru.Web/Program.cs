using System.Text.Json.Serialization;
using GSTGuru.Web.Data;
using GSTGuru.Web.Features.Chat;
using GSTGuru.Web.Gst;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddTransient<IGstCalculator, GstCalculator>();
builder.Services.AddTransient<OfflineChatProvider>();
builder.Services.AddHttpClient<OpenAiChatProvider>();
builder.Services.AddHttpClient<AnthropicChatProvider>();
builder.Services.AddHttpClient<GeminiChatProvider>();
builder.Services.AddSingleton<ChatProviderFactory>();
builder.Services.AddScoped<ChatService>();

builder.Services.AddDbContext<GstDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GstDbContext>();
    var calculator = scope.ServiceProvider.GetRequiredService<IGstCalculator>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, calculator);
}

app.Run();
