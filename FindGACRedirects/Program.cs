using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace FindGACRedirects
{
    class Program
    {
        static void Main(string[] args)
        {
            var redirects = new Dictionary<string, RedirectVersionInfo>(StringComparer.OrdinalIgnoreCase);

            var root = XElement.Load(args[1]);
            XNamespace ns = "urn:schemas-microsoft-com:asm.v1";
            foreach (var dependentAssembly in root.Descendants(ns + "dependentAssembly"))
            {
                var assemblyIdentity = dependentAssembly.Elements(ns + "assemblyIdentity").First();

                var bindingRedirectElement = dependentAssembly.Elements(ns + "bindingRedirect").First();
                var redirect = bindingRedirectElement.Attribute("oldVersion").Value;
                string[] parts = redirect.Split('-');

                redirects.Add(assemblyIdentity.Attribute("name").Value,
                    new RedirectVersionInfo
                    {
                        Min = Version.Parse(parts[0]),
                        Max = Version.Parse(parts[1]),
                        NewVersion = Version.Parse(bindingRedirectElement.Attribute("newVersion").Value)
                    });
            }

            string[] gacEntries = File.ReadAllLines(args[0]);

            foreach (string gacEntry in gacEntries)
            {
                AssemblyName aname;
                try
                {
                    aname = new AssemblyName(gacEntry);
                }
                catch
                {
                    continue;
                }

                if (aname.Version == null) continue;

                if (redirects.TryGetValue(aname.Name, out RedirectVersionInfo range))
                {
                    if (aname.Version >= range.Min && aname.Version <= range.Max)
                    {
                        Console.WriteLine($"GAC assembly {aname.Name} {aname.Version} is getting redirected to {range.NewVersion}.");
                    }
                }
            }

        }

        class RedirectVersionInfo
        {
            public Version Min { get; set; }
            public Version Max { get; set; }
            public Version NewVersion { get; set; }
        }
    }
}
