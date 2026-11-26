using System.Text;

namespace UWUVCI.Core.Tests.Injection;

/// <summary>Builds a tiny FrameLayout.arc with the two panes the patcher edits, laid out like the real one.</summary>
internal static class FrameLayoutArc
{
    /// <summary>Offset of the frame pane width float, relative to the start of the archive.</summary>
    public static int FrameWidthOffset { get; private set; }
    /// <summary>Offset of the frame pane's four texture coordinates (TL, TR, BL, BR as big-endian s,t floats).</summary>
    public static int FrameUvOffset { get; private set; }
    /// <summary>Offset of the byte that shows or hides the dark filter.</summary>
    public static int MaskFlagOffset { get; private set; }

    private static void U16(List<byte> b, int v) { b.Add((byte)(v >> 8)); b.Add((byte)v); }
    private static void U32(List<byte> b, uint v) { b.Add((byte)(v >> 24)); b.Add((byte)(v >> 16)); b.Add((byte)(v >> 8)); b.Add((byte)v); }
    private static void Set32(List<byte> b, int at, uint v) { b[at] = (byte)(v >> 24); b[at + 1] = (byte)(v >> 16); b[at + 2] = (byte)(v >> 8); b[at + 3] = (byte)v; }

    private static byte[] PicPane(string name, int size)
    {
        var p = new List<byte>();
        p.AddRange(Encoding.ASCII.GetBytes("pic1"));
        U32(p, (uint)size);
        p.Add(0x01); p.Add(0x00); p.Add(0x00); p.Add(0x00);          // flags (the mask flag is the byte at +0x08)
        var name24 = new byte[0x18]; Encoding.ASCII.GetBytes(name).CopyTo(name24, 0);
        p.AddRange(name24);
        while (p.Count < size) p.Add(0);
        if (size >= 0x88)
        {
            var uv = new[] { 0f, 0f, 1f, 0f, 0f, 1f, 1f, 1f };
            for (int i = 0; i < uv.Length; i++)
            {
                var bits = BitConverter.SingleToUInt32Bits(uv[i]);
                for (int k = 0; k < 4; k++) p[0x68 + i * 4 + k] = (byte)(bits >> (24 - 8 * k));
            }
        }
        return p.ToArray();
    }

    /// <summary>The archive has two files: an animation (index 0) and the layout (index 1), like the real one.</summary>
    public static byte[] Build(int frameSize = 0x60)
    {
        var frame = PicPane("frame", frameSize);
        var mask = PicPane("frame_mask", 0x30);
        var flyt = new List<byte>();
        flyt.AddRange(Encoding.ASCII.GetBytes("FLYT")); U16(flyt, 0xFEFF); U16(flyt, 0x14); U32(flyt, 0x02020000);
        U32(flyt, 0); U16(flyt, 2); U16(flyt, 0);                       // file size patched below, section count
        flyt.AddRange(frame); flyt.AddRange(mask);
        Set32(flyt, 0x0C, (uint)flyt.Count);

        var anim = new byte[0x20];
        var arc = new List<byte>();
        arc.AddRange(Encoding.ASCII.GetBytes("SARC")); U16(arc, 0x14); U16(arc, 0xFEFF); U32(arc, 0); U32(arc, 0); U16(arc, 0x0100); U16(arc, 0);
        arc.AddRange(Encoding.ASCII.GetBytes("SFAT")); U16(arc, 0x0C); U16(arc, 2); U32(arc, 0x65);
        U32(arc, 1); U32(arc, 0x01000000); U32(arc, 0); U32(arc, (uint)anim.Length);               // node 0: anim
        U32(arc, 2); U32(arc, 0x01000004); U32(arc, 0x20); U32(arc, (uint)(0x20 + flyt.Count));  // node 1: layout (begin at 0x38)
        arc.AddRange(Encoding.ASCII.GetBytes("SFNT")); U16(arc, 8); U16(arc, 0);
        arc.AddRange(Encoding.ASCII.GetBytes("a.bflan\0\0")); arc.AddRange(Encoding.ASCII.GetBytes("b.bflyt\0\0"));
        while (arc.Count % 0x20 != 0) arc.Add(0);
        int dataOffset = arc.Count;
        Set32(arc, 0x0C, (uint)dataOffset);
        arc.AddRange(anim);
        while (arc.Count - dataOffset < 0x20) arc.Add(0);
        int layoutStart = arc.Count;
        arc.AddRange(flyt);
        Set32(arc, 0x08, (uint)arc.Count);
        FrameWidthOffset = layoutStart + 0x14 + 0x4C;
        FrameUvOffset = layoutStart + 0x14 + 0x68;
        MaskFlagOffset = layoutStart + 0x14 + frame.Length + 0x08;
        return arc.ToArray();
    }
}
