namespace cms_genai_rag_aiorchestrator.Contracts
{
    public class RequestBody
    {
        public required string Conversation_id { get; set; }
        public required string Person_id { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
        public required string Query { get; set; }
    }
}
