using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using ElectionApi.Models;

namespace ElectionApi.Repositories
{
    public interface IRepository<TDocument> where TDocument : IDocument
    {
        IQueryable<TDocument> AsQueryable();
        IEnumerable<TDocument> FilterBy(
            Expression<Func<TDocument, bool>> filterExpression);
        Task<IEnumerable<TDocument>> FilterByAsync(Expression<Func<TDocument, bool>> filterExpression);
        IEnumerable<TProjected> FilterBy<TProjected>(
           Expression<Func<TDocument, bool>> filterExpression, ProjectionDefinition<TDocument, TProjected> projectionDefinition);
        Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression, ProjectionDefinition<TDocument, TProjected> projectionDefinition);
        IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression);
        Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression);
        IEnumerable<TDocument> FilterBy(FilterDefinition<TDocument> filterExpression);
        Task<IEnumerable<TDocument>> FilterByAsync(FilterDefinition<TDocument> filterExpression);
        IEnumerable<TProjected> FilterBy<TProjected>(FilterDefinition<TDocument> filterExpression, ProjectionDefinition<TDocument, TProjected> projectionDefinition);
        Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(FilterDefinition<TDocument> filterExpression, ProjectionDefinition<TDocument, TProjected> projectionDefinition);
        TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression);
        Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression);
        TDocument FindOne(FilterDefinition<TDocument> filterExpression);
        Task<TDocument> FindOneAsync(FilterDefinition<TDocument> filterExpression);
        TDocument FindById(string id);
        Task<TDocument> FindByIdAsync(string id);
        void InsertOne(TDocument document);
        Task InsertOneAsync(TDocument document);
        void InsertMany(ICollection<TDocument> documents);
        Task InsertManyAsync(ICollection<TDocument> documents);
        void ReplaceOne(TDocument document);
        Task ReplaceOneAsync(TDocument document);
        void UpdateOne(string id, UpdateDefinition<TDocument> document);
        Task UpdateOneAsync(string id, UpdateDefinition<TDocument> document);
        void UpdateMany(List<string> ids, UpdateDefinition<TDocument> document);
        Task UpdateManyAsync(List<string> ids, UpdateDefinition<TDocument> document);
        void UpdateMany(FilterDefinition<TDocument> filterExpression, UpdateDefinition<TDocument> document);
        Task UpdateManyAsync(FilterDefinition<TDocument> filterExpression, UpdateDefinition<TDocument> document);
        void DeleteOne(Expression<Func<TDocument, bool>> filterExpression);
        Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression);
        void DeleteOne(FilterDefinition<TDocument> filterExpression);
        Task DeleteOneAsync(FilterDefinition<TDocument> filterExpression);
        void DeleteById(string id);
        Task DeleteByIdAsync(string id);
        void DeleteMany(Expression<Func<TDocument, bool>> filterExpression);
        Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression);
        void DeleteMany(FilterDefinition<TDocument> filterExpression);
        Task DeleteManyAsync(FilterDefinition<TDocument> filterExpression);
        void DeleteAll();
        Task DeleteAllAsync();
        long Count();
        Task<long> CountAsync();
        long Count(Expression<Func<TDocument, bool>> filterExpression);
        long Count(FilterDefinition<TDocument> filterExpression);
        Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression);
        Task<long> CountAsync(FilterDefinition<TDocument> filterExpression);
        Task CreateIndex(Expression<Func<TDocument, object>> filterExpression);
        Task<List<TDocument>> PaginationAsync(
            FilterDefinition<TDocument> filterExpression,
            SortDefinition<TDocument> sortDefinition,
            int skipSize = 0,
            int limitSize = 50
            );
        Task<List<AggregateFacetResults>> PaginationWithFacetAsync(
            FilterDefinition<TDocument> filterExpression,
            SortDefinition<TDocument> sortDefinition,
            int skipSize = 0,
            int limitSize = 30
            );

        // Convenience methods for CRUD operations
        Task<IEnumerable<TDocument>> GetAllAsync();
        Task<TDocument?> GetByIdAsync(string id);
        Task CreateAsync(TDocument document);
        Task UpdateAsync(TDocument document);
        Task DeleteAsync(string id);
    }
}
