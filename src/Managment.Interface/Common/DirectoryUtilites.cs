using System.IO;

namespace Managment.Interface.Common
{
    public static class DirectoryUtilites
    {
        public static void CreateOrClearDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            string[] directories = Directory.GetDirectories(path);
            foreach (string directory in directories)
            {
                Directory.Delete(directory, true);
            }

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }

        public static void CopyDirectory(string sourceDirrectory, string destinationDirrectory, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDirrectory);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDirrectory);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDirrectory, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDirrectory, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
