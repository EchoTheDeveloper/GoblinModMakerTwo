using System.Diagnostics;
using System;
using System.IO;

namespace GMMBackend {
    public class GitManagement {
        public static string GenGitKey(string email)
        {
            string path = $"{GMMBackend.Utils.GetAppDataPath()}\\GitSSHKey";
            string pubKeyPath = path + ".pub";

            var psi = new ProcessStartInfo
            {
                FileName = "ssh-keygen",
                Arguments = $"-t ed25519 -f \"{path}\" -C \"{email}\" -N \"\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process.WaitForExit();

            string error = process.StandardError.ReadToEnd();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("SSH key generated successfully!");
                if (File.Exists(pubKeyPath))
                {
                    return File.ReadAllText(pubKeyPath);
                }
                else
                {
                    Console.WriteLine("Public key file not found.");
                    return string.Empty;
                }
            }
            else
            {
                Console.WriteLine($"Error: {error}");
                return string.Empty;
            }
        }

    }
}