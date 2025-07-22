
﻿namespace Integration.Models

{
    public class DocumentHistory
    {
        /// <summary>
        /// Unique identifier for the file upload.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Name of the uploaded file.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Current state of the file upload.
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Timestamp when the file upload started.
        /// </summary>
        public string? StartTimestamp { get; set; }

        /// <summary>
        /// Description of the current state.
        /// </summary>
        public string? StateDescription { get; set; }

        /// <summary>
        /// Timestamp of the last state update.
        /// </summary>
        public string? StateTimestamp { get; set; }
    }
}
