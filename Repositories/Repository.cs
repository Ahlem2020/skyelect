using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;
using ElectionApi.Models;

namespace ElectionApi.Repositories
{
    public class Repository<TDocument> : IRepository<TDocument> where TDocument : IDocument
    {
        protected readonly IMongoCollection<TDocument> _collection;

        public Repository(IMongoDatabase database)
        {
            _collection = database.GetCollection<TDocument>(typeof(TDocument).Name);
        }

        public virtual IQueryable<TDocument> AsQueryable()
        {
            return _collection.AsQueryable();
        }

        public virtual IEnumerable<TDocument> FilterBy(
            Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(filterExpression).ToEnumerable();
        }

        public virtual Task<IEnumerable<TDocument>> FilterByAsync(
            Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() => { return _collection.Find(filterExpression).ToEnumerable(); });
        }

        public virtual IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression, ProjectionDefinition<TDocument, TProjected> projectionDefinition)
        {
            return _collection.Find(filterExpression).Project(projectionDefinition).ToEnumerable();
        }

        public virtual Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression, ProjectionDefinition<TDocument, TProjected> projectionDefinition)
        {
            return Task.Run(() => { return _collection.Find(filterExpression).Project(projectionDefinition).ToEnumerable(); });
        }

        public virtual IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression)
        {
            return _collection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
        }

        public virtual Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression)
        {
            return Task.Run(() => { return _collection.Find(filterExpression).Project(projectionExpression).ToEnumerable(); });

        }

        public virtual IEnumerable<TDocument> FilterBy(FilterDefinition<TDocument> filterExpression)
        {
            return _collection.Find(filterExpression).ToEnumerable();
        }

        public virtual Task<IEnumerable<TDocument>> FilterByAsync(FilterDefinition<TDocument> filterExpression)
        {
            return Task.Run(() => { return _collection.Find(filterExpression).ToEnumerable(); });

        }

        public virtual IEnumerable<TProjected> FilterBy<TProjected>(FilterDefinition<TDocument> filterExpression, ProjectionDefinition<TDocument, TProjected> projectionDefinition)
        {
            return _collection.Find(filterExpression).Project(projectionDefinition).ToEnumerable();
        }

        public virtual Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(FilterDefinition<TDocument> filterExpression, ProjectionDefinition<TDocument, TProjected> projectionDefinition)
        {
            return Task.Run(() => { return _collection.Find(filterExpression).Project(projectionDefinition).ToEnumerable(); });
        }

        public virtual TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(filterExpression).FirstOrDefault();
        }

        public virtual Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() =>
            {
                return _collection.Find(filterExpression).FirstOrDefaultAsync();
            });
        }

        public virtual TDocument FindOne(FilterDefinition<TDocument> filterExpression)
        {
            return _collection.Find(filterExpression).FirstOrDefault();
        }

        public virtual Task<TDocument> FindOneAsync(FilterDefinition<TDocument> filterExpression)
        {
            return Task.Run(() =>
            {
                return _collection.Find(filterExpression).FirstOrDefaultAsync();
            });
        }

        public virtual TDocument FindById(string id)
        {
            var filter = Builders<TDocument>.Filter.Eq("_id", ObjectId.Parse(id));
            return _collection.Find(filter).FirstOrDefault();
        }

        public virtual Task<TDocument> FindByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var filter = Builders<TDocument>.Filter.Eq("_id", ObjectId.Parse(id));
                return _collection.Find(filter).FirstOrDefaultAsync();
            });
        }


        public virtual void InsertOne(TDocument document)
        {
            _collection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TDocument document)
        {
            return Task.Run(() => { return _collection.InsertOneAsync(document); });
        }

        public void InsertMany(ICollection<TDocument> documents)
        {
            _collection.InsertMany(documents);
        }


        public virtual Task InsertManyAsync(ICollection<TDocument> documents)
        {
            return Task.Run(() => { _collection.InsertManyAsync(documents); });
        }

        public void ReplaceOne(TDocument document)
        {
            var filter = Builders<TDocument>.Filter.Eq("_id", ObjectId.Parse(document.Id));
            _collection.FindOneAndReplace(filter, document);
        }

        public virtual Task ReplaceOneAsync(TDocument document)
        {
            return Task.Run(() =>
            {
                var filter = Builders<TDocument>.Filter.Eq("_id", ObjectId.Parse(document.Id));
                _collection.FindOneAndReplaceAsync(filter, document);
            });

        }

        public virtual void UpdateOne(string id, UpdateDefinition<TDocument> document)
        {
            var filter = Builders<TDocument>.Filter.Eq("_id", ObjectId.Parse(id));
            document = document.Set(x => x.UpdatedAt, DateTime.UtcNow);
            _collection.UpdateOne(filter, document);
        }
        public virtual Task UpdateOneAsync(string id, UpdateDefinition<TDocument> document)
        {
            return Task.Run(() =>
            {
                var filter = Builders<TDocument>.Filter.Eq("_id", ObjectId.Parse(id));
                document = document.Set(x => x.UpdatedAt, DateTime.UtcNow);
                _collection.UpdateOneAsync(filter, document);
            });
        }

        public virtual void UpdateMany(List<string> ids, UpdateDefinition<TDocument> document)
        {
            var objectIds = ids.Select((id) => ObjectId.Parse(id));
            var filter = Builders<TDocument>.Filter.In("_id", objectIds);
            document = document.Set(x => x.UpdatedAt, DateTime.UtcNow);
            _collection.UpdateMany(filter, document);
        }

        public virtual Task UpdateManyAsync(List<string> ids, UpdateDefinition<TDocument> document)
        {
            return Task.Run(() =>
            {
                var objectIds = ids.Select((id) => ObjectId.Parse(id));
                var filter = Builders<TDocument>.Filter.In("_id", objectIds);
                document = document.Set(x => x.UpdatedAt, DateTime.UtcNow);
                _collection.UpdateManyAsync(filter, document);
            });
        }

        public virtual void UpdateMany(FilterDefinition<TDocument> filterExpression, UpdateDefinition<TDocument> document)
        {
            document = document.Set(x => x.UpdatedAt, DateTime.UtcNow);
            _collection.UpdateMany(filterExpression, document);
        }

        public virtual Task UpdateManyAsync(FilterDefinition<TDocument> filterExpression, UpdateDefinition<TDocument> document)
        {
            return Task.Run(() =>
            {
                document = document.Set(x => x.UpdatedAt, DateTime.UtcNow);
                _collection.UpdateManyAsync(filterExpression, document);
            });
        }

        public virtual void DeleteOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            _collection.FindOneAndDelete(filterExpression);
        }

        public virtual Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() =>
            {
                _collection.FindOneAndDeleteAsync(filterExpression);
            });
        }

        public virtual void DeleteOne(FilterDefinition<TDocument> filterExpression)
        {
            _collection.FindOneAndDelete(filterExpression);
        }

        public virtual Task DeleteOneAsync(FilterDefinition<TDocument> filterExpression)
        {
            return Task.Run(() =>
            {
                _collection.FindOneAndDeleteAsync(filterExpression);
            });
        }

        public virtual void DeleteById(string id)
        {
            var filter = Builders<TDocument>.Filter.Eq("_id", ObjectId.Parse(id));
            _collection.FindOneAndDelete(filter);
        }

        public virtual Task DeleteByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var filter = Builders<TDocument>.Filter.Eq("_id", ObjectId.Parse(id));
                _collection.FindOneAndDeleteAsync(filter);
            });
        }

        public virtual void DeleteMany(Expression<Func<TDocument, bool>> filterExpression)
        {
            _collection.DeleteMany(filterExpression);
        }

        public virtual Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() =>
            {
                _collection.DeleteManyAsync(filterExpression);
            });
        }

        public virtual void DeleteMany(FilterDefinition<TDocument> filterExpression)
        {
            _collection.DeleteMany(filterExpression);
        }

        public virtual Task DeleteManyAsync(FilterDefinition<TDocument> filterExpression)
        {
            return Task.Run(() =>
            {
                _collection.DeleteManyAsync(filterExpression);
            });
        }

        public virtual void DeleteAll()
        {
            _collection.DeleteMany(Builders<TDocument>.Filter.Empty);
        }

        public virtual Task DeleteAllAsync()
        {
            return Task.Run(() =>
            {
                _collection.DeleteManyAsync(Builders<TDocument>.Filter.Empty);
            });
        }

        public virtual long Count()
        {
            return _collection.CountDocuments(Builders<TDocument>.Filter.Empty);
        }

        public virtual Task<long> CountAsync()
        {
            return Task.Run(() =>
            {
                return _collection.CountDocumentsAsync(Builders<TDocument>.Filter.Empty);
            });
        }

        public virtual long Count(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.CountDocuments(filterExpression);
        }

        public virtual long Count(FilterDefinition<TDocument> filterExpression)
        {
            return _collection.CountDocuments(filterExpression);
        }

        public virtual Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() =>
            {
                return _collection.CountDocumentsAsync(filterExpression);
            });
        }

        public virtual Task<long> CountAsync(FilterDefinition<TDocument> filterExpression)
        {
            return Task.Run(() =>
            {
                return _collection.CountDocumentsAsync(filterExpression);
            });
        }

        public virtual Task CreateIndex(Expression<Func<TDocument, object>> filterExpression)
        {
            var builder = Builders<TDocument>.IndexKeys;
            var indexModel = new CreateIndexModel<TDocument>(builder.Ascending(filterExpression));
            return Task.Run(() => _collection.Indexes.CreateOneAsync(indexModel));
        }

        public virtual Task<List<TDocument>> PaginationAsync(
            FilterDefinition<TDocument> filterExpression,
            SortDefinition<TDocument> sortDefinition,
            int skipSize = 0,
            int limitSize = 50
            )
        {
            return Task.Run(() =>
            {
                return _collection.Aggregate().Match(filterExpression)
                .Sort(sortDefinition)
                .Skip(skipSize)
                .Limit(limitSize)
                .ToListAsync();
            });

        }

        public virtual Task<List<AggregateFacetResults>> PaginationWithFacetAsync(
            FilterDefinition<TDocument> filterExpression,
            SortDefinition<TDocument> sortDefinition,
            int skipSize = 0,
            int limitSize = 30
            )
        {
            return Task.Run(() =>
            {
                var countFacet = AggregateFacet.Create("count",
                PipelineDefinition<TDocument, AggregateCountResult>.Create(new[]
                {
                    PipelineStageDefinitionBuilder.Count<TDocument>()
                }));
                var dataFacet = AggregateFacet.Create("data",
                PipelineDefinition<TDocument, TDocument>.Create(new[]
                {
                    PipelineStageDefinitionBuilder.Sort<TDocument>(sortDefinition),
                    PipelineStageDefinitionBuilder.Skip<TDocument>(skipSize),
                    PipelineStageDefinitionBuilder.Limit<TDocument>(limitSize),
                }));
                return _collection.Aggregate().Match(filterExpression)
                .Facet(countFacet, dataFacet)
                .ToListAsync();
            });

        }

        // Convenience methods for CRUD operations
        public virtual async Task<IEnumerable<TDocument>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public virtual async Task<TDocument?> GetByIdAsync(string id)
        {
            return await FindByIdAsync(id);
        }

        public virtual async Task CreateAsync(TDocument document)
        {
            await InsertOneAsync(document);
        }

        public virtual async Task UpdateAsync(TDocument document)
        {
            await ReplaceOneAsync(document);
        }

        public virtual async Task DeleteAsync(string id)
        {
            await DeleteByIdAsync(id);
        }
    }
}
