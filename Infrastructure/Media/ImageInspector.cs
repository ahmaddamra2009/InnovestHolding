using System.Buffers.Binary;

namespace InnovestHolding.Infrastructure.Media;

/// <summary>The three image formats the site accepts. SVG (script), GIF, BMP, TIFF, HEIC, ICO… are never accepted.</summary>
public enum ImageFormat
{
    Jpeg,
    Png,
    WebP,
}

/// <summary>Why a file was refused (each maps to an admin message).</summary>
public enum ImageProblem
{
    None,
    /// <summary>Not JPEG/PNG/WebP by signature, or the extension/declared type is not one of them.</summary>
    Type,
    /// <summary>The extension, the declared content type and the signature do not agree.</summary>
    Mismatch,
    /// <summary>The structure is broken (truncated chunk, bad CRC, missing frame header…).</summary>
    Corrupt,
    /// <summary>Width/height outside the allowed range, or too many pixels.</summary>
    Dimensions,
    /// <summary>Animated PNG/WebP.</summary>
    Animated,
    /// <summary>Empty or over the size cap.</summary>
    Size,
}

/// <summary>Width and height as the picture is DISPLAYED (a JPEG with EXIF orientation 5–8 has them swapped).</summary>
public sealed record ImageInfo(ImageFormat Format, int Width, int Height);

/// <summary>
/// What an image is for, and its limits. Logos are shown a few dozen pixels high, so a heavy original there costs page
/// weight for nothing (no re-encode by decision): they get a lower cap.
/// </summary>
public sealed record ImageUse(string Name, int MaxBytes, int MaxLongSide, long MaxPixels)
{
    /// <summary>News, events, pages, founder photo: up to 5 MB, 8000 px per side, 25 MP.</summary>
    public static readonly ImageUse General = new("general", MediaStore.MaxBytes, ImageInspector.MaxSide, ImageInspector.MaxPixels);

    /// <summary>Partner logos: up to 1 MB and 1200 px on the longest side.</summary>
    public static readonly ImageUse Logo = new("logo", 1024 * 1024, 1200, 1200L * 1200);

    public bool Allows(ImageInfo info) =>
        Math.Max(info.Width, info.Height) <= MaxLongSide && (long)info.Width * info.Height <= MaxPixels;
}

