using ScholarshipRequest.Api.Features.SystemInfo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health/live");
app.MapSystemInfoEndpoints();

app.Run();

public partial class Program;
