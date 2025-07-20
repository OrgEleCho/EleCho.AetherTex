
using System.Numerics;
using EleCho.AetherTex;
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
        var megaTexture = new MegaTexture(TextureFormat.Bgra8888, 1024, 1024, 2, 2);
        megaTexture.Write(GetTextureData(bitmap), 0, 0);
        megaTexture.Write(GetTextureData(bitmap), 1, 0);
        megaTexture.Write(GetTextureData(bitmap), 0, 1);
        megaTexture.Write(GetTextureData(bitmap), 1, 1);

        var quad = new QuadVectors(
            new Vector2(0, 0),
            new Vector2(2048, 0),
            new Vector2(2048, 2048),
            new Vector2(0, 2048));

        using var output = File.Create("output.png");
        var source = megaTexture.CreateSource("color.bgr");
        megaTexture.Read(source, quad, GetTextureData(bitmap2));
        bitmap2.Encode(output, SKEncodedImageFormat.Png, 100);

        Console.WriteLine("Hello, World!");
    }
}