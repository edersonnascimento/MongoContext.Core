using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MongoContext.Core.UnitTest
{
    internal static class Mocks
    {
        public static IMgDbContext GetDbContext() => new DbContextTest(CreateMockClient().Object.GetDatabase("DummyDatabase"));
        public static IMongoCollection<T> GetMongoCollection<T>(IList<T> subject)
        {
            var client = CreateMockClient();
            var collectionSettings = new MongoCollectionSettings();
            var mockCollection = new Mock<IMongoCollection<T>>();
            mockCollection.SetupGet(c => c.Database).Returns(client.Object.GetDatabase("DummyDatabase"));
            mockCollection.SetupGet(c => c.DocumentSerializer).Returns(BsonSerializer.SerializerRegistry.GetSerializer<T>());
            mockCollection.SetupGet(c => c.Settings).Returns(collectionSettings);
            mockCollection.SetupGet(c => c.CollectionNamespace).Returns(new CollectionNamespace("DummyDatabase", "EntityTestCollection"));
            
            SetupCollectionMock(subject, mockCollection);
            SetupCollectionMockAsync(subject, mockCollection);

            return mockCollection.Object;
        }

        private static void SetupCollectionMock<T>(IList<T> subject, Mock<IMongoCollection<T>> mockCollection)
        {
            mockCollection.Setup(c => c.InsertOne(It.IsAny<T>(), It.IsAny<InsertOneOptions>(), default(CancellationToken)))
                            .Callback((T entity, InsertOneOptions options, CancellationToken token) => subject.Add(entity));

            mockCollection.Setup(c => c.ReplaceOne(It.IsAny<FilterDefinition<T>>(), It.IsAny<T>(), It.IsAny<ReplaceOptions>(), default(CancellationToken)))
                .Callback((FilterDefinition<T> filter, T entity, ReplaceOptions options, CancellationToken token) =>
                {
                    T toRemove = GetElement(subject, entity);

                    if (toRemove != null)
                        subject.Remove(toRemove);

                    subject.Add(entity);
                });

            mockCollection.Setup(c => c.DeleteOne(It.IsAny<FilterDefinition<T>>(), default(CancellationToken)))
                .Callback((FilterDefinition<T> filter, CancellationToken token) =>
                {
                    var bson = filter.RenderToBsonDocument();
                    var id = bson.GetValue("_id");

                    var toRemove = GetElement(subject, id.AsObjectId);

                    if (toRemove != null)
                        subject.Remove(toRemove);
                });

            mockCollection.Setup(c => c.DeleteMany(It.IsAny<FilterDefinition<T>>(), default(CancellationToken)))
                .Callback((FilterDefinition<T> filter, CancellationToken token) =>
                {
                    var bson = filter.RenderToBsonDocument();
                    var id = bson.GetValue("_id");

                    var toRemove = GetElements(subject, id.AsObjectId);

                    for (var i = toRemove.Count() - 1; i >= 0; i--)
                        subject.Remove(toRemove.ElementAt(i));
                });
        }
        private static void SetupCollectionMockAsync<T>(IList<T> subject, Mock<IMongoCollection<T>> mockCollection)
        {
            mockCollection.Setup(c => c.InsertOneAsync(It.IsAny<T>(), It.IsAny<InsertOneOptions>(), default(CancellationToken)))
                            .Returns((T entity, InsertOneOptions options, CancellationToken token) => Task.Run(() => subject.Add(entity)) );

            mockCollection.Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<T>>(), It.IsAny<T>(), It.IsAny<ReplaceOptions>(), default(CancellationToken)))
                .Returns((FilterDefinition<T> filter, T entity, ReplaceOptions options, CancellationToken token) =>
                {
                    return Task.Run<ReplaceOneResult>(() => { 
                        T toRemove = GetElement(subject, entity);

                        if (toRemove != null)
                            subject.Remove(toRemove);

                        subject.Add(entity);

                        return default(ReplaceOneResult);
                    });
                });

            mockCollection.Setup(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<T>>(), default(CancellationToken)))
                .Returns((FilterDefinition<T> filter, CancellationToken token) =>
                {
                    return Task.Run<DeleteResult>(() => {
                        var bson = filter.RenderToBsonDocument();
                        var id = bson.GetValue("_id");

                        var toRemove = GetElement(subject, id.AsObjectId);

                        if (toRemove != null)
                            subject.Remove(toRemove);

                        return default(DeleteResult);
                    });
                });

            mockCollection.Setup(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<T>>(), default(CancellationToken)))
                .Returns((FilterDefinition<T> filter, CancellationToken token) =>
                {
                    return Task.Run<DeleteResult>(() => { 
                        var bson = filter.RenderToBsonDocument();
                        var id = bson.GetValue("_id");

                        var toRemove = GetElements(subject, id.AsObjectId);

                        for (var i = toRemove.Count() - 1; i >= 0; i--)
                            subject.Remove(toRemove.ElementAt(i));                    

                        return default(DeleteResult);
                    });
                });
        }
        private static BsonDocument RenderToBsonDocument<T>(this FilterDefinition<T> filter)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            return filter.Render(documentSerializer, serializerRegistry);
        }
        private static IEnumerable<T> GetElements<T>(IList<T> subject, object eid)
        {
            var id = GetId<T>();
            var toRemove = subject.Where(s =>
            {
                var sid = id.GetValue(s);
                return sid.Equals(eid);
            });
            return toRemove;
        }
        private static T GetElement<T>(IList<T> subject, object eid)
        {
            var id = GetId<T>();
            var toRemove = subject.FirstOrDefault(s =>
            {
                var sid = id.GetValue(s);
                return sid.Equals(eid);
            });
            return toRemove;
        }
        private static T GetElement<T>(IList<T> subject, T entity)
        {
            var id = GetId<T>();
            var eid = id.GetValue(entity);

            var toRemove = subject.FirstOrDefault(s =>
            {
                var sid = id.GetValue(s);
                return sid.Equals(eid);
            });
            return toRemove;
        }
        private static Mock<IMongoClient> CreateMockClient()
        {
            var mockCluster = new Mock<ICluster>();
            var clientSettings = new MongoClientSettings();

            var mockClient = new Mock<IMongoClient>();
            mockClient.SetupGet(m => m.Cluster).Returns(mockCluster.Object);
            mockClient.SetupGet(m => m.Settings).Returns(clientSettings);
            mockClient
                .Setup(m => m.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                .Returns((string databaseName, MongoDatabaseSettings settings) =>
                {
                    var mockDatabase = new Mock<IMongoDatabase>();
                    mockDatabase.SetupGet(d => d.Client).Returns(mockClient.Object);
                    mockDatabase.SetupGet(d => d.DatabaseNamespace).Returns(new DatabaseNamespace(databaseName));

                    return mockDatabase.Object;
                });

            return mockClient;
        }
        private static PropertyInfo GetId<T>()
        {
            var id = typeof(T).GetProperties()
                            .Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(BsonIdAttribute)))
                            .FirstOrDefault();
            if (id == null)
            {
                var map = GetDbContext().GetRegisteredClassMap<T>();
                if (map != null)
                {
                    id = typeof(T).GetProperties()
                        .Where(p => p.Name == map.IdMemberMap?.MemberName)
                        .FirstOrDefault();
                }

            }
            return id;
        }
    }
}
