namespace Integration.Interfaces
{
    public interface ICognitiveSearchService
    {
        Task<string> SearchAsync(
        string? query = null,
        float[]? embedding = null,
        CancellationToken cancellationToken = default);

    }
}
