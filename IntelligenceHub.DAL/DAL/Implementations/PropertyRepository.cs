using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.DAL.Implementations
{
    public class PropertyRepository : GenericRepository<DbProperty>, IPropertyRepository
    {
        public PropertyRepository(IOptionsMonitor<Settings> settings) : base(settings.CurrentValue.DbConnectionString)
        {
        }

        public async Task<IEnumerable<DbProperty>> GetToolProperties(int Id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                            SELECT * FROM {_table}
                            WHERE ToolId = @Id";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("Id", Id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var propertyList = new List<DbProperty>();
                            while (await reader.ReadAsync())
                            {
                                propertyList.Add(MapFromReader<DbProperty>(reader));
                            }
                            return propertyList;
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task AddAsync(DbProperty property)
        {
            await base.AddAsync(property);
        }

        public async Task DeleteAsync(DbProperty property)
        {
            await base.DeleteAsync(property);
        }

        public async Task<DbProperty> UpdatePropertyAsync(DbProperty existingEntity, DbProperty entity)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var setClause = string.Join(", ", typeof(DbProperty).GetProperties()
                        .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "ToolId") // Exclude Id and Name properties
                        .Select(p => $"{p.Name} = @{p.Name}"));

                    var query = $@"
                            UPDATE {_table} SET {setClause} 
                            WHERE Name = @Name
                            AND ToolID = @ToolID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        foreach (var property in typeof(DbProperty).GetProperties())
                        {
                            // Exclude Id and Name properties from being updated
                            if (property.Name != "Id" && property.Name != "Name" && property.Name != "ToolId")
                            {
                                var paramName = $"@{property.Name}";
                                var value = property.GetValue(entity) ?? DBNull.Value;
                                command.Parameters.AddWithValue(paramName, value);
                            }
                        }

                        command.Parameters.AddWithValue("@Name", existingEntity.Name);
                        command.Parameters.AddWithValue("@ToolID", existingEntity.ToolId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapFromReader<DbProperty>(reader);
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}