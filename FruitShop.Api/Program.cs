namespace FruitShop.Api;

using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using FruitShop.Api.Data;
using FruitShop.Api.Middleware;
using FruitShop.Api.Repositories;
using FruitShop.Api.Services;
using FruitShop.Api.Services.ConditionEvaluator;
using FruitShop.Api.Services.PriceActionStrategy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        const string allowSpecificOrigins = "_myAllowSpecificOrigins";

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(allowSpecificOrigins, policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddAutoMapper(configuration => configuration.AddMaps(typeof(Program).Assembly));
        builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblyContaining<Program>());
        builder.Services.AddDbContext<FruitShopDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("FruitShopDatabase")));
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IPriceRuleRepository, PriceRuleRepository>();
        builder.Services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        builder.Services.AddScoped<IOrderProcessingService, OrderProcessingService>();
        builder.Services.AddScoped<IOrderItemCalculationService, OrderItemCalculationService>();
        builder.Services.AddScoped<IPricingService, PricingService>();
        builder.Services.AddScoped<IPricingStrategyFactory, PricingStrategyFactory>();
        builder.Services.AddScoped<IConditionEvaluator, QuantityConditionEvaluator>();
        builder.Services.AddScoped<IConditionEvaluator, DateConditionEvaluator>();
        builder.Services.AddScoped<IConditionEvaluator, CustomerTierConditionEvaluator>();
        builder.Services.AddScoped<IConditionEvaluator, CartSubtotalConditionEvaluator>();
        builder.Services.AddScoped<IPriceActionStrategy, PercentageDiscountStrategy>();
        builder.Services.AddScoped<IPriceActionStrategy, AmountOffStrategy>();
        builder.Services.AddScoped<IPriceActionStrategy, OverridePriceStrategy>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FruitShopDbContext>();
            await dbContext.Database.MigrateAsync();
            await FruitShopDemoSeeder.SeedAsync(dbContext);
        }

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors(allowSpecificOrigins);

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
