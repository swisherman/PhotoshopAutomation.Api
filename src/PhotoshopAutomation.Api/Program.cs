using MockupWorkflow.Shared.Models;
using PhotoshopAutomation.Api.Services;
using PhotoshopAutomationApi.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add services to the container.
builder.Services.AddSingleton<PodCollection>();
builder.Services.AddSingleton<MongoService>();
builder.Services.AddScoped<IMockupGenerationService, MockupGenerationService>();

builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection(MongoSettings.SectionName));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<MongoSettings>>().Value);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUxP", policy =>
    {
        var allowedOrigins =
     builder.Configuration
         .GetSection("Cors:AllowedOrigins")
         .Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowUxp", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
            });
        });
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
