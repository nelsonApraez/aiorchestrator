
using cms_genai_rag_aiorchestrator.Utilities;

namespace cms_genai_rag_aiorchestrator.Tests
{
    public class UtilitiesTests
    {
        #region NormalizeFileName Tests

        [Fact]
        public void NormalizeFileName_EmptyOrNull_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => cms_genai_rag_aiorchestrator.Utilities.Utilities.NormalizeFileName(""));
            Assert.Throws<ArgumentException>(() => cms_genai_rag_aiorchestrator.Utilities.Utilities.NormalizeFileName(null));
        }

        [Fact]
        public void NormalizeFileName_AccentedAndSpecialCharacters_ConvertsProperly()
        {
            // Arrange
            // Á, é, í, ó, ú, ü, ñ, y 
            var input = "Árbol#con%espacios.txt";

            // Act
            var result = cms_genai_rag_aiorchestrator.Utilities.Utilities.NormalizeFileName(input);

            // Assert
            Assert.Equal("arbolconespacios.txt", result);
        }

        [Fact]
        public void NormalizeFileName_SpacesAreConvertedToDashes()
        {
            // Arrange
            var input = "Mi Archivo con espacios.doc";           

            // Act
            var result = cms_genai_rag_aiorchestrator.Utilities.Utilities.NormalizeFileName(input);

            // Assert
            Assert.Equal("mi-archivo-con-espacios.doc", result);
        }

        [Fact]
        public void NormalizeFileName_WithPath_PreservesDirectoryAndFixesFileName()
        {
            // Arrange
            var input = @"Archivo con Ñ.pdf";


            // Act
            var result = cms_genai_rag_aiorchestrator.Utilities.Utilities.NormalizeFileName(input);

            // Assert
            var expected = @"archivo-con-n.pdf";
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
