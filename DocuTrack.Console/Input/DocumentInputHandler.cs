using DocuTrack.Console.Display;
using DocuTrack.Core.Enums;
using DocuTrack.Core.Models;
using DocuTrack.Core.Requests;
using DocuTrack.Core.Services;

namespace DocuTrack.Console.Input
{
    public class DocumentInputHandler
    {
        private readonly DocumentService _documentService;
        public DocumentInputHandler(DocumentService documentService)
        {
            _documentService = documentService;
        }
        public void AddDocument()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("=================================");
            System.Console.WriteLine("          ADD DOCUMENT");
            System.Console.WriteLine("=================================");
            System.Console.WriteLine();

            string title = ReadRequiredText("Enter document title: ");

            string? description = ReadOptionalText("Enter document description (optional): ");

            DocumentType documentType = ReadDocumentType();

            Department department = ReadDepartment();

            string owner = ReadRequiredText("Enter document owner: ");

            // Create a new document request
            var createDocumentRequest = new CreateDocumentRequest
            {
                Title = title,
                Description = description,
                DocumentType = documentType,
                Department = department,
                Owner = owner
            };
            Document document = _documentService.CreateDocument(createDocumentRequest);

            DocumentDisplay.ShowDocumentCreated(document);
        }

        private static DocumentType ReadDocumentType()
        {
            while (true)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Select document type:");
                System.Console.WriteLine("1. Invoice");
                System.Console.WriteLine("2. Contract");
                System.Console.WriteLine("3. Purchase Order");
                System.Console.WriteLine("4. Engineering Drawing");
                System.Console.WriteLine("5. Quality Report");
                System.Console.WriteLine("6. Employee Record");
                System.Console.WriteLine("7. Compliance Document");
                System.Console.WriteLine("8. Other");
                System.Console.Write("Enter your choice: ");

                string? input = System.Console.ReadLine();

                if (int.TryParse(input, out int option) &&
                    option >= 1 &&
                    option <= 8)
                {
                    return (DocumentType)option;
                }

                System.Console.WriteLine(
                    "Invalid input. Please enter a whole number between 1 and 8.");
            }
        }

        private static Department ReadDepartment()
        {
            while (true)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Select department:");
                System.Console.WriteLine("1. Human Resources");
                System.Console.WriteLine("2. Finance");
                System.Console.WriteLine("3. Engineering");
                System.Console.WriteLine("4. Purchasing");
                System.Console.WriteLine("5. Manufacturing");
                System.Console.WriteLine("6. Quality Assurance");
                System.Console.WriteLine("7. Legal");
                System.Console.WriteLine("8. Sales");
                System.Console.WriteLine("9. Information Technology");
                System.Console.Write("Enter your choice: ");

                string? input = System.Console.ReadLine();

                if (int.TryParse(input, out int option) &&
                    option >= 1 &&
                    option <= 9)
                {
                    return (Department)option;
                }

                System.Console.WriteLine(
                    "Invalid input. Please enter a whole number between 1 and 9.");
            }
        }

        private static string ReadRequiredText(string prompt)
        {
            while (true)
            {
                System.Console.Write(prompt);

                string input = System.Console.ReadLine() ?? string.Empty;

                input = input.Trim();

                if (!string.IsNullOrWhiteSpace(input))
                {
                    return input;
                }

                System.Console.WriteLine("This field is required. Please enter a value.");

                System.Console.WriteLine();
            }
        }

        private static string? ReadOptionalText(string prompt)
        {
            System.Console.Write(prompt);

            string input = System.Console.ReadLine() ?? string.Empty;

            input = input.Trim();

            return string.IsNullOrWhiteSpace(input)
                ? null
                : input;
        }
    }
}
