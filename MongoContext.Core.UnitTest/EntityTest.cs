using MongoDB.Bson;
using System;

namespace MongoContext.Core.UnitTest
{
    [Collection(Name = "EntitiesTest")]
    public class EntityTest
    {
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public DateTime Date { get; set; }
        public Decimal Dec { get; set; }
    }
}
