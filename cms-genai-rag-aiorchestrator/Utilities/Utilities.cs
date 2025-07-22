using System.Text.RegularExpressions;

namespace cms_genai_rag_aiorchestrator.Utilities
{
    public static class Utilities
    {

        public static string NormalizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            string directory = Path.GetDirectoryName(fileName) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            // Convertir a minúsculas y reemplazar caracteres acentuados
            string normalizedFileName = fileNameWithoutExtension.ToLower()
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("ü", "u")
                .Replace("ñ", "n");

            // Reemplazar espacios con guiones
            normalizedFileName = Regex.Replace(normalizedFileName, @"\s+", "-");

            // Eliminar otros símbolos no permitidos
            normalizedFileName = Regex.Replace(normalizedFileName, @"[@#\$&%+/=\\?]", "");

            return Path.Combine(directory, $"{normalizedFileName}{extension}");
        }
    }
}
