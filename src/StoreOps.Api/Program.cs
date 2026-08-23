using StoreOps.Api.Middleware;
using StoreOps.Application.Activities;
using StoreOps.Application.Alerts;
using StoreOps.Application.Programmes;
using StoreOps.Application.Reports;
using StoreOps.Application.Staff;
using StoreOps.Infrastructure;
using StoreOps.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddInfrastructure();

builder.Services.AddActivitiesModule();
builder.Services.AddProgrammesModule();
builder.Services.AddStaffModule();
builder.Services.AddAlertsModule(builder.Configuration);
builder.Services.AddReportsModule();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await DemoDataSeeder.SeedAsync(app.Services);
}

app.Run();

public partial class Program { }
