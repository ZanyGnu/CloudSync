
namespace CloudSync.PicasaDownloader
{
    using System;
    using System.IO;

    public static class Utils
    {
        /// <summary>
        /// Removes invalid characters, and replaces them with a safe character
        /// </summary>
        /// <param name="inputString">The input string to cleanup</param>
        /// <returns>A scrubbed string safe to use as a filename or directory name.</returns>
        public static string ScrubStringForFileSystem(string inputString)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                inputString = inputString.Replace(c, '-');
            }

            return inputString;
        }

        /// <summary>
        /// Reads from the conole to retrieve a password without displaying the characters.
        /// (Handles backspace)
        /// </summary>
        /// <returns>The password</returns>
        public static string GetPassword()
        {
            string password = string.Empty;
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                    }
                }
            }

            while (key.Key != ConsoleKey.Enter);

            return password;
        }

    }
}
