using System.IO;

namespace LicenseInspector
{
    public interface IFileAccess
    {
        bool FileExists(string path);
        string[] GetFiles(string path, string searchPattern);
        string ReadAllText(string path);
    }

    public static class FileAccess
    {
        public static IFileAccess GetAccessor()
        {
            return new DiskAccess();
        }
    }

    internal class DiskAccess : IFileAccess
    {
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }
}
