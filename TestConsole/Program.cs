
using System.Numerics;
using EleCho.MegaTextures;
using SkiaSharp;

namespace TestConsole;

internal class Program
{
    private static TextureData GetTextureData(SKBitmap bitmap)
    {
        return new TextureData(TextureFormat.Bgra8888, bitmap.Width, bitmap.Height, bitmap.GetPixels(), bitmap.RowBytes);
    }

    private static void Main(string[] args)
    {
        var bitmap = SKBitmap.Decode("test.jpg");
        var bitmap2 = new SKBitmap(512, 512, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var megaTexture = new MegaTexture(TextureFormat.Bgra8888, 1024, 1024);
        megaTexture.Write(GetTextureData(bitmap), 0, 0);

        var quad = new QuadVectors(
            new Vector2(0, 0),
            new Vector2(100, 0),
            new Vector2(100, 100),
            new Vector2(0, 100));

        megaTexture.Read(megaTexture.DefaultSource, quad, GetTextureData(bitmap));

        using var output = File.Create("output.png");
        bitmap2.Encode(output, SKEncodedImageFormat.Png, 100);

        Console.WriteLine("Hello, World!");
    }
}