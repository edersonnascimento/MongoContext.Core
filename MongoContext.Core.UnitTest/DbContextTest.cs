using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoContext.Core.UnitTest
{
    internal class DbContextTest : MgDbContext
    {
        private readonly List<EntityTest> _list = new List<EntityTest>();
        public DbContextTest(IMongoDatabase database) : base(database) { }

        public override IMongoCollection<T> Collection<T>()
        {
            return (IMongoCollection<T>)Mocks.GetMongoCollection(_list);
        }
        public override IQueryable<T> Query<T>()
        {
            if (typeof(T) == typeof(EntityTest))
                return (IQueryable<T>)_list.AsQueryable();

            return base.Query<T>();
        }

        public MgDbSet<EntityTest> Entities { get; set; }

        private Mock<IMongoClient> CreateMockClient()
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
    }
}
