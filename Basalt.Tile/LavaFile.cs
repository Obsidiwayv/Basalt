using System.Security.Cryptography;
using System.Text;

namespace Basalt.Tile
{
    public class BasaltLavaFile(string FileName)
    {
        public string Name { get; } = FileName;

        public static string Extension { get; } = ".lava";

        public string Fetch() 
        {
            if (!File.Exists(Name)) 
                throw new BasaltException("Basalt could not find a project file");
            return File.ReadAllText(Name);
        } 

        public string ComputeHash()
        {
            string FileContents = Fetch();
            byte[] Hash = SHA256.HashData(Encoding.UTF8.GetBytes(FileContents));
            return Convert.ToHexString(Hash);
        }
    }
}
