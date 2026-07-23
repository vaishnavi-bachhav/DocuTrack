using DocuTrack.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTrack.Core.Requests
{
    public class CreateDocumentRequest
    {
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DocumentType DocumentType { get; init; }
        public Department Department { get; init; }
        public required string Owner { get; init; } 

        // Using init here -- prevents modification after the request is created.
    }
}
