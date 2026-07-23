using DocuTrack.Console.Display;
using DocuTrack.Console.Input;
using DocuTrack.Console.Menus; 
using DocuTrack.Core.Models;
using DocuTrack.Core.Repositories;
using DocuTrack.Core.Services;

var documentRepository = new InMemoryDocumentRepository();
var documentService = new DocumentService(documentRepository);
var documentInputHandler = new DocumentInputHandler(documentService);
bool isRunning = true;

while (isRunning)
{
    int option = Menu.DisplayMainMenu();

    switch (option)
    {
        case 1:
            documentInputHandler.AddDocument();
            break;

        case 2:
            IReadOnlyCollection<Document> documents =
               documentService.GetAllDocuments();

            DocumentDisplay.ShowAllDocuments(documents);
            break;

        case 3:
            DocumentDisplay.ShowExitMessage();
            isRunning = false;
            break;
    }
}