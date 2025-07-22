// TestConfig.cs
using Microsoft.Extensions.Configuration;

namespace cms.UnitTest
{
    public static class TestConfig
    {
        public static IConfiguration Instance { get; }

        static TestConfig()
        {
            // Carga el archivo appsettings.Test.json
            // Asegúrate de que "appsettings.Test.json" se copie al bin/Debug en las propiedades del archivo
            Instance = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: false)
                .Build();
        }

    }
}
