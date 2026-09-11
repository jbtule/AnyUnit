using System.Collections.Generic;
using System.IO;
using AnyUnit.Run;

public static class WriteResults
{
    public const string JsonType = "json";
    public const string NunitType = "nunit";

    public static void ToFiles(ResultsFile file, IDictionary<string, string> typesAndPaths)
    {
        foreach (var typeAndPath in typesAndPaths)
        {
            switch (typeAndPath.Key)
            {
                default:
                    File.WriteAllText(typeAndPath.Value,file.ToListJson());
                    break;
            }
        }
    }
}