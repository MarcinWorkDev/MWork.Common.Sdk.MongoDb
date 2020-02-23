using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MWork.Common.Sdk.Repositories;
using MWork.Common.Sdk.Repositories.Types;

namespace MWork.Common.Sdk.MongoDb
{
    public class MongoRepository<TEntity> : IDataRepository<TEntity> where TEntity : IWithId
    {
        private readonly ILogger<IDataRepository<TEntity>> _logger;
        private readonly IMongoCollection<TEntity> _collection;
        private readonly bool _withCreatedAudit = false;
        private readonly bool _withUpdatedAudit = false;
        
        public MongoRepository(ILogger<IDataRepository<TEntity>> logger, IMongoDatabase database, string collectionName)
        {
            _logger = logger;
            _collection = database.GetCollection<TEntity>(collectionName);
            _withCreatedAudit = (bool) typeof(TEntity).GetInterfaces()?.Any(x => x == typeof(IWithCreatedMetadata));
            _withUpdatedAudit = (bool) typeof(TEntity).GetInterfaces()?.Any(x => x == typeof(IWithModifiedMetadata));
        }

        public async Task Create(TEntity entity)
        {
            if (_withCreatedAudit && ((IWithCreatedMetadata) entity).CreatedBy == null)
            {
                throw new ArgumentNullException(nameof(IWithCreatedMetadata.CreatedBy), "Property value is required for entity with creation audit!");
            }

            if (_withCreatedAudit && ((IWithCreatedMetadata) entity).CreatedAtUtc == default)
            {
                ((IWithCreatedMetadata) entity).CreatedAtUtc = DateTime.UtcNow;
            }

            await _collection.InsertOneAsync(entity);
        }

        public async Task<TEntity> GetOne(Guid id)
        {
            var items = await _collection.FindAsync(x => x.Id == id);
            return items.FirstOrDefault();
        }

        public async Task<IEnumerable<TEntity>> GetAll(Expression<Func<TEntity, bool>> filter)
        {
            var items = await _collection.FindAsync(filter);
            return items.ToEnumerable();
        }

        public async Task Update(Guid id, Action<TEntity> changes)
        {
            var entity = await GetOne(id);
            changes.Invoke(entity);
            
            await this.Update(entity);
        }

        public async Task Update(TEntity entity)
        {
            if (_withCreatedAudit && ((IWithModifiedMetadata) entity).ModifiedBy == null)
            {
                throw new ArgumentNullException(nameof(IWithModifiedMetadata.ModifiedBy), "Property value is required for entity with modification audit!");
            }

            if (_withCreatedAudit && ((IWithModifiedMetadata) entity).ModifiedAtUtc == default)
            {
                ((IWithModifiedMetadata) entity).ModifiedAtUtc = DateTime.UtcNow;
            }
            
            await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        }

        public async Task Delete(Guid id)
        {
            await _collection.DeleteOneAsync(o => o.Id == id);
        }
    }
}