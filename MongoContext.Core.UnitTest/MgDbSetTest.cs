using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoContext.Core.UnitTest
{
    internal class MgDbSetTest
    {
        private Random _rnd;
        IMgDbSet<EntityTest> _dbSet;

        [SetUp]
        public void Setup()
        {
            _dbSet = Mocks.GetDbContext().GetDbSet<EntityTest>("EntityTest");
            _rnd = new Random();
        }

        #region Sync

        [Test]
        public void DbSet_Save()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            _dbSet.Save(entity);

            Assert.IsTrue(_dbSet.Any(e => e.Id == entity.Id));
        }

        [Test]
        public void DbSet_Update()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            _dbSet.Save(entity);
            
            entity.Dec = 0;
            _dbSet.Save(entity);

            var rec = _dbSet.FirstOrDefault(e => e.Id == entity.Id);

            Assert.AreEqual(rec.Dec, 0);
        }

        [Test]
        public void DbSet_SaveRange()
        {
            var _list = new List<EntityTest>();
            for (int i = 0; i < 10; i++)
                _list.Add(new EntityTest
                {
                    Date = DateTime.Now.AddDays(_rnd.Next(1, 1000) * -1),
                    Dec = (decimal)_rnd.NextDouble()
                });

            _dbSet.SaveRange(_list.ToArray());

            Assert.IsTrue(_dbSet.Any(e => e.Id == _list.First().Id));
        }

        [Test]
        public void DbSet_List()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            _dbSet.Save(entity);
            var list = _dbSet.List(e => true, 100, 1);

            Assert.Greater(list.Count(), 0);
            Assert.IsTrue(list.Contains(entity));
        }

        [Test]
        public void DbSet_All()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            _dbSet.Save(entity);
            var list = _dbSet.All();

            Assert.Greater(list.Count(), 0);
            Assert.IsTrue(list.Contains(entity));
        }

        [Test]
        public void DbSet_Where()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            _dbSet.Save(entity);
            var list = _dbSet.Where(e => e.Id == entity.Id);

            Assert.Greater(list.Count(), 0);
            Assert.IsTrue(list.Contains(entity));
        }

        [Test]
        public void DbSet_Delete()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            _dbSet.Save(entity);

            _dbSet.Delete(entity);

            Assert.IsFalse(_dbSet.Any(e => e.Id == entity.Id));
        }

        [Test]
        public void DbSet_DeleteMany()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            _dbSet.Save(entity);

            _dbSet.Delete(e => e.Id == entity.Id);

            Assert.IsFalse(_dbSet.Any(e => e.Id == entity.Id));
        }

        #endregion

        #region Async

        [Test]
        public async Task DbSet_SaveSync()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            await _dbSet.SaveAsync(entity);

            Assert.IsTrue(_dbSet.Any(e => e.Id == entity.Id));
        }

        [Test]
        public async Task DbSet_UpdateAsync()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            await _dbSet.SaveAsync(entity);

            entity.Dec = 0;
            await _dbSet.SaveAsync(entity);

            var rec = _dbSet.FirstOrDefault(e => e.Id == entity.Id);

            Assert.AreEqual(rec.Dec, 0);
        }

        [Test]
        public async Task DbSet_SaveRangeAsync()
        {
            var _list = new List<EntityTest>();
            for (int i = 0; i < 10; i++)
                _list.Add(new EntityTest
                {
                    Date = DateTime.Now.AddDays(_rnd.Next(1, 1000) * -1),
                    Dec = (decimal)_rnd.NextDouble()
                });

            await _dbSet.SaveRangeAsync(_list.ToArray());

            Assert.IsTrue(_dbSet.Any(e => e.Id == _list.First().Id));
        }

        [Test]
        public async Task DbSet_DeleteAsync()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            await _dbSet.SaveAsync(entity);

            await _dbSet.DeleteAsync(entity);

            Assert.IsFalse(_dbSet.Any(e => e.Id == entity.Id));
        }

        [Test]
        public async Task DbSet_DeleteManyAsync()
        {
            var entity = new EntityTest { Date = DateTime.Now, Dec = (decimal)_rnd.NextDouble() };
            await _dbSet.SaveAsync(entity);

            await _dbSet.DeleteAsync(e => e.Id == entity.Id);

            Assert.IsFalse(_dbSet.Any(e => e.Id == entity.Id));
        }

        #endregion
    }
}
