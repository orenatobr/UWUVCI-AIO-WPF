using System.Xml.Linq;

namespace UWUVCI.Core.Injection;

/// <summary>Edits <c>meta/meta.xml</c> and <c>code/app.xml</c> of a base: names, title id, group id, product code.</summary>
public static class MetaXml
{
    private static readonly string[] Languages =
        ["ja", "en", "fr", "de", "it", "es", "zhs", "ko", "nl", "pt", "ru", "zht"];

    /// <summary>Rewrites the file in place.</summary>
    /// <param name="path">Path to meta.xml.</param>
    /// <param name="appXmlPath">Path to code/app.xml, which must carry the same title and group id.</param>
    /// <param name="gameName">Name; "Line1|Line2" makes a two line long name. Blank keeps the base names.</param>
    /// <param name="rng">Source for the random title id, injectable for tests.</param>
    /// <exception cref="FileNotFoundException">meta.xml does not exist.</exception>
    public static void Apply(string path, string appXmlPath, string gameName, Random rng)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("meta.xml not found.", path);
        var root = XDocument.Load(path).Root ?? throw new InvalidDataException("meta.xml has no root element.");

        if (!string.IsNullOrWhiteSpace(gameName))
        {
            var lines = gameName.Split('|', 2);
            var longName = string.Join("\n", lines);
            foreach (var l in Languages)
            {
                Set(root, $"longname_{l}", longName);
                Set(root, $"shortname_{l}", lines[0]);
            }
        }

        string id = $"{rng.Next(0x3000, 0x10000):X4}{rng.Next(0x3000, 0x10000):X4}";
        string code = $"{rng.Next(0x3000, 0x10000):X4}";
        Set(root, "product_code", $"WUP-N-{code}");
        Set(root, "title_id", $"00050002{id}");
        Set(root, "group_id", $"0000{code}");
        root.Document!.Save(path);

        if (!File.Exists(appXmlPath)) throw new FileNotFoundException("app.xml not found.", appXmlPath);
        var app = XDocument.Load(appXmlPath);
        var appRoot = app.Root ?? throw new InvalidDataException("app.xml has no root element.");
        Set(appRoot, "title_id", $"00050002{id}");
        Set(appRoot, "group_id", $"0000{code}");
        app.Save(appXmlPath);
    }

    private static void Set(XElement root, string name, string value)
    {
        var e = root.Element(name);
        if (e is not null) e.Value = value;
    }
}
