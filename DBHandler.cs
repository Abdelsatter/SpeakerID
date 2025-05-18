using System;
using System.Collections.Generic;
using System.Data.SQLite;
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


        public static Dictionary<string, List<Sequence>> GetAllAudioFiles()
        {
            var result = new Dictionary<string, List<Sequence>>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var records = connection.Query("SELECT user_name, features FROM audio_file");

                foreach (var record in records)
                {
                    string userName = record.user_name;
                    byte[] featuresBytes = record.features;
                    string json = Encoding.UTF8.GetString(featuresBytes);
                    Sequence sequence = JsonConvert.DeserializeObject<Sequence>(json);

                    if (!result.ContainsKey(userName))
                        result[userName] = new List<Sequence>();

                    result[userName].Add(sequence);
                }
            }

            return result;
        }
        public static void PrintUserSequenceCounts()
        {
            var userSequences = GetAllAudioFiles();

            Console.WriteLine("---------------------------------");
            Console.WriteLine($"{"User Name",-20} | {"Sequence Count",15}");
            Console.WriteLine("---------------------------------");

            foreach (var kvp in userSequences)
            {
                string userName = kvp.Key;
                int count = kvp.Value.Count;
                Console.WriteLine($"{userName,-20} | {count,15}");
            }

            Console.WriteLine("---------------------------------");
        }

    }
}