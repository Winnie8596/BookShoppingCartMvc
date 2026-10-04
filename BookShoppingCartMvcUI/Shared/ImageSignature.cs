namespace BookShoppingCartMvcUI.Shared;

// checks the first bytes of an upload so a renamed .exe or .html can't pass as a cover.
// the extension has to match too, a png called .jpg is refused
public static class ImageSignature
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool Matches(IFormFile file)
    {
        var expected = Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => Jpeg,
            ".png" => Png,
            _ => null
        };
        if (expected is null || file.Length < expected.Length)
            return false;

        using var stream = file.OpenReadStream();
        var header = new byte[expected.Length];
        stream.ReadExactly(header);
        return header.AsSpan().SequenceEqual(expected);
    }
}
