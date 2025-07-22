namespace cms_genai_rag_aiorchestrator.Contracts
{
    public class ProcessResponse
    {
        public string? Conversation_id { get; set; }
        public bool Current_state { get; set; }
        public string? Detail { get; set; }
        public string? Error { get; set; }
        public string? Answer { get; set; }
        public string? Thoughts { get; set; }
    }
}