/// <summary>
/// Strict, header-only inspection of an uploaded image — no decoder, no native code (shared hosting: CTO decision).
/// It walks the whole container (PNG chunks with their CRC, JPEG segments up to EOI, WebP RIFF chunks), reads the
/// dimensions from the header, refuses animation, and <see cref="Sanitize"/> rebuilds the file from the known parts
/// only: metadata that can carry personal data (EXIF/GPS, XMP, IPTC, comments, text chunks) and any byte after the
/// image's end (where a ZIP/HTML polyglot would sit) are dropped. Pixels are never touched.
/// </summary>
public static class ImageInspector
{
    public const int MinSide = 16;
    public const int MaxSide = 8000;
    public const long MaxPixels = 25_000_000;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>The format named by the first bytes, or null. Used again when a stored file is served.</summary>
    public static ImageFormat? Sniff(ReadOnlySpan<byte> head)
    {
        if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return ImageFormat.Jpeg;
        if (head.Length >= 8 && head[..8].SequenceEqual(PngSignature)) return ImageFormat.Png;
        if (head.Length >= 12 && head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8)) return ImageFormat.WebP;
        return null;
    }

    public static string Extension(ImageFormat f) => f switch { ImageFormat.Jpeg => "jpg", ImageFormat.Png => "png", _ => "webp" };

    public static string ContentType(ImageFormat f) => f switch { ImageFormat.Jpeg => "image/jpeg", ImageFormat.Png => "image/png", _ => "image/webp" };

    public static string Name(ImageFormat f) => f switch { ImageFormat.Jpeg => "jpeg", ImageFormat.Png => "png", _ => "webp" };

    public static ImageFormat? FromName(string? name) => name switch
    {
        "jpeg" => ImageFormat.Jpeg,
        "png" => ImageFormat.Png,
        "webp" => ImageFormat.WebP,
        _ => null,
    };

    /// <summary>The format a file extension claims (".jpg", ".jpeg", ".png", ".webp", any case), else null.</summary>
    public static ImageFormat? FromExtension(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".png" => ImageFormat.Png,
            ".webp" => ImageFormat.WebP,
            _ => null,
        };
    }

    /// <summary>The format a declared content type claims (image/jpeg, image/pjpeg, image/png, image/webp), else null.</summary>
    public static ImageFormat? FromContentType(string? contentType)
    {
        var t = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        return t switch
        {
            "image/jpeg" or "image/pjpeg" => ImageFormat.Jpeg,
            "image/png" => ImageFormat.Png,
            "image/webp" => ImageFormat.WebP,
            _ => null,
        };
    }

    /// <summary>Validates the whole file; on success <paramref name="info"/> holds the format and the header dimensions.</summary>
    public static ImageProblem Inspect(ReadOnlySpan<byte> data, out ImageInfo? info)
    {
        info = null;
        var format = Sniff(data);
        if (format is null) return ImageProblem.Type;
        var result = format switch
        {
            ImageFormat.Png => WalkPng(data, null),
            ImageFormat.Jpeg => WalkJpeg(data, null),
            _ => WalkWebP(data, null),
        };
        if (result.Problem != ImageProblem.None) return result.Problem;
        if (result.Width < MinSide || result.Height < MinSide || result.Width > MaxSide || result.Height > MaxSide
            || (long)result.Width * result.Height > MaxPixels)
            return ImageProblem.Dimensions;
        info = new ImageInfo(format.Value, result.Width, result.Height);
        return ImageProblem.None;
    }

    /// <summary>
    /// The file rebuilt from its image parts only (call after <see cref="Inspect"/> succeeded). JPEG keeps its frame,
    /// tables, JFIF/Adobe/ICC segments and the EXIF orientation (as a minimal EXIF block); PNG keeps the critical
    /// and colour chunks; WebP keeps the bitstream, alpha and ICC chunks. The result is inspected again by the caller.
    /// </summary>
    public static byte[] Sanitize(ReadOnlySpan<byte> data, ImageFormat format)
    {
        using var output = new MemoryStream(data.Length);
        var result = format switch
        {
            ImageFormat.Png => WalkPng(data, output),
            ImageFormat.Jpeg => WalkJpeg(data, output),
            _ => WalkWebP(data, output),
        };
        if (result.Problem != ImageProblem.None) throw new InvalidOperationException("Sanitize called on an invalid image.");
        return output.ToArray();
    }

    private readonly record struct Walk(ImageProblem Problem, int Width = 0, int Height = 0);

    private static readonly Walk Corrupt = new(ImageProblem.Corrupt);

    // ── PNG ──

    // Kept: critical chunks and the ones that change how the pixels look. Everything else (tEXt, zTXt, iTXt, eXIf,
    // tIME, private chunks…) is dropped.
    private static readonly HashSet<string> PngKeep = ["IHDR", "PLTE", "IDAT", "IEND", "tRNS", "gAMA", "cHRM", "sRGB", "iCCP", "sBIT", "pHYs", "bKGD"];

    private static Walk WalkPng(ReadOnlySpan<byte> d, Stream? output)
    {
        output?.Write(PngSignature);
        var pos = 8;
        int width = 0, height = 0;
        var first = true;
        var sawData = false;
        while (true)
        {
            if (pos + 12 > d.Length) return Corrupt;
            var length = BinaryPrimitives.ReadUInt32BigEndian(d[pos..]);
            if (length > int.MaxValue || pos + 12 + (long)length > d.Length) return Corrupt;
            var type = System.Text.Encoding.ASCII.GetString(d.Slice(pos + 4, 4));
            var chunk = d.Slice(pos, 12 + (int)length);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(d[(pos + 8 + (int)length)..]);
            if (Crc32.Compute(d.Slice(pos + 4, 4 + (int)length)) != crc) return Corrupt;

            if (first)
            {
                if (type != "IHDR" || length != 13) return Corrupt;
                width = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(d[(pos + 8)..]), int.MaxValue);
                height = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(d[(pos + 12)..]), int.MaxValue);
                first = false;
            }
            else if (type == "IHDR") return Corrupt;

            if (type is "acTL" or "fcTL" or "fdAT") return new Walk(ImageProblem.Animated);
            if (type == "IDAT") sawData = true;
            if (output is not null && PngKeep.Contains(type)) output.Write(chunk);

            pos += 12 + (int)length;
            if (type == "IEND")
                // Anything after IEND (an appended archive or page) is simply not copied.
                return sawData ? new Walk(ImageProblem.None, width, height) : Corrupt;
        }
    }

    // ── JPEG ──

    private static Walk WalkJpeg(ReadOnlySpan<byte> d, Stream? output)
    {
        output?.Write([0xFF, 0xD8]);
        var pos = 2;
        int width = 0, height = 0;
        var sawFrame = false;
        var rotated = false; // EXIF orientation 5–8: the picture is shown turned a quarter, width and height swap
        while (true)
        {
            // A marker: 0xFF, optional 0xFF fill bytes, then the code.
            if (pos >= d.Length || d[pos] != 0xFF) return Corrupt;
            while (pos < d.Length && d[pos] == 0xFF) pos++;
            if (pos >= d.Length) return Corrupt;
            var code = d[pos++];

            if (code == 0xD9) // EOI — the end of the image; bytes after it are not copied
            {
                output?.Write([0xFF, 0xD9]);
                if (!sawFrame) return Corrupt;
                return rotated ? new Walk(ImageProblem.None, height, width) : new Walk(ImageProblem.None, width, height);
            }
            if (code is 0xD8 or 0x00 || code is >= 0xD0 and <= 0xD7 || code == 0x01) return Corrupt; // no stray SOI/RST/TEM here

            if (pos + 2 > d.Length) return Corrupt;
            var length = BinaryPrimitives.ReadUInt16BigEndian(d[pos..]);
            if (length < 2 || pos + length > d.Length) return Corrupt;
            var segment = d.Slice(pos - 2, length + 2); // FF code len data
            var payload = d.Slice(pos + 2, length - 2);
            pos += length;

            var isFrame = code is >= 0xC0 and <= 0xCF && code is not (0xC4 or 0xC8 or 0xCC);
            if (isFrame)
            {
                if (payload.Length < 6) return Corrupt;
                height = BinaryPrimitives.ReadUInt16BigEndian(payload[1..]);
                width = BinaryPrimitives.ReadUInt16BigEndian(payload[3..]);
                if (width == 0 || height == 0) return Corrupt; // DNL-defined height is not supported
                sawFrame = true;
            }

            var orientation = code == 0xE1 ? ExifOrientation(payload) : null;
            if (orientation is >= 5 and <= 8) rotated = true;

            if (output is not null)
            {
                if (code == 0xE1)
                {
                    // EXIF (and XMP) dropped; the orientation alone is carried over so phone photos stay upright.
                    if (orientation is >= 2 and <= 8) output.Write(MinimalExif(orientation.Value));
                }
                else if (KeepJpegSegment(code, payload))
                {
                    output.Write(segment);
                }
            }

            if (code == 0xDA) // SOS: entropy-coded data follows until the next real marker
            {
                if (!sawFrame) return Corrupt;
                var start = pos;
                while (true)
                {
                    if (pos + 1 >= d.Length) return Corrupt;
                    if (d[pos] == 0xFF && d[pos + 1] != 0x00 && d[pos + 1] is not (>= 0xD0 and <= 0xD7) && d[pos + 1] != 0xFF)
                        break;
                    pos++;
                }
                output?.Write(d[start..pos]);
            }
        }
    }

    /// <summary>JFIF (APP0), ICC profile (APP2 "ICC_PROFILE"), Adobe (APP14, colour transform) and every non-APP,
    /// non-comment segment (tables, frame, scan). Other APPn and COM are metadata.</summary>
    private static bool KeepJpegSegment(byte code, ReadOnlySpan<byte> payload)
    {
        if (code == 0xFE) return false;                               // COM
        if (code is < 0xE0 or > 0xEF) return true;                    // DQT, DHT, SOFn, SOS, DRI…
        return code switch
        {
            0xE0 => payload.StartsWith("JFIF\0"u8) || payload.StartsWith("JFXX\0"u8),
            0xE2 => payload.StartsWith("ICC_PROFILE\0"u8),
            0xEE => payload.StartsWith("Adobe"u8),
            _ => false,
        };
    }

    /// <summary>The Orientation tag (0x0112) of IFD0 of an APP1 Exif payload, or null.</summary>
    public static int? ExifOrientation(ReadOnlySpan<byte> payload)
    {
        if (!payload.StartsWith("Exif\0\0"u8)) return null;
        var tiff = payload[6..];
        if (tiff.Length < 8) return null;
        bool little;
        if (tiff[0] == 0x49 && tiff[1] == 0x49) little = true;
        else if (tiff[0] == 0x4D && tiff[1] == 0x4D) little = false;
        else return null;
        uint U32(ReadOnlySpan<byte> s) => little ? BinaryPrimitives.ReadUInt32LittleEndian(s) : BinaryPrimitives.ReadUInt32BigEndian(s);
        ushort U16(ReadOnlySpan<byte> s) => little ? BinaryPrimitives.ReadUInt16LittleEndian(s) : BinaryPrimitives.ReadUInt16BigEndian(s);
        var ifd = U32(tiff[4..]);
        if (ifd > (uint)tiff.Length - 2) return null;
        var count = U16(tiff[(int)ifd..]);
        for (var i = 0; i < count; i++)
        {
            var entry = (int)ifd + 2 + i * 12;
            if (entry + 12 > tiff.Length) return null;
            if (U16(tiff[entry..]) == 0x0112 && U16(tiff[(entry + 2)..]) == 3)
                return U16(tiff[(entry + 8)..]);
        }
        return null;
    }

    /// <summary>An APP1 segment holding only IFD0 with the Orientation tag (big-endian TIFF).</summary>
    internal static byte[] MinimalExif(int orientation)
    {
        byte[] body =
        [
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0,
            (byte)'M', (byte)'M', 0, 0x2A, 0, 0, 0, 8, // TIFF header, IFD0 at 8
            0, 1,                                     // one entry
            0x01, 0x12, 0, 3, 0, 0, 0, 1, 0, (byte)orientation, 0, 0, // Orientation SHORT ×1
            0, 0, 0, 0,                               // no next IFD
        ];
        var segment = new byte[body.Length + 4];
        segment[0] = 0xFF;
        segment[1] = 0xE1;
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(body.Length + 2));
        body.CopyTo(segment, 4);
        return segment;
    }

    // ── WebP ──

    private static Walk WalkWebP(ReadOnlySpan<byte> d, Stream? output)
    {
        if (d.Length < 20) return Corrupt;
        var riffSize = BinaryPrimitives.ReadUInt32LittleEndian(d[4..]);
        if (riffSize < 4 || riffSize + 8L > d.Length) return Corrupt;
        var end = (int)riffSize + 8; // bytes after the RIFF container are not part of the image (not copied)

        var kept = new List<(int Start, int Length)>();
        var pos = 12;
        int width = 0, height = 0;
        var first = true;
        var extended = false;
        var sawBitstream = false;
        var vp8xFlagsAt = -1;
        int streamWidth = 0, streamHeight = 0;
        while (pos < end)
        {
            if (pos + 8 > end) return Corrupt;
            var fourCc = System.Text.Encoding.ASCII.GetString(d.Slice(pos, 4));
            var size = BinaryPrimitives.ReadUInt32LittleEndian(d[(pos + 4)..]);
            var padded = size + (size & 1);
            if (pos + 8L + padded > end) return Corrupt;
            var payload = d.Slice(pos + 8, (int)size);

            switch (fourCc)
            {
                case "VP8X":
                    if (!first || size < 10) return Corrupt;
                    if ((payload[0] & 0x02) != 0) return new Walk(ImageProblem.Animated);
                    width = 1 + (payload[4] | payload[5] << 8 | payload[6] << 16);
                    height = 1 + (payload[7] | payload[8] << 8 | payload[9] << 16);
                    extended = true;
                    vp8xFlagsAt = pos + 8;
                    break;
                case "VP8 ":
                    if (size < 10 || payload[3] != 0x9D || payload[4] != 0x01 || payload[5] != 0x2A) return Corrupt;
                    streamWidth = BinaryPrimitives.ReadUInt16LittleEndian(payload[6..]) & 0x3FFF;
                    streamHeight = BinaryPrimitives.ReadUInt16LittleEndian(payload[8..]) & 0x3FFF;
                    if (sawBitstream) return Corrupt;
                    sawBitstream = true;
                    break;
                case "VP8L":
                    if (size < 5 || payload[0] != 0x2F) return Corrupt;
                    var bits = BinaryPrimitives.ReadUInt32LittleEndian(payload[1..]);
                    streamWidth = 1 + (int)(bits & 0x3FFF);
                    streamHeight = 1 + (int)((bits >> 14) & 0x3FFF);
                    if (sawBitstream) return Corrupt;
                    sawBitstream = true;
                    break;
                case "ANIM":
                case "ANMF":
                    return new Walk(ImageProblem.Animated);
            }
            if (first && fourCc is not ("VP8X" or "VP8 " or "VP8L")) return Corrupt;
            first = false;

            // Kept: the header, the bitstream, alpha and the colour profile. EXIF, XMP and unknown chunks are dropped.
            if (fourCc is "VP8X" or "VP8 " or "VP8L" or "ALPH" or "ICCP")
                kept.Add((pos, 8 + (int)padded));
            pos += 8 + (int)padded;
        }
        if (!sawBitstream) return Corrupt;
        // The size that is stored and rendered is the bitstream's: a VP8X canvas must declare exactly that size.
        if (extended && (width != streamWidth || height != streamHeight)) return Corrupt;
        width = streamWidth;
        height = streamHeight;
        if (width == 0 || height == 0) return Corrupt;

        if (output is not null)
        {
            var total = 4 + kept.Sum(k => k.Length);
            Span<byte> header = stackalloc byte[12];
            "RIFF"u8.CopyTo(header);
            BinaryPrimitives.WriteUInt32LittleEndian(header[4..], (uint)total);
            "WEBP"u8.CopyTo(header[8..]);
            output.Write(header);
            foreach (var (start, length) in kept)
            {
                if (start + 8 == vp8xFlagsAt)
                {
                    var copy = d.Slice(start, length).ToArray();
                    copy[8] &= unchecked((byte)~(0x08 | 0x04)); // EXIF and XMP flags off: those chunks are gone
                    output.Write(copy);
                }
                else
                {
                    output.Write(d.Slice(start, length));
                }
            }
        }
        return new Walk(ImageProblem.None, width, height);
    }
}

/// <summary>CRC-32 (ISO-HDLC, the PNG chunk CRC).</summary>
internal static class Crc32
{
    private static readonly uint[] Table = Build();

    private static uint[] Build()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var c = 0xFFFFFFFFu;
        foreach (var b in data) c = Table[(c ^ b) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }
}
