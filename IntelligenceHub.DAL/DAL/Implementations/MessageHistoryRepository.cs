using IntelligenceHub.API.DTOs;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.DAL.Implementations
{
    public class MessageHistoryRepository : GenericRepository<DbMessage>, IMessageHistoryRepository
    {
        public MessageHistoryRepository(IOptionsMonitor<Settings> settings) : base(settings.CurrentValue.DbConnectionString)
        {
        }

        public async Task<List<Message>> GetConversationAsync(Guid conversationId, int maxMessages)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT TOP(@MaxMessages) *
                        FROM MessageHistory
                        WHERE [ConversationId] = @ConversationId
                        ORDER BY timestamp ASC;";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaxMessages", maxMessages);
                        command.Parameters.AddWithValue("@ConversationId", conversationId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var conversationHistory = new List<Message>();
                            while (await reader.ReadAsync())
                            {
                                var dbMessage = MapFromReader<DbMessage>(reader);
                                var mappedMessage = DbMappingHandler.MapFromDbMessage(dbMessage);
                                conversationHistory.Add(mappedMessage);
                            }

                            // Reorder the messages in ascending order by timestamp
                            return conversationHistory.OrderBy(m => m.TimeStamp).ToList();
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> DeleteConversationAsync(Guid conversationId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"
                        DELETE FROM MessageHistory 
                        WHERE [ConversationId] = @ConversationId;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ConversationId", conversationId);
                        var response = await command.ExecuteNonQueryAsync();
                        if (response > 0) return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> DeleteMessageAsync(Guid conversationId, int messageId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        DELETE FROM MessageHistory 
                        WHERE [ConversationId] = @ConversationId AND [Id] = @MessageId;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ConversationId", conversationId);
                        command.Parameters.AddWithValue(@"MessageId", messageId);
                        var response = await command.ExecuteNonQueryAsync();
                        if (response > 0) return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<DbMessage> AddAsync(DbMessage document, string tableName = null)
        {
            return await base.AddAsync(document, tableName);
        }
    }
}