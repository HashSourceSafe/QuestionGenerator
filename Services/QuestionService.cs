using QuestionGeneratorWithCohere.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace QuestionGeneratorWithCohere.Services
{
    public class QuestionService
    {
        private readonly string _connectionString;

        public QuestionService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task InsertQuestionsAsync(QuestionResponse response)
        {
            // Convert the QuestionResponse object to JSON
            var jsonData = JsonSerializer.Serialize(response);

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("InsertQuestionsFromJson", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Pass the JSON as a parameter to the stored procedure
                    command.Parameters.AddWithValue("@Json", jsonData);

                    connection.Open();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
