
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Unakin
{
    public static class HashHelper
    {
        public static string GenerateShortHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string normalizedInput = input.ToLower(); // Ensure case-insensitivity
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedInput));
                string fullHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return fullHash.Substring(0, Math.Min(16, fullHash.Length));
            }
        }


        public static string GetAppDataDirectory(string input)
        {
            // Generate the hash
            string hash = GenerateShortHash(input);

            // Get the AppData/Unakin/{hash} path
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Unakin",
                hash
            );

            // Ensure the directory exists
            Directory.CreateDirectory(appDataPath);

            return appDataPath;
        }
        public static string GetProjectPath()
        {
#if UNITY_EDITOR
            // Application.dataPath points to the "Assets" folder
            // Move one level up to get the root directory of the project
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
#else
            throw new System.InvalidOperationException("Project path is only available in the Editor.");
#endif
        }

        public static string GetUnakinProjectAppDataFolder()
        {
            string projectPath = GetProjectPath();
            string appDataDir = HashHelper.GetAppDataDirectory(projectPath);
            return appDataDir;
            //Console.WriteLine($"AppData Directory: {appDataDir}");
        }
        public static string GetUnakinProjectAppDataTempFolder()
        {
            string unakinProjectAppDataFolder = GetUnakinProjectAppDataFolder();
            string tempPath = $"{unakinProjectAppDataFolder}\\Temp\\";

            // Ensure the directory exists
            Directory.CreateDirectory(tempPath);

            return tempPath;
        }
    }
}
