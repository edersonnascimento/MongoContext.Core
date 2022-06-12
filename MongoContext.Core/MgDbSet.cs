using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace MongoContext.Core
{
    /// <summary>
    /// Created by: Éderson Curto do Nascimento
    /// Created at: 2017-01-28
    /// E-mail: ederson.nascimento@outlook.com
    /// </summary>
    public class MgDbSet<T> : IMgDbSet<T> where T : class
    {
        private readonly IMgDbContext _context;
        public MgDbSet(IMgDbContext context)
        {
            _context = context;
            Connection = _context.GetConnection();
        }

        #region Private Methods

        private PropertyInfo GetId()
        {
            var id = typeof(T).GetProperties()
                            .Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(BsonIdAttribute)))
                            .FirstOrDefault();
            if (id == null)
            {
                var map = _context.GetRegisteredClassMap<T>();
                if (map != null)
                {
                    id = typeof(T).GetProperties()
                        .Where(p => p.Name == map.IdMemberMap?.MemberName)
                        .FirstOrDefault();    
                }
                
            }
            return id;
        }

        private bool IsNew(PropertyInfo id, object value) =>
            (id.PropertyType == typeof(ObjectId) && (ObjectId)value == ObjectId.Empty) ||
            (id.PropertyType == typeof(Guid) && (Guid)value == Guid.Empty);
        private void InsertOrReplace(T subject, PropertyInfo id)
        {
            var value = id.GetValue(subject);
            if (IsNew(id, value)) {
                if (id.PropertyType == typeof(Guid)) id.SetValue(subject, Guid.NewGuid());
                if (id.PropertyType == typeof(ObjectId)) id.SetValue(subject, ObjectId.GenerateNewId());

                _context.Collection<T>().InsertOne(subject);
            } else {
                var filter = Builders<T>.Filter.Eq(id.Name, value);
                _context.Collection<T>().ReplaceOne(filter, subject, new ReplaceOptions { IsUpsert = true });
            }
        }
        private async Task InsertOrReplaceAsync(T subject, PropertyInfo id)
        {
            var value = id.GetValue(subject);
            if (IsNew(id, value)) {
                if (id.PropertyType == typeof(Guid)) id.SetValue(subject, Guid.NewGuid());

                await _context.Collection<T>().InsertOneAsync(subject);
            } else {
                var filter = Builders<T>.Filter.Eq(id.Name, value);
                await _context.Collection<T>().ReplaceOneAsync(filter, subject, new ReplaceOptions { IsUpsert = true });
            }
        }

        #endregion

        #region Protected Methods

        protected IMongoDatabase Connection { get; private set; }
        protected IQueryable<T> Query() => _context.Query<T>();

        #endregion

        #region Public Methods
        public IQueryable<T> Where(Expression<Func<T, bool>> where) => _context.Query<T>().Where(where);
        #endregion

        #region Sync Methods
        
        public T FirstOrDefault(Expression<Func<T, bool>> where) => _context.Query<T>().FirstOrDefault(where);
        public bool Any(Expression<Func<T, bool>> where) => _context.Query<T>().Any(where);

        public void Delete(T Subject)
        {
            PropertyInfo id = GetId();
            if (id != null) {
                var filter = Builders<T>.Filter.Eq(id.Name, id.GetValue(Subject));
                _context.Collection<T>().DeleteOne(filter);
            }
        }
        public void Delete(Expression<Func<T, bool>> where) => _context.Collection<T>().DeleteMany(where);

        public void Save(T subject)
        {
            PropertyInfo id = GetId();
            if (id != null)
                InsertOrReplace(subject, id);
        }
        public void SaveRange(T[] subjects)
        {
            PropertyInfo id = GetId();
            if (id != null)
                foreach (T subject in subjects)
                    InsertOrReplace(subject, id);
        }

        public void Set<U>(Expression<Func<T, bool>> where, U subdocument, string subdocname)
        {
            var filter = Builders<T>.Filter.Where(where);
            var update = Builders<T>.Update.Set(
                            (string.IsNullOrEmpty(subdocname) ? nameof(subdocument) : subdocname),
                            subdocument
                        );

            _context.Collection<T>().UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
        }
        public void Set<U>(T subject, U subdocument, string subdocname)
        {
            PropertyInfo id = GetId();
            if (id != null) {
                var value = id.GetValue(subject);
                if (!IsNew(id, value)) {
                    var filter = Builders<T>.Filter.Eq(id.Name, value);
                    var update = Builders<T>.Update.Set(
                                    (string.IsNullOrEmpty(subdocname) ? nameof(subdocument) : subdocname),
                                    subdocument
                                );

                    _context.Collection<T>().UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
                }
            }
        }
        
        public IEnumerable<T> List(Expression<Func<T, bool>> where, int pageSize, int page)
        {
            if (pageSize <= 0)
                return _context.Query<T>().Where(where).ToList();

            var skip = (page <= 0) ? 0 : (page - 1) * pageSize;
            return _context.Query<T>()
                .Where(where)
                .Skip(skip)
                .Take(pageSize)
                .ToList();
        }
        
        public IList<T> All() => _context.Query<T>().ToList();
        
        #endregion
        
        #region Async Methods
        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> where) => await ((IMongoQueryable<T>)_context.Query<T>()).FirstOrDefaultAsync(where);
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> where) => await ((IMongoQueryable<T>)_context.Query<T>()).AnyAsync(where);
        public async Task DeleteAsync(T Subject)
        {
            PropertyInfo id = GetId();
            if (id != null) {
                var filter = Builders<T>.Filter.Eq(id.Name, id.GetValue(Subject));
                await _context.Collection<T>().DeleteOneAsync(filter);
            }
        }
        public async Task DeleteAsync(Expression<Func<T, bool>> where) => await _context.Collection<T>().DeleteManyAsync(where);
        public async Task SaveAsync(T subject)
        {
            PropertyInfo id = GetId();
            if (id != null)
                await InsertOrReplaceAsync(subject, id);
        }
        public async Task SaveRangeAsync(T[] subjects)
        {
            PropertyInfo id = GetId();
            if (id != null)
                foreach (T subject in subjects)
                    await InsertOrReplaceAsync(subject, id);
        }
        public async Task SetAsync<U>(Expression<Func<T, bool>> where, U subdocument, string subdocname)
        {
            var filter = Builders<T>.Filter.Where(where);
            var update = Builders<T>.Update.Set(
                            (string.IsNullOrEmpty(subdocname) ? nameof(subdocument) : subdocname),
                            subdocument
                        );

            await _context.Collection<T>().UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }
        public async Task SetAsync<U>(T subject, U subdocument, string subdocname)
        {
            PropertyInfo id = GetId();
            if (id != null) {
                var value = id.GetValue(subject);
                if (!IsNew(id, value)) {
                    var filter = Builders<T>.Filter.Eq(id.Name, value);
                    var update = Builders<T>.Update.Set(
                                    (string.IsNullOrEmpty(subdocname) ? nameof(subdocument) : subdocname),
                                    subdocument
                                );

                    await _context.Collection<T>().UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
                }
            }
        }
        public async Task<IEnumerable<T>> List<TKey>(Expression<Func<T, bool>> where, int pageSize, int page, Expression<Func<T, TKey>> orderby, int asc = 1)
        {
            var filtered = _context.Query<T>().Where(where);
            if (pageSize <= 0)
                return (asc > 0) ?
                    filtered.OrderBy(orderby).ToList() :
                    filtered.OrderByDescending(orderby).ToList();

            var skip = (page <= 0) ? 0 : (page - 1) * pageSize;

            if (asc > 0)
                return filtered.OrderBy(orderby)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();

            return filtered.OrderByDescending(orderby)
                .Skip(skip)
                .Take(pageSize)
                .ToList();
        }
        public async Task<IList<T>> AllAsync() => await ((IMongoQueryable<T>)_context.Query<T>()).ToListAsync();

        #endregion
    }
    public interface IMgDbSet<T> where T : class
    {
        IQueryable<T> Where(Expression<Func<T, bool>> where);

        #region  Sync Methods

        T FirstOrDefault(Expression<Func<T, bool>> where);
        bool Any(Expression<Func<T, bool>> where);
        void Delete(T Subject);
        void Delete(Expression<Func<T, bool>> where);
        void Save(T subject);
        void SaveRange(T[] subjects);
        void Set<U>(Expression<Func<T, bool>> where, U subdocument, string subdocname);
        void Set<U>(T subject, U subdocument, string subdocname);
        
        IEnumerable<T> List(Expression<Func<T, bool>> where, int pageSize, int page);
        
        IList<T> All();

        #endregion

        #region Async Methods

        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> where);
        Task<bool> AnyAsync(Expression<Func<T, bool>> where);
        Task DeleteAsync(T Subject);
        Task DeleteAsync(Expression<Func<T, bool>> where);
        Task SaveAsync(T subject);
        Task SaveRangeAsync(T[] subjects);
        Task SetAsync<U>(Expression<Func<T, bool>> where, U subdocument, string subdocname);
        Task SetAsync<U>(T subject, U subdocument, string subdocname);
        
        Task<IEnumerable<T>> List<TKey>(Expression<Func<T, bool>> where, int pageSize, int page, Expression<Func<T, TKey>> orderby, int asc = 1);
        
        Task<IList<T>> AllAsync();
        #endregion
    }
}
