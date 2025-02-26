using System.IO;

namespace Managment.Interface.Common
{
    public static class DirectoryUtilites
    {
        public static void CreateOrClearRepositoryDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            string[] files = Directory.GetFiles(path);

            foreach (string file in files)
            {
                File.Delete(file);
            }
        }
    }
}
