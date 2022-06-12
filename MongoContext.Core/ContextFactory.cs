using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoContext.Core
{
    public class ContextFactory<T> where T : MgDbContext
    {
        public static T Create(string connectionString, string databaseName) =>
            (T)Activator.CreateInstance(typeof(T), new[] { GetMongoDatabase(connectionString, databaseName) });

        public static IMongoDatabase GetMongoDatabase(string connectionString, string databaseName)
        {
            var mongoUrl = new MongoUrl(connectionString);

            var mongoSettings = MongoClientSettings.FromUrl(mongoUrl);
            mongoSettings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e => Debug.WriteLine($"{e.CommandName} - {e.Command.ToJson()}"));
            };
            var mongoClient = new MongoClient(mongoSettings);

            return mongoClient.GetDatabase(databaseName ?? mongoUrl.DatabaseName);
        }
    }
}
