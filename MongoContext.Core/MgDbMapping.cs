using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoContext.Core
{
    /// <summary>
    /// Created by: Éderson Nascimento
    /// Date: 07/02/2021
    /// Contact: ederson.nascimento@me.com.br
    /// </summary>
    public class MgDbMapping<T> : IMgDbMapping
        where T : IMgDbContext
    {
        private static Dictionary<string, Type> _mgDbSet;

        public MgDbMapping()
        {
            Register();
        }

        static MgDbMapping()
        {
            Func<PropertyInfo, bool> where = t =>
                t.PropertyType.IsGenericType && (
                    t.PropertyType.FullName.StartsWith(typeof(IMgDbSet<>).FullName)
                    || t.PropertyType.FullName.StartsWith(typeof(MgDbSet<>).FullName)
                );
            _mgDbSet = typeof(T).GetProperties()
                                .Where(where)
                                .ToDictionary(t => t.Name.ToLower(), t => t.PropertyType.GenericTypeArguments.First());
        }

        public virtual void Register()
        {
            foreach (var key in _mgDbSet.Keys)
                Map(key);
        }

        /// <summary>
        /// Maps a MgDbSet to a collection
        /// </summary>
        /// <param name="property">MgDbSet property name</param>
        protected static void Map(string property) => Map(property, null);
        /// <summary>
        /// Maps a MgDbSet to a collection with a custom Action to implement the mapping. The Action must use MgDbMap e BsonClassMap to register the class
        /// </summary>
        /// <param name="customBsonClassMap">Custom mapping Action</param>
        protected static void Map(Action customBsonClassMap)
        {
            if (customBsonClassMap == null)
                throw new ArgumentNullException(nameof(customBsonClassMap));

            customBsonClassMap();
        }
        /// <summary>
        /// Maps a MgDbSet to a collection with a given collection name and custom mapping Action
        /// </summary>
        /// <param name="property">MgDbSet property name</param>
        /// <param name="collection">Collection name</param>
        protected static void Map(string property, string collection)
        {
            property = property.ToLower();
            if (_mgDbSet.ContainsKey(property))
            {
                var type = _mgDbSet[property];

                if (string.IsNullOrWhiteSpace(collection))
                {
                    var attr = type.GetCustomAttribute(typeof(CollectionAttribute), true) as CollectionAttribute;
                    collection = attr?.Name;
                }

                MgDbMap.Register(new MgDbMap(type)
                {
                    Collection = string.IsNullOrWhiteSpace(collection) ? $"{type.Name}s" : collection
                });

                Register(type);
            }
        }


        private static void Register(Type dbset)
        {
            if (!BsonClassMap.IsClassMapRegistered(dbset))
            {
                var classMap = new BsonClassMap(dbset);
                classMap.AutoMap();
                classMap.SetIgnoreExtraElements(true);
                classMap.SetIgnoreExtraElementsIsInherited(true);

                var properties = dbset.GetProperties();
                var hasId = properties.Any(p =>
                    p.CustomAttributes.Any(a => a.AttributeType == typeof(BsonIdAttribute)));

                foreach (var property in properties)
                {
                    var member = classMap.GetMemberMap(property.Name);
                    if (member != null)
                    {
                        if (!hasId && string.Compare(property.Name, "id", StringComparison.InvariantCultureIgnoreCase) == 0)
                            classMap.SetIdMember(member);

                        if (property.PropertyType.Name == typeof(DateTime).Name)
                        {
                            member.SetSerializer(new DateTimeSerializer(DateTimeKind.Local));
                        }
                        else if (property.PropertyType.FullName == typeof(DateTime?).FullName)
                        {
                            member.SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(DateTimeKind.Local)));
                        }
                        else if (property.PropertyType.Name == typeof(decimal).Name)
                        {
                            member.SetSerializer(new DecimalSerializer(BsonType.Decimal128));
                        }
                        else if (property.PropertyType.FullName == typeof(decimal?).FullName)
                        {
                            member.SetSerializer(new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
                        }
                    }
                }

                BsonClassMap.RegisterClassMap(classMap);
            }
        }
    }
    public interface IMgDbMapping
    {
        void Register();
    }
}
