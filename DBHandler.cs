using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using Dapper;
using Newtonsoft.Json;
using Recorder.MFCC;

namespace Recorder
{
    public class DBHandler
    {
        private static readonly string connectionString = "Data Source=templates.db;Version=3;";
        private static readonly string createUserTable = @"
        CREATE TABLE IF NOT EXISTS user (
            name TEXT PRIMARY KEY
        )";
        private static readonly string createAudioFileTable = @"
        CREATE TABLE IF NOT EXISTS audio_file (
            user_name TEXT,
            features BLOB,
            FOREIGN KEY (user_name) REFERENCES user(name)
        )";

        public static void CreateTables()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                connection.Execute(createUserTable);
                connection.Execute(createAudioFileTable);
            }
        }

        public static void ResetTables()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    connection.Execute("DELETE FROM audio_file", transaction: transaction);
                    connection.Execute("DELETE FROM user", transaction: transaction);

                    transaction.Commit();
                }

                connection.Execute("VACUUM");
            }

            Console.WriteLine("All tables have been reset and database vacuumed.");
        }

        public static void InsertBulkUserAndAudio(ConcurrentBag<KeyValuePair<string, Sequence>> dataBag)
        {
            if (dataBag == null || dataBag.IsEmpty)
                throw new ArgumentException("dataBag cannot be null or empty.");

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    while (dataBag.TryTake(out var item))
                    {
                        string userName = item.Key;
                        Sequence features = item.Value;

                        if (string.IsNullOrEmpty(userName) || features == null)
                            continue; // skip invalid data

                        string serializedFeatures = JsonConvert.SerializeObject(features);
                        byte[] featuresBytes = Encoding.UTF8.GetBytes(serializedFeatures);

                        connection.Execute(
                            "INSERT OR IGNORE INTO user (name) VALUES (@Name)",
                            new { Name = userName },
                            transaction
                        );

                        connection.Execute(
                            "INSERT INTO audio_file (user_name, features) VALUES (@UserName, @Features)",
                            new { UserName = userName, Features = featuresBytes },
                            transaction
                        );
                    }

                    transaction.Commit();
                }
            }
        }


        public static void InsertUserAndAudio(string userName, Sequence features)
        {
            if (string.IsNullOrEmpty(userName) || features == null)
                throw new ArgumentException("userName and features cannot be null or empty.");

            string serializedFeatures = JsonConvert.SerializeObject(features);
            byte[] featuresBytes = Encoding.UTF8.GetBytes(serializedFeatures);

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    connection.Execute(
                        "INSERT OR IGNORE INTO user (name) VALUES (@Name)",
                        new { Name = userName },
                        transaction
                    );

                    connection.Execute(
                        "INSERT INTO audio_file (user_name, features) VALUES (@UserName, @Features)",
                        new { UserName = userName, Features = featuresBytes },
                        transaction
                    );

                    transaction.Commit();
                }
            }
        }

        public static List<KeyValuePair<string, Sequence>> GetAllAudioFiles()
        {
            var result = new List<KeyValuePair<string, Sequence>>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var records = connection.Query("SELECT user_name, features FROM audio_file")
                        .Select((record, index) => new { record, index })
                        .ToList();

                foreach (var item in records)
                {
                    var record = item.record;
                    int i = item.index;

                    string userName = record.user_name;
                    byte[] featuresBytes = record.features;
                    string json = Encoding.UTF8.GetString(featuresBytes);
                    Sequence sequence = JsonConvert.DeserializeObject<Sequence>(json);

                    result.Add(new KeyValuePair<string, Sequence>(userName, sequence));
                }
            }

            return result;
        }

        public static void PrintUserSequenceCounts()
        {
            var userSequences = GetAllAudioFiles();
            Dictionary<string, int> freqCounts = new Dictionary<string, int>();

            Console.WriteLine("---------------------------------");
            Console.WriteLine($"{"User Name",-20} | {"Sequence Count",15}");
            Console.WriteLine("---------------------------------");

            foreach (var kvp in userSequences)
            {
                if (!freqCounts.ContainsKey(kvp.Key))
                    freqCounts[kvp.Key] = 0;

                freqCounts[kvp.Key]++;
            }

            foreach (var user in freqCounts)
            {
                string userName = user.Key;
                int count = user.Value;
                Console.WriteLine($"{userName,-20} | {count,15}");
            }
            Console.WriteLine("---------------------------------");
        }
    }
}