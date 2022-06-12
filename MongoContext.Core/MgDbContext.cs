using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoContext.Core
{

    /// <summary>
    /// Created by: Éderson Curto do Nascimento
    /// Created at: 2017-01-28
    /// E-mail: ederson.nascimento@outlook.com
    /// </summary>
    public abstract class MgDbContext : IMgDbContext
    {
        private static object _mapLocker = new object();
        private static IList<Type> mappings = new List<Type>();

        protected readonly IMongoDatabase _database;
        protected readonly Dictionary<string, object> _dbSetDictionary = new Dictionary<string, object>();

        /// <summary>
        /// Context abstract constructor
        /// </summary>
        /// <param name="connectionString">MongoDb Connection string with format: "mongodb://<UserName>:<Password>@<ServerAddress>:<ServerPort>"</param>
        /// <param name="databaseName">Database name</param>
        public MgDbContext(IMongoDatabase database)
        {
            _database = database;
            CreateMapping();
            CreateDbSetProperties();
        }


        #region Private Methods

        private static string GetName<T>()
        {
            CollectionAttribute attr = typeof(T).GetTypeInfo()
                                                .GetCustomAttributes(false)
                                                .Where(a => a is CollectionAttribute)
                                                .OfType<CollectionAttribute>()
                                                .FirstOrDefault();
            return attr == null ? typeof(T).Name : attr.Name;
        }
        private void CreateDbSetProperties()
        {
            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.PropertyType.Name.IndexOf("MgDbSet") >= 0)
                {
                    Type classType = typeof(MgDbSet<>);
                    Type constructedType = classType.MakeGenericType(property.PropertyType.GenericTypeArguments);

                    object dbset = Activator.CreateInstance(constructedType, new object[] { this });
                    property.SetValue(this, dbset);

                    var typeName = property.PropertyType.GenericTypeArguments.FirstOrDefault()?.Name;
                    if (!string.IsNullOrWhiteSpace(typeName) && !_dbSetDictionary.ContainsKey(typeName))
                    {
                        _dbSetDictionary.Add(typeName, dbset);
                    }
                }
            }
        }
        private void CreateMapping()
        {
            var type = GetType();
            lock (_mapLocker)
            {
                if (!mappings.Contains(type))
                {
                    mappings.Add(type);

                    Type implementedMapping = Assembly.GetAssembly(type)
                        .GetTypes()
                        .FirstOrDefault(t => t.IsClass && t.BaseType.GenericTypeArguments.Any(g => g.Name == type.Name));
                    if (implementedMapping == null)
                    {
                        Type classType = typeof(MgDbMapping<>);
                        Type constructedType = classType.MakeGenericType(type);
                        var map = (IMgDbMapping)Activator.CreateInstance(constructedType);
                        map.Register();
                    }
                    else
                    {
                        var map = (IMgDbMapping)Activator.CreateInstance(implementedMapping);
                        map.Register();
                    }

                }
            }
        }

        #endregion

        #region Protected
        protected virtual IMongoDatabase GetMongoDatabase() => _database;
        #endregion

        #region IMgDbContext

        public virtual IMongoDatabase GetConnection() => GetMongoDatabase();
        public virtual IMgDbSet<T> GetDbSet<T>(string name) where T : class
        {
            if (_dbSetDictionary.ContainsKey(name))
            {
                return (MgDbSet<T>)_dbSetDictionary[name];
            }
            return default;
        }

        public virtual BsonClassMap GetRegisteredClassMap<T>() => BsonClassMap.GetRegisteredClassMaps().FirstOrDefault(r => r.ClassType == typeof(T));
        public virtual IMongoCollection<T> Collection<T>()
        {
            var db = this.GetConnection();
            var classMap = MgDbMap.GetClassMap(typeof(T));
            var collection = typeof(T).GetTypeInfo()
                                    .GetCustomAttributes(true)
                                    .Where(a => a is CollectionAttribute)
                                    .FirstOrDefault();

            string name = classMap?.Collection;
            name = collection == null ? name : (collection as CollectionAttribute).Name;
            name = name ?? GetName<T>();

            return db.GetCollection<T>(name);
        }
        public virtual IQueryable<T> Query<T>()
        {
            var classmap = GetRegisteredClassMap<T>();
            bool hasDiscriminator = typeof(T).GetTypeInfo()
                                            .GetCustomAttributes(false)
                                            .Where(a => a is BsonDiscriminatorAttribute)
                                            .Count() > 0;

            if (hasDiscriminator || classmap != null && classmap.DiscriminatorIsRequired)
                return Collection<T>().OfType<T>().AsQueryable();

            return Collection<T>().AsQueryable();
        }

        #endregion
    }
    public interface IMgDbContext
    {
        IMongoDatabase GetConnection();
        IMgDbSet<T> GetDbSet<T>(string name) where T : class;

        IQueryable<T> Query<T>();
        BsonClassMap GetRegisteredClassMap<T>();
        IMongoCollection<T> Collection<T>();
    }
}
