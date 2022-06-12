using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NUnit.Framework;
using System.Linq;

namespace MongoContext.Core.UnitTest
{
    internal class MgDbMappingTest
    {

        IMgDbMapping _mapping;

        [SetUp]
        public void Setup()
        {
            _mapping = new MgDbMapping<DbContextTest>();
        }

        [Test]
        public void Mapping_MgDbMapGenerated()
        {
            _mapping.Register();
            var mgDbMap = MgDbMap.GetClassMap(typeof(EntityTest));

            Assert.IsInstanceOf(typeof(MgDbMap), mgDbMap);
        }

        [Test]
        public void Mapping_Register()
        {
            _mapping.Register();

            var mgMap = MgDbMap.GetRegisteredMaps().ToList();
            var bsonMap = BsonClassMap.GetRegisteredClassMaps().First();
            var date = bsonMap.DeclaredMemberMaps.FirstOrDefault(d => d.ElementName == "Date");
            var dec = bsonMap.DeclaredMemberMaps.FirstOrDefault(d => d.ElementName == "Dec");

            Assert.AreEqual(1, mgMap.Count);
            Assert.AreEqual("EntitiesTest", mgMap.First().Collection);
            Assert.AreSame(typeof(EntityTest), mgMap.First().ClassType);

            Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(EntityTest)));

            Assert.AreEqual(3, bsonMap.DeclaredMemberMaps.Count());
            Assert.AreSame(bsonMap.IdMemberMap, bsonMap.DeclaredMemberMaps.First());
            Assert.IsInstanceOf(typeof(DateTimeSerializer), date.GetSerializer());
            Assert.IsInstanceOf(typeof(DecimalSerializer), dec.GetSerializer());
        }
    }
}
