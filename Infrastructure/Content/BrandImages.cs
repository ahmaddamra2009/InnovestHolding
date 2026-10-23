using System.Buffers.Binary;
using System.Text.RegularExpressions;
using InnovestHolding.Infrastructure.Media;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// A brand asset of the site's &lt;head&gt; (Admin, «الهوية البصرية»): the SiteContents key holds the FileKey of the
/// uploaded image, exactly like <see cref="SiteImages"/>. While nothing is uploaded the bundled file stays in use.
/// Labels, guidance and refusal messages are Arabic literals: the admin is Arabic only (same rule as SiteImages).
/// </summary>
/// <param name="Formats">Accepted formats (checked on the extension, the declared type and the signature).</param>
/// <param name="MinWidth">Smallest width (and height for a square slot).</param>
/// <param name="MinHeight">Smallest height.</param>
/// <param name="MaxSide">Largest side.</param>
/// <param name="Square">Width must equal height.</param>
/// <param name="RejectAlpha">A PNG with an alpha channel or a tRNS chunk is refused (nothing is re-encoded, so it
/// cannot be flattened: CTO decision).</param>
/// <param name="DefaultPath">The bundled file shown while nothing is uploaded.</param>
/// <param name="HasAlt">The image carries an alt text (the share image's og:image:alt).</param>
/// <param name="MaxPixels">Largest width × height (0 = MaxSide²).</param>
public sealed record BrandImage(
    string Key, string Label, string Guidance, ImageFormat[] Formats,
    int MinWidth, int MinHeight, int MaxSide, bool Square, bool RejectAlpha, int MaxBytes,
    string DefaultPath, string DefaultSize, bool HasAlt = false, long MaxPixels = 0)
{
    public long PixelCap => MaxPixels > 0 ? MaxPixels : (long)MaxSide * MaxSide;

    /// <summary>The upload caps passed to the shared pipeline (MediaStore): bytes, longest side and pixels of this slot.</summary>
    public ImageUse Use => new(Key, MaxBytes, MaxSide, PixelCap);
}

/// <summary>The three uploadable brand assets and the theme colour (G26, G27, G28).</summary>
public static partial class BrandImages
{
    public const string Favicon = "brand_favicon";
    public const string AppleTouchIcon = "brand_apple_touch_icon";
    public const string OgDefault = "brand_og_default";

    /// <summary>SiteContents key of the mobile browser bar colour (#RRGGBB); empty = <see cref="DefaultThemeColor"/>.</summary>
    public const string ThemeColorKey = "brand_theme_color";
    public const string DefaultThemeColor = "#111145";

    public const int KB = 1024;

    public static readonly IReadOnlyList<BrandImage> All =
    [
        new(Favicon, "أيقونة المتصفح (Favicon)",
            "تظهر في تبويب المتصفح وفي نتائج البحث. PNG فقط، مربعة، من 32×32 إلى 512×512 بكسل (يُفضَّل 48 أو 96 أو 192 أو 512)، حتى 512 KB. يبقى الملف /favicon.ico المرفق متاحاً دائماً للمتصفحات القديمة.",
            [ImageFormat.Png], 32, 32, 512, Square: true, RejectAlpha: false, MaxBytes: 512 * KB,
            DefaultPath: "/assets/brand/favicon.svg", DefaultSize: "SVG"),
        new(AppleTouchIcon, "أيقونة الآيفون (Apple Touch Icon)",
            "تظهر عند إضافة الموقع إلى الشاشة الرئيسية في الآيفون والآيباد. PNG فقط، 180×180 بكسل بالضبط، بخلفية مصمتة بلا شفافية، حتى 512 KB.",
            [ImageFormat.Png], 180, 180, 180, Square: true, RejectAlpha: true, MaxBytes: 512 * KB,
            DefaultPath: "/assets/brand/apple-touch-icon.png", DefaultSize: "180×180"),
        new(OgDefault, "صورة المشاركة الافتراضية (og:image)",
            "تُستخدم عند مشاركة أي صفحة ليست لها صورة خاصة بها (واتساب، لينكدإن، X). JPG أو PNG، من 1200×630 بكسل (بنسبة 1.91:1) حتى 4000 بكسل لأطول ضلع و12 ميغابكسل، حتى 5 MB (يُفضَّل أقل من 600 KB لواتساب).",
            [ImageFormat.Jpeg, ImageFormat.Png], 1200, 630, 4000, Square: false, RejectAlpha: false, MaxBytes: MediaStore.MaxBytes,
            DefaultPath: "/assets/brand/og-default.jpg", DefaultSize: "1200×630", HasAlt: true, MaxPixels: 12_000_000),
    ];

