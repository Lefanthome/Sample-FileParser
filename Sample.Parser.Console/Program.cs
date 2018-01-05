using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sample.Parser.Tools;

namespace Sample.Parser.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Traitement du fichier");

            var lines = ExtractReport(@"ContactList.csv", ';');

            if (lines != null)
            {
                foreach(var line in lines)
                {
                    Console.WriteLine($"Id: {line.Id} - Name: {line.Name} - Company: {line.Company}");
                }
            }

            Console.WriteLine("");

            CreateFile(@"NewContactList.csv");
            Console.WriteLine("Fin du traitement");
            Console.Read();
        }

        private static void CreateFile(string path)
        {
            if(File.Exists(path))
            {
                File.Delete(path);
            }
            List<ContactModel> contacts = new List<ContactModel>();
            contacts.Add(new ContactModel { Id = 1, Name = "Ludovic", Company = "Softfluent", Country = "France" });
            contacts.Add(new ContactModel { Id = 2, Name = "Alexendra", Company = "MS", Country = "UK" });
            contacts.Add(new ContactModel { Id = 3, Name = "Thierry", Company = "Softfluent", Country = "USA" });

            var csvSerializer = new FileParser<ContactModel>(';');

            using (FileStream fs = File.Create(path))
            {
                Byte[] info = new UTF8Encoding(true).GetBytes(csvSerializer.Serialize(contacts));
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }

        }
        private static List<CsvModel> ExtractReport(string path, char separator)
        {
            // Instanciation du Parser
            var csvSerializer = new FileParser<CsvModel>(separator);
            IList<CsvModel> reportLines = null;

            if (!File.Exists(path))
            {
                Console.WriteLine($"PathFile not exist:{path}");
                return null;
            }

            try
            {
                // Deserialize File
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    reportLines = csvSerializer.Deserialize(fs);
                }

                if (reportLines == null)
                    Console.WriteLine($"Path File:{path} file is null");
                else
                    Console.WriteLine($"Path File:{path} Total Lines:{reportLines.Count}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Message error: {ex.Message}");
            }

            return reportLines?.ToList();
        }
    }
}
