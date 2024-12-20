using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.DAL.Implementations
{
    public class IndexRepository : GenericRepository<DbIndexDocument>, IIndexRepository
    {
        public IndexRepository(IOptionsMonitor<Settings> settings) : base(settings.CurrentValue.DbConnectionString)
        {
        }

        public async Task<int> GetRagIndexLengthAsync(string tableName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = $@"SELECT COUNT(*) FROM [{tableName}]";

                using (var command = new SqlCommand(query, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    return (int)result;
                }
            }
        }

        public async Task<DbIndexDocument?> GetDocumentAsync(string tableName, string title)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var sql = $@"SELECT * FROM [{tableName}] WHERE Title = @Title;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapFromReader<DbIndexDocument>(reader);
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> CreateIndexAsync(string indexName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = $@"CREATE TABLE [{indexName}] (
                                   Id INT IDENTITY(1,1) PRIMARY KEY,
                                   Title NVARCHAR(255) NOT NULL,
                                   Content NVARCHAR(MAX) NOT NULL,
                                   Topic NVARCHAR(255),
                                   Keywords NVARCHAR(255),
                                   Source NVARCHAR(510) NOT NULL,
                                   Created DATETIMEOFFSET NOT NULL,
                                   Modified DATETIMEOFFSET NOT NULL
                               );";

                using (var command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
        }

        public async Task<bool> DeleteIndexAsync(string table)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = $@"DROP TABLE IF EXISTS [{table}]";

                using (var command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
        }

        public async Task<IEnumerable<DbIndexDocument>> GetAllAsync(int count, int page)
        {
            return await base.GetAllAsync(count, page);
        }

        public async Task<DbIndexDocument> AddAsync(DbIndexDocument document, string tableName)
        {
            return await base.AddAsync(document, tableName);
        }

        public async Task<int> DeleteAsync(DbIndexDocument entity, string name)
        {
            return await base.DeleteAsync(entity, name);
        }
    }
}