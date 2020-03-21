using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;

namespace LicenseInspector
{
    public class Projects
    {
        public static Projects LoadFrom(string path)
        {
            var str = File.ReadAllText(path).ToLower();
            try
            {
                return JsonConvert.DeserializeObject<Projects>(str);
            }
            catch (JsonReaderException jre)
            {
                if (jre.Message.Contains("Bad JSON escape sequence"))
                {
                    Log.Error($"Error converting content in {path} to JSON. Are the file paths properly escaped with '\\\\'?");
                }

                throw;
            }
        }

        public string[] InternalProjects { get; set; }

        [JsonConstructor]
        public Projects(string[] internalProjects)
        {
            InternalProjects = internalProjects.Select(x => x.ToLower()).ToArray();
        }

        public bool Contains(string projectPath)
        {
            return InternalProjects.Contains(projectPath.ToLower());
        }
    }
}
