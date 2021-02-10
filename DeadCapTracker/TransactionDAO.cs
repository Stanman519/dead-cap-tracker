using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeadCapTracker.Models.MFL;
using Npgsql;

namespace DeadCapTracker
{
    public class TransactionDAO
    {
        private string _connectionString;

        public TransactionDAO()
        {
            _connectionString =  "Host=localhost;Username=postgres;Password=rasras19;Database=DeadCapTracker";
        }

        public async Task<int> GetLatestTransactionId()
        {
            int latestId = 0;
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using (var cmd = new NpgsqlCommand("select max(transactionid) from transaction", conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    latestId = (reader.GetInt32(0));
                }
            return latestId;
        }
        
        public async Task<List<int>> GetExistingFranchiseIds()
        {
            var franchiseIds = new List<int>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using (var cmd = new NpgsqlCommand("select franchiseid from franchise", conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    franchiseIds.Add(reader.GetInt32(0));
                }
            return franchiseIds;
        }

        /*public async Task AddTransactions(List<TransactionDTO> filteredTransactions)
        {
            foreach (var t in filteredTransactions)
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using (var cmd = new NpgsqlCommand(
                    "insert into transaction (transactionid, franchiseid, amount, playername, position, team, years, salary, timestamp, yearoftransaction) " +
                    "VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10)", conn))
                {
                    cmd.Parameters.AddWithValue("p1", t.TransactionId);
                    cmd.Parameters.AddWithValue("p2", t.FranchiseId);
                    cmd.Parameters.AddWithValue("p3", t.Amount);
                    cmd.Parameters.AddWithValue("p4", t.PlayerName);
                    cmd.Parameters.AddWithValue("p5", t.Position);
                    cmd.Parameters.AddWithValue("p6", t.Team);
                    cmd.Parameters.AddWithValue("p7", t.Years);
                    cmd.Parameters.AddWithValue("p8", t.Salary);
                    cmd.Parameters.AddWithValue("p9", t.Timestamp);
                    cmd.Parameters.AddWithValue("p10", t.YearOfTransaction);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task AddFranchises(List<FranchiseDTO> newFranchises)
        {
            foreach (var f in newFranchises)
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using (var cmd = new NpgsqlCommand(
                    "insert into franchise (franchiseid, icon, bbidavailablebalance, ownername, abbrev, teamname) " +
                    "VALUES (@p1, @p2, @p3, @p4, @p5, @p6)", conn))
                {
                    cmd.Parameters.AddWithValue("p1", f.FranchiseId);
                    cmd.Parameters.AddWithValue("p2", f.Icon);
                    cmd.Parameters.AddWithValue("p3", f.BBidAvailableBalance);
                    cmd.Parameters.AddWithValue("p4", f.Ownername);
                    cmd.Parameters.AddWithValue("p5", f.Abbrev);
                    cmd.Parameters.AddWithValue("p6", f.Teamname);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }*/



        public async Task<IEnumerable<TransactionDTO>> GetExistingTransactions()
        {
            var existing = new List<TransactionDTO>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

// Insert some data
/*            await using (var cmd = new NpgsqlCommand("Select * from public.\"Transaction\"", conn))
            {
                cmd.Parameters.AddWithValue("p", "Hello world");
                await cmd.ExecuteNonQueryAsync();
            }
*/
// Retrieve all rows
            await using (var cmd = new NpgsqlCommand("Select * from transaction", conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    var x = new TransactionDTO();
                    x.PlayerName = (reader.GetString(5));
                    x.Position = (reader.GetString(6));
                    Console.WriteLine(x.PlayerName);
                    existing.Add(x);
                }

            return existing;
        }
    }
}