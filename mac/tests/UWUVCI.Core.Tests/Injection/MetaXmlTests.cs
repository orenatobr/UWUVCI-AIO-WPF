using System.Xml.Linq;
using UWUVCI.Core.Injection;

namespace UWUVCI.Core.Tests.Injection;

public class MetaXmlTests
{
    [Fact]
    public void Apply_SetsNamesAndIds_Deterministically()
    {
        using var b = new TestBase();
        var path = Path.Combine(b.Root.FullName, "meta", "meta.xml");
        MetaXml.Apply(path, "Banjo-Tooie|N64", new Random(42));

        var d = XDocument.Load(path).Root!;
        Assert.Equal("Banjo-Tooie\nN64", d.Element("longname_en")!.Value);
        Assert.Equal("Banjo-Tooie", d.Element("shortname_pt")!.Value);
        Assert.Matches("^WUP-N-[0-9A-F]{4}$", d.Element("product_code")!.Value);
        Assert.Matches("^00050002[0-9A-F]{8}$", d.Element("title_id")!.Value);
        Assert.Equal("0000" + d.Element("product_code")!.Value[6..], d.Element("group_id")!.Value);

        var again = Path.Combine(b.Root.FullName, "meta", "meta2.xml");
        File.WriteAllText(again, TestBase.MetaXml);
        MetaXml.Apply(again, "Banjo-Tooie|N64", new Random(42));
        Assert.Equal(d.Element("title_id")!.Value, XDocument.Load(again).Root!.Element("title_id")!.Value);
    }

    [Fact]
    public void Apply_BlankName_KeepsOriginalNames()
    {
        using var b = new TestBase();
        var path = Path.Combine(b.Root.FullName, "meta", "meta.xml");
        MetaXml.Apply(path, "", new Random(1));
        Assert.Equal("Old", XDocument.Load(path).Root!.Element("longname_en")!.Value);
    }

    [Fact]
    public void Apply_MissingFile_Throws() =>
        Assert.Throws<FileNotFoundException>(() => MetaXml.Apply("/nonexistent/meta.xml", "x", new Random(1)));
}
