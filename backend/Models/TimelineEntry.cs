namespace backend.Models
{
    public class TimelineEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }

        public string EntryType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string ExternalUrl { get; set; } = string.Empty;
        public string SourceApi { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string Metadata { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
