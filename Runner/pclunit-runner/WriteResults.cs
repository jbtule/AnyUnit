using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PclUnit.Run;

namespace pclunit_runner
{
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
                        File.WriteAllText(typeAndPath.Value, file.ToListJson());
                        break;
                }
            }
        }
    }
}
