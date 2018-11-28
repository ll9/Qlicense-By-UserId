namespace DemoWinFormApp.Utils
{
    public interface IFileHandler
    {
        bool FileExists(string file);
        byte[] GetPublicKey();
        string ReadAllText(string file);
    }
}