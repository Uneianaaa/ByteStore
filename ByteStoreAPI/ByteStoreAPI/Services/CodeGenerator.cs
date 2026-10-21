using System.Security.Cryptography;

namespace ByteStoreAPI.Services
{
    public static class CodeGenerator
    {
        public static string Generate()
        {
            int code = RandomNumberGenerator.GetInt32(100000, 1000000);

            return code.ToString("D6");
        }
    }
}