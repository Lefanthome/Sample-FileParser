using Sample.Parser.Tools;

namespace Sample.Parser.ConsoleApp
{
    public class ContactModel
    {
        [MappingCol("Id")]
        public int Id { get; set; }
        [MappingCol("Name")]
        public string Name { get; set; }
        [MappingCol("Societe")]
        public string Company { get; set; }
        [MappingCol("Pays")]
        public string Country { get; set; }
    }
}
