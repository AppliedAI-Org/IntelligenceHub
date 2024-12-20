using IntelligenceHub.API.DTOs;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.DAL.Implementations
{
    //make this more generic
    public class ProfileToolsAssociativeRepository : IAssociativeRepository<DbProfileTool>, IProfileToolsAssociativeRepository
    {
        private readonly string _connectionString;

        public ProfileToolsAssociativeRepository(IOptionsMonitor<Settings> settings)
        {
            _connectionString = settings.CurrentValue.DbConnectionString;
        }

        public async Task<List<DbProfileTool>> GetToolAssociationsAsync(int profileId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"SELECT * FROM profileTools WHERE ProfileId = @ProfileId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        // Assuming there is an Id property for the WHERE clause
                        command.Parameters.AddWithValue("@ProfileId", profileId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var associations = new List<DbProfileTool>();
                            while (await reader.ReadAsync())
                            {
                                associations.Add(MapAssociationsFromReader(reader));
                            }
                            return associations;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> AddAssociationsByProfileIdAsync(int profileId, List<int> toolList)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    foreach (var toolId in toolList)
                    {
                        var query = @"
                            IF @ProfileID IS NOT NULL AND NOT EXISTS (
                                SELECT 1
                                FROM profileTools
                                WHERE ProfileID = @ProfileID
                                AND ToolID = @ToolID
                            )
                            BEGIN
                                INSERT INTO profileTools (ProfileID, ToolID) 
                                VALUES (@ProfileID, @ToolID)
                            END";

                        using (var command = new SqlCommand(query, connection))
                        {

                            command.Parameters.AddWithValue("@ToolID", toolId);
                            command.Parameters.AddWithValue("@ProfileID", profileId);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AddAssociationsByToolIdAsync(int toolId, List<string> profileList)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // move this foreach out for increased versatility
                    foreach (var name in profileList)
                    {
                        // check if association exists, then checks if profile exists
                        var queryCheckExistence = @"
                            DECLARE @ProfileID int
                            SET @ProfileID = (SELECT Id FROM profiles WHERE [Name] = @Name)

                            -- If profile exists, check if association does not exist
                            IF @ProfileID IS NOT NULL AND NOT EXISTS (
                                SELECT 1
                                FROM profileTools
                                WHERE ProfileID = @ProfileID
                                AND ToolID = @ToolID
                            )
                            BEGIN
                                INSERT INTO profileTools (ProfileID, ToolID) 
                                VALUES (@ProfileID, @ToolID)
                            END";

                        using (var commandCheckExistence = new SqlCommand(queryCheckExistence, connection))
                        {
                            commandCheckExistence.Parameters.AddWithValue("@Name", name);
                            commandCheckExistence.Parameters.AddWithValue("@ToolID", toolId);

                            await commandCheckExistence.ExecuteNonQueryAsync();
                        }
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> DeleteToolAssociationAsync(int toolId, string name) // change to delete all associations by tool
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"     
                        DECLARE @ProfileID int;
                        SET @ProfileID = (SELECT Id FROM profiles WHERE [Name] = @Name);
                        IF @ProfileID IS NOT NULL
                        BEGIN
                            DELETE FROM profileTools
                            WHERE ToolID = @ToolID
                            AND ProfileID = @ProfileID;
                        END";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ToolID", toolId);
                        command.Parameters.AddWithValue("@Name", name);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> DeleteProfileAssociationAsync(int profileID, string name) // change to delete all associations by tool
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"     
                        DECLARE @ToolID int;
                        SET @ToolID = (SELECT Id FROM tools WHERE [Name] = @Name);
                        IF @ToolID IS NOT NULL
                        BEGIN
                            DELETE FROM profileTools
                            WHERE ProfileID = @ProfileID
                            AND ToolID = @ToolID;
                        END";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProfileID", profileID);
                        command.Parameters.AddWithValue("@Name", name);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> DeleteAllProfileAssociationsAsync(int profileId) // change to delete all associations by profile
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"DELETE FROM profileTools WHERE ProfileId = @ProfileId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        // Assuming there is an Id property for the WHERE clause
                        command.Parameters.AddWithValue("@ProfileId", profileId);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> DeleteAllToolAssociationsAsync(int toolId) // change to delete all associations by profile
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"DELETE FROM profileTools WHERE ToolID = @ToolID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        // Assuming there is an Id property for the WHERE clause
                        command.Parameters.AddWithValue("@ToolID", toolId);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private DbProfileTool MapAssociationsFromReader(SqlDataReader reader)
        {
            var entity = new DbProfileTool();
            foreach (var property in typeof(DbProfileTool).GetProperties())
            {
                var columnName = property.Name;
                var value = reader[columnName];
                if (value != DBNull.Value)
                {
                    property.SetValue(entity, value);
                }
            }
            return entity;
        }
    }
}