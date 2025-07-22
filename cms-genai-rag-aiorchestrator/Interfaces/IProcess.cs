using cms_genai_rag_aiorchestrator.Contracts;
using System.Text.Json;

namespace cms_genai_rag_aiorchestrator.Interfaces
{
    public interface IProcess
    {
        Task<JsonElement> RunProcessAsync(RequestBody requestBody);
    }
}
