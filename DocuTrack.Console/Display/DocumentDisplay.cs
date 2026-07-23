using DocuTrack.Core.Models;

namespace DocuTrack.Console.Display;

internal static class DocumentDisplay
{
    public static void ShowDocumentCreated(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        System.Console.WriteLine();
        System.Console.WriteLine("Document created successfully.");
        System.Console.WriteLine();
        System.Console.WriteLine($"Document number: {document.DocumentNumber}");
        System.Console.WriteLine($"Title: {document.Title}");
        System.Console.WriteLine($"Status: {FormatEnumValue(document.Status.ToString())}");
        System.Console.WriteLine($"Version: {document.Version}");
        System.Console.WriteLine();
    }

    public static void ShowAllDocuments(
        IReadOnlyCollection<Document> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        System.Console.WriteLine();
        System.Console.WriteLine("=================================");
        System.Console.WriteLine("            DOCUMENTS");
        System.Console.WriteLine("=================================");
        System.Console.WriteLine();

        if (documents.Count == 0)
        {
            System.Console.WriteLine(
                "No documents are currently available.");

            System.Console.WriteLine();
            return;
        }

        foreach (Document document in documents)
        {
            ShowDocumentSummary(document);
        }

        System.Console.WriteLine();
    }

    public static void ShowError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        System.Console.WriteLine();
        System.Console.WriteLine($"Error: {message}");
        System.Console.WriteLine();
    }

    public static void ShowExitMessage()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Thank you for using DocuTrack.");
    }

    private static void ShowDocumentSummary(Document document)
    {
        string documentType = FormatEnumValue(document.Type.ToString());

        string department = FormatEnumValue(document.Department.ToString());

        string status = FormatEnumValue(document.Status.ToString());

        System.Console.WriteLine(
            $"{document.DocumentNumber} | " +
            $"{document.Title} | " +
            $"{documentType} | " +
            $"{department} | " +
            $"{document.Owner} | " +
            $"{status}");
    }

    private static string FormatEnumValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var characters = new List<char>();

        for (int index = 0; index < value.Length; index++)
        {
            char currentCharacter = value[index];

            if (index > 0 && char.IsUpper(currentCharacter))
            {
                characters.Add(' ');
            }

            characters.Add(currentCharacter);
        }

        return new string(characters.ToArray());
    }
}