    public static BrandImage? Find(string? key) => All.FirstOrDefault(i => i.Key == key);

    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}\z")]
    public static partial Regex HexColor();

    /// <summary>The stored theme colour if it is a valid #RRGGBB, else the default (a bad row never reaches the page).</summary>
    public static string ThemeColor(string? stored) =>
        stored is not null && HexColor().IsMatch(stored.Trim()) ? stored.Trim().ToLowerInvariant() : DefaultThemeColor;

    private static string FormatList(BrandImage slot) => string.Join(" أو ", slot.Formats.Select(FormatName));

    private static string FormatName(ImageFormat f) => f switch { ImageFormat.Jpeg => "JPG", ImageFormat.WebP => "WebP", _ => "PNG" };

    /// <summary>The file input's accept list for a slot (extensions and media types of its formats).</summary>
    public static string Accept(BrandImage slot) => string.Join(",", slot.Formats.SelectMany(f => f switch
    {
        ImageFormat.Jpeg => new[] { ".jpg", ".jpeg", "image/jpeg" },
        ImageFormat.WebP => new[] { ".webp", "image/webp" },
        _ => new[] { ".png", "image/png" },
    }));

    private static string Bytes(int bytes) => bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024)} MB" : $"{bytes / 1024} KB";

    private static string TypeError(BrandImage slot) => $"الصيغة غير مدعومة. المسموح: {FormatList(slot)} فقط.";

    /// <summary>
    /// What can be decided before a byte is read: the extension and the declared type are formats of the slot, and the
    /// length is within its cap. Null = go on and read the file.
    /// </summary>
    public static string? CheckUpload(BrandImage slot, string? fileName, string? contentType, long length)
    {
        if (ImageInspector.FromExtension(fileName) is not { } byName || !slot.Formats.Contains(byName)) return TypeError(slot);
        if (ImageInspector.FromContentType(contentType) is not { } byType || !slot.Formats.Contains(byType)) return TypeError(slot);
        if (length > slot.MaxBytes) return $"الملف أكبر من {Bytes(slot.MaxBytes)}.";
        return null;
    }

    /// <summary>
    /// The slot's own rules, checked before the shared pipeline stores anything: format, byte size, dimensions, square,
    /// pixels, alpha. Returns the Arabic refusal message, or null when the slot accepts the file. A file the inspector
    /// cannot read returns null too: the pipeline (MediaStore.SaveAsync) then refuses it with its usual message.
    /// </summary>
    public static string? Check(BrandImage slot, ReadOnlySpan<byte> data, string? fileName, string? contentType)
    {
        if (CheckUpload(slot, fileName, contentType, data.Length) is { } early) return early;
        if (ImageInspector.Inspect(data, out var info) != ImageProblem.None) return null;
        if (!slot.Formats.Contains(info!.Format)) return TypeError(slot);

        var (w, h) = (info.Width, info.Height);
        if (slot.Square && w != h) return $"الصورة غير مربعة (الأبعاد {w}×{h}). يجب أن تكون مربعة.";
        if (slot.MinWidth == slot.MaxSide && (w != slot.MinWidth || h != slot.MinHeight))
            return $"الأبعاد {w}×{h} غير مقبولة: يجب أن تكون {slot.MinWidth}×{slot.MinHeight} بالضبط.";
        if (w < slot.MinWidth || h < slot.MinHeight)
            return $"الأبعاد {w}×{h} صغيرة جداً: الحد الأدنى {slot.MinWidth}×{slot.MinHeight}.";
        if (w > slot.MaxSide || h > slot.MaxSide)
            return $"الأبعاد {w}×{h} كبيرة جداً: الحد الأقصى {slot.MaxSide} بكسل لأطول ضلع.";
        if ((long)w * h > slot.PixelCap)
            return $"الأبعاد {w}×{h} كبيرة جداً: الحد الأقصى {slot.PixelCap / 1_000_000} ميغابكسل.";
        if (slot.RejectAlpha && info.Format == ImageFormat.Png && PngHasTransparency(data))
            return "الصورة تحتوي على شفافية (قناة ألفا). أيقونة الآيفون يجب أن تكون بخلفية مصمتة: احفظها PNG بلا شفافية ثم ارفعها من جديد.";
        return null;
    }

    /// <summary>
    /// A PNG that can carry transparency: colour type 4 (grey + alpha) or 6 (RGBA), or a tRNS chunk (palette or
    /// single-colour transparency). Header-only, no pixel is decoded — an RGBA file whose pixels are all opaque is
    /// still refused (the message says how to save it). Call on a file <see cref="ImageInspector.Inspect"/> accepted.
    /// </summary>
    public static bool PngHasTransparency(ReadOnlySpan<byte> png)
    {
        if (png.Length < 33) return false;
        var colourType = png[25]; // signature 8 + length 4 + "IHDR" 4 + width 4 + height 4 + depth 1
        if (colourType is 4 or 6) return true;
        var pos = 8;
        while (pos + 12 <= png.Length)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(png[pos..]);
            if (length > int.MaxValue || pos + 12L + length > png.Length) return false;
            var type = png.Slice(pos + 4, 4);
            if (type.SequenceEqual("tRNS"u8)) return true;
            if (type.SequenceEqual("IDAT"u8) || type.SequenceEqual("IEND"u8)) return false; // tRNS must come before IDAT
            pos += 12 + (int)length;
        }
        return false;
    }

    /// <summary>
    /// What the admin sees under a stored image: the measured size, then a non-blocking advice when the file is
    /// accepted but not ideal. Null = no advice.
    /// </summary>
    public static string? Advice(BrandImage slot, int width, int height, int bytes)
    {
        if (slot.Key == Favicon && width % 48 != 0)
            return "يُفضّل أن تكون الأبعاد من مضاعفات 48 (مثل 48 أو 96 أو 192) لتظهر الأيقونة في نتائج بحث Google.";
        if (slot.Key is OgDefault or ForumIdentity.OgDefault)
        {
            var ratio = (double)width / height;
            if (ratio < 1.8 || ratio > 2.0)
                return $"النسبة {ratio.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}:1 بعيدة عن 1.91:1، قد تُقصّ الصورة عند المشاركة.";
            if (bytes > 600 * KB)
                return $"حجم الملف {bytes / KB} KB: أكبر من 600 KB، قد لا يُظهر واتساب معاينة الصورة.";
        }
        return null;
    }
}
