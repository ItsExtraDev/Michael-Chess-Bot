namespace Michael
{
    /// <summary>
    /// LogWriter handles writing log messages to files for debugging and analysis.
    /// Logs can be categorized by type (e.g., UCI commands, search info) and saved
    /// to the user's LocalApplicationData folder under a "Michael" subfolder.
    /// </summary>
    public class LogWriter
    {
        // --- Properties ---

        /// <summary>
        /// Determines which type of log file to write to.
        /// </summary>
        private FileType fileType { get; set; } = FileType.None;

        /// <summary>
        /// Whether writing to files is enabled.
        /// </summary>
        private bool writeToFiles { get; set; }

        /// <summary>
        /// Initializes a new instance of LogWriter.
        /// </summary>
        /// <param name="fileType">The type of log file.</param>
        /// <param name="writeToFiles">Whether logging to files is enabled.</param>
        public LogWriter(FileType fileType, bool writeToFiles)
        {
            this.fileType = fileType;
            this.writeToFiles = writeToFiles; // fixed: assign parameter instead of always false
        }

        /// <summary>
        /// Writes a line of text to the log file if writing is enabled.
        /// Automatically creates the directory if it doesn't exist.
        /// </summary>
        /// <param name="text">The message to write.</param>
        public void WriteToFile(string text)
        {
            if (writeToFiles && fileType != FileType.None)
            {
                Directory.CreateDirectory(AppDataPath); // ensure directory exists
                string path = Path.Combine(AppDataPath, GetFileName());

                using (StreamWriter writer = new StreamWriter(path, true)) // append mode
                {
                    writer.WriteLine(text);
                }
            }
        }

        /// <summary>
        /// Gets the path to the "Michael" folder in LocalApplicationData.
        /// Example: C:\Users\<User>\AppData\Local\Michael
        /// </summary>
        public static string AppDataPath
        {
            get
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(dir, "Michael");
            }
        }

        /// <summary>
        /// Determines the filename to use based on the FileType.
        /// </summary>
        /// <returns>The filename for the log.</returns>
        private string GetFileName()
        {
            return fileType switch
            {
                FileType.UCI => "UCI_log.txt",
                FileType.Search => "Search_log.txt",
                _ => ""
            };
        }
    }

    /// <summary>
    /// Enumerates the types of logs that can be written.
    /// </summary>
    public enum FileType
    {
        None,
        UCI,
        Search
    }
}
