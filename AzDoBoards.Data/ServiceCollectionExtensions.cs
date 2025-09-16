using AzDoBoards.Data.Abstractions;
using AzDoBoards.Data.Repositories;
using AzDoBoards.Utility;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace AzDoBoards.Data;

/// <summary>
/// Service collection extensions for data layer
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds data services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">Storage connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Register Azure Table Service Client
        services.AddSingleton<TableServiceClient>(serviceProvider =>
        {
            return new TableServiceClient(connectionString);
        });

        // Register repositories
        services.AddScoped<IKeyValueRepository, AzureTableSettingsRepository>();
        services.AddScoped<ISettingsRepository, AzureTableSettingsRepository>();

        return services;
    }

    /// <summary>
    /// Adds data services with default connection string from constants
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        return services.AddDataServices(Constants.Azure_DefaultStorageConnectionString);
    }
}