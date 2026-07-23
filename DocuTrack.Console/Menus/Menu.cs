namespace DocuTrack.Console.Menus;

internal static class Menu
{
    public static int DisplayMainMenu()
    {
        while (true)
        {
            ShowMainMenu();

            string? input = System.Console.ReadLine();

            if (int.TryParse(input, out int option) &&
                option >= 1 &&
                option <= 3)
            {
                return option;
            }

            System.Console.WriteLine();
            System.Console.WriteLine(
                "Invalid input. Please enter a whole number between 1 and 3.");
            System.Console.WriteLine();
        }
    }

    private static void ShowMainMenu()
    {
        System.Console.WriteLine("=================================");
        System.Console.WriteLine("           DOCUTRACK");
        System.Console.WriteLine("=================================");
        System.Console.WriteLine();
        System.Console.WriteLine("1. Add document");
        System.Console.WriteLine("2. View all documents");
        System.Console.WriteLine("3. Exit");
        System.Console.WriteLine();
        System.Console.Write("Select an option: ");
    }
}