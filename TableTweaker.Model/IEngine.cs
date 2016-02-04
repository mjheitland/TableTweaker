namespace TableTweaker.Model
{
    interface IEngine
    {
        char FieldDelimiter { get; set; }

        string Process(Table table, string pattern, string code);
    }
}
