using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MWork.Common.Sdk.Repositories;
using MWork.Common.Sdk.Repositories.Types;

namespace MWork.Common.Sdk.MongoDb
{
    public static class Extensions
    {
        public static IServiceCollection AddMongo(this IServiceCollection services, Action<MongoDbOptions> optionsBuilder)
        {
            var options = new MongoDbOptions();
            optionsBuilder?.Invoke(options);

            services.AddSingleton<IMongoClient>(new MongoClient(options.ConnectionString));
            services.AddScoped<IMongoDatabase>(o
                => services
                    .BuildServiceProvider()
                    .GetService<IMongoClient>()
                    .GetDatabase(options.Database));

            return services;
        }

        public static IServiceCollection AddMongoRepository<TEntity>(this IServiceCollection services, string collectionName)
            where TEntity : IWithId
        {
            services.AddScoped<IDataRepository<TEntity>>(o =>
            {
                var logger = services.BuildServiceProvider().GetService<ILogger<IDataRepository<TEntity>>>();
                var mongoDatabase = services.BuildServiceProvider().GetService<IMongoDatabase>();
                return new MongoRepository<TEntity>(logger, mongoDatabase, collectionName);
            });
                
            return services;
        }
    }
}