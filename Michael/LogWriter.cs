namespace Michael
{
    public class LogWriter
    {
        private FileType fileType { get; set; } = FileType.None;
        private bool writeToFiles {get; set;}

        public LogWriter(FileType fileType, bool writeToFiles)
        {
            this.fileType = fileType;
            this.writeToFiles = writeToFiles;
        }

        public void WriteToFile(string text)
        {
            if (writeToFiles && fileType != FileType.None)
            {
                Directory.CreateDirectory(AppDataPath);
                string path = Path.Combine(AppDataPath, GetFileName());

                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(text);
                }
            }
        }

        public static string AppDataPath
        {
            get
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(dir, "Michael");
            }
        }

        private string GetFileName()
        {
            switch (fileType)
            {
                case FileType.UCI:
                    return "UCI_log.txt";
                case FileType.Search:
                    return "Search_log.txt";

                default:
                    return "";
            }
        }
    }



    public enum FileType
    {
        None,
        UCI,
        Search
    }
}
