namespace UWUVCI.Core.Tests.Injection;

/// <summary>Builds a minimal Wii U N64 VC base layout in a temp folder.</summary>
internal sealed class TestBase : IDisposable
{
    public DirectoryInfo Root { get; } = Directory.CreateTempSubdirectory("uwuvci-base-");
    public string Content => Path.Combine(Root.FullName, "content");

    public TestBase()
    {
        Directory.CreateDirectory(Path.Combine(Content, "rom"));
        Directory.CreateDirectory(Path.Combine(Content, "config"));
        Directory.CreateDirectory(Path.Combine(Root.FullName, "meta"));
        Directory.CreateDirectory(Path.Combine(Root.FullName, "code"));
        File.WriteAllBytes(Path.Combine(Content, "rom", "NAAE.z64"), new byte[] { 1, 2, 3, 4 });
        File.WriteAllText(Path.Combine(Content, "config", "NAAE.z64.ini"), "old");
        File.WriteAllBytes(Path.Combine(Content, "FrameLayout.arc"), FrameLayoutArc.Build(0x88));   // every real base has one
        File.WriteAllText(Path.Combine(Root.FullName, "meta", "meta.xml"), MetaXml);
        File.WriteAllText(Path.Combine(Root.FullName, "code", "app.xml"), AppXml);
        File.WriteAllText(Path.Combine(Root.FullName, "code", "cos.xml"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><app type=\"complex\" access=\"777\"><version type=\"unsignedInt\" length=\"4\">1</version></app>");
        File.WriteAllText(Path.Combine(Root.FullName, "code", "a.rpx"), "hi");
    }

    public static string AppXml =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><app type=\"complex\" access=\"777\"><version type=\"unsignedInt\" length=\"4\">16</version>" +
        "<os_version type=\"hexBinary\" length=\"8\">000500101000400A</os_version><title_id type=\"hexBinary\" length=\"8\">0005000212345678</title_id>" +
        "<title_version type=\"hexBinary\" length=\"2\">0000</title_version><sdk_version type=\"unsignedInt\" length=\"4\">21500</sdk_version>" +
        "<app_type type=\"hexBinary\" length=\"4\">8000002E</app_type><group_id type=\"hexBinary\" length=\"4\">00001234</group_id>" +
        "<os_mask type=\"hexBinary\" length=\"32\">0000000000000000000000000000000000000000000000000000000000000000</os_mask>" +
        "<common_id type=\"hexBinary\" length=\"8\">0000000000000000</common_id></app>";

    public static string MetaXml =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><menu type=\"complex\" access=\"777\">" +
        string.Concat(new[] { "ja", "en", "fr", "de", "it", "es", "zhs", "ko", "nl", "pt", "ru", "zht" }
            .SelectMany(l => new[] { $"<longname_{l}>Old</longname_{l}>", $"<shortname_{l}>Old</shortname_{l}>" })) +
        "<product_code>WUP-N-OLD0</product_code><title_id>0005000212345678</title_id><group_id>00001234</group_id><drc_use>65537</drc_use></menu>";

    public void Dispose() => Root.Delete(true);
}
