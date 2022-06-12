using NUnit.Framework;

namespace MongoContext.Core.UnitTest
{
    internal class MgDbContextTest
    {
        IMgDbContext _context = Mocks.GetDbContext();

        [Test]
        public void DbContext_HasConnection()
        {
            var dataBase = _context.GetConnection();
            Assert.That(dataBase, Is.Not.Null);
        }

        [Test]
        public void DbContext_DbSetNotNull()
        {
            var dbSet = _context.GetDbSet<EntityTest>(typeof(EntityTest).Name);
            Assert.That(dbSet, Is.Not.Null);
        }

        [Test]
        public void DbContext_DbSetNull()
        {
            var dbSet = _context.GetDbSet<EntityTest>("QualquerNome");
            Assert.That(dbSet, Is.Null);
        }

        [Test]
        public void DbContext_GetBsonClassMap()
        {
            var classMap = _context.GetRegisteredClassMap<EntityTest>();
            Assert.IsNotNull(classMap);
            Assert.AreSame(typeof(EntityTest), classMap.ClassType);
        }

        [Test]
        public void DbContext_GetCollection()
        {
            var collection = _context.Collection<EntityTest>();

            Assert.IsNotNull(collection);
        }

        [Test]
        public void DbContext_GetQueryable()
        {
            var queryable = _context.Query<EntityTest>();

            Assert.IsNotNull(queryable);
        }
    }
}
