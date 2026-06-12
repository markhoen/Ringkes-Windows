using System.IO;

namespace Ringkes.Helpers
{
    public static class FileHelper
    {
        public static bool IsPDFFile(string path)
        {
            return Path.GetExtension(path)
                .ToLower() == ".pdf";
        }

        public static bool IsImageFile(string path)
        {
            string ext =
                Path.GetExtension(path).ToLower();

            return ext == ".jpg"
                || ext == ".jpeg"
                || ext == ".png"
                || ext == ".bmp"
                || ext == ".gif"
                || ext == ".webp";
        }
    }
}