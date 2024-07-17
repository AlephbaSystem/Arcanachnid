using Arcanachnid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace Arcanachnid.Database.Drivers
{
    internal class MySqlDriver : IDisposable
    {
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private readonly MySqlConnection _connection;

        public MySqlDriver(string connectionString)
        {
            _connection = new MySqlConnection(connectionString);
            _connection.Open();
        }

        public async Task AddOrUpdateModelAsync(GraphNode model)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                model.Body = model.Body.Replace("│", "");
                model.Body = model.Body.Replace("┴", "");
                model.Body = model.Body.Replace("└", "");
                model.Body = model.Body.Replace("┘", "");
                model.Body = model.Body.Replace("┤", "");
                model.Body = model.Body.Replace("├", "");
                model.Body = model.Body.Replace("┼", "");
                model.Body = model.Body.Replace("┬", "");
                model.Body = model.Body.Replace("┌", "");
                model.Body = model.Body.Replace("─", "");
                model.Body = model.Body.Replace("┐", "");
                model.Body = model.Body.Replace("\n", " ");
                model.Body = model.Body.Replace("\r", " ");
                model.Category = string.IsNullOrWhiteSpace(model.Category) ? "نامشخص" : model.Category.Trim();
                model.Tags = model.Tags?.Count > 0 ? model.Tags.Select(x => x.Trim()).ToList() : new List<string>() { "نامشخص" };
                model.References = model.References?.Select(x => (x.Item1.Trim(), x.Item2.Trim())).ToList() ?? new List<(string, string)>();
                model.Title = model.Title.Trim();
                model.Body = model.Body.Trim();
                model.Url = model.Url.Trim();

                var command = _connection.CreateCommand();
                command.CommandText = @"
                INSERT INTO posts (url, title, body, post_id, category, date)
                VALUES (@url, @title, @body, @postId, @category, @date);
                ";

                command.Parameters.AddWithValue("@url", model.Url);
                command.Parameters.AddWithValue("@title", model.Title);
                command.Parameters.AddWithValue("@body", model.Body);
                command.Parameters.AddWithValue("@postId", model.PostId);
                command.Parameters.AddWithValue("@category", model.Category);
                command.Parameters.AddWithValue("@date", model.Date.ToString("yyyy-MM-dd"));

                await command.ExecuteNonQueryAsync();
                //command.Parameters.Clear();
                //foreach (var tag in model.Tags)
                //{
                //    command.CommandText = "INSERT IGNORE INTO tags (name) VALUES (@name);";
                //    command.Parameters.Clear();
                //    command.Parameters.AddWithValue("@name", tag);
                //    await command.ExecuteNonQueryAsync();
                //    command.Parameters.Clear();
                //}

                //foreach (var reference in model.References)
                //{
                //    command.CommandText = "INSERT INTO references (id, url) VALUES (@id, @url) ON DUPLICATE KEY UPDATE url = VALUES(url);";
                //    command.Parameters.Clear();
                //    command.Parameters.AddWithValue("@id", reference.Item1);
                //    command.Parameters.AddWithValue("@url", reference.Item2);
                //    await command.ExecuteNonQueryAsync();
                //    command.Parameters.Clear();
                //}

            }
            catch (Exception ex)
            {
                _ = ex;
            }
            finally
            {
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}