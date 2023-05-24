using System.Text;

namespace APBConfigManager;

public static class FileUtils
{
    /// <summary>
    /// Reads the content of a JSON file and returns it as a UTF8 string.
    /// </summary>
    public static string ReadJsonFile(string filepath)
    {
        FileStream fStream = OpenFile(FileMode.OpenOrCreate, filepath);

        byte[] buffer = new byte[fStream.Length];
        fStream.Read(buffer, 0, buffer.Length);
        fStream.Close();

        return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>
    /// Writes the given data to the specified JSON file.
    /// </summary>
    public static void WriteJsonFile(string data, string filepath)
    {
        FileStream fStream = OpenFile(FileMode.Create, filepath);

        fStream.Write(Encoding.UTF8.GetBytes(data), 0, data.Length);
        fStream.Close();
    }

    /// <summary>
    /// Opens the specified file as a FileStream.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="filepath"/> is not a valid filepath.
    /// </exception>
    public static FileStream OpenFile(FileMode fileMode, string filepath)
    {
        string? dirPath = Path.GetDirectoryName(filepath)?.ToString();
        ArgumentNullException.ThrowIfNull(dirPath);

        if (dirPath == string.Empty)
            throw new ArgumentException("Invalid filepath");

        // Ensures directory structure exists
        Directory.CreateDirectory(dirPath);

        return File.Open(filepath, fileMode);
    }

    /// <summary>
    /// Creates the directory structure of the specified path if it does
    /// not already exist.
    /// </summary>
    public static void CreateDirectoryIfNotExists(string path)
    {
        Directory.CreateDirectory(path);
    }

    /// <summary>
    /// Returns true if the specified file is a symbolic link.
    /// </summary>
    public static bool IsSymlink(string filepath)
    {
        FileInfo info = new FileInfo(filepath);
        return info.LinkTarget != null;
    }
}