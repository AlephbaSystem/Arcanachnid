using Arcanachnid.Models;
using Neo4j.Driver;

namespace Arcanachnid.Database.Drivers
{
    public class Neo4jDriver
    {
        private readonly IDriver _driver;

        public Neo4jDriver(string uri, string user, string password)
        {
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        }

        public async Task AddModelAsync(GraphNode model)
        {
            var session = _driver.AsyncSession();
            try
            {
                await session.WriteTransactionAsync(async transaction =>
                {
                    var result = await transaction.RunAsync($@"
                    CREATE (post:Post {{ 
                        title: $title, 
                        category: $category, 
                        body: $body, 
                        url: $url, 
                        postId: $postId, 
                        date: $date 
                    }})

                    WITH post
                    UNWIND $tags AS tag
                    MERGE (t:Tag {{ name: tag }})
                    MERGE (post)-[:TAGGED_WITH]->(t)

                    WITH post
                    UNWIND $references AS reference
                    MERGE (r:Reference {{ id: reference.Item1, url: reference.Item2 }})
                    MERGE (post)-[:REFERENCES]->(r)
                    ", new
                    {
                        title = model.Title,
                        category = model.Category,
                        body = model.Body,
                        url = model.Url,
                        postId = model.PostId,
                        date = model.Date,
                        tags = model.Tags,
                        references = model.References
                    });
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}
