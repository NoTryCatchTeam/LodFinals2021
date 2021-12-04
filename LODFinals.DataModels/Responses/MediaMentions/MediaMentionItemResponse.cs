using System;

namespace LODFinals.DataModels.Responses.MediaMentions
{
    public class MediaMentionItemResponse
    {
        public Guid GId { get; set; }

        public string? Title { get; set; }

        public DateTime PublishDate { get; set; }

        public string? Media { get; set; }
    }
}
