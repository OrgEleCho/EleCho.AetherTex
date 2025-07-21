
using System.Diagnostics;
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

    static void TestFeature()
    {
        var bitmap = SKBitmap.Decode("test.jpg");
        var bitmap2 = new SKBitmap(512, 512, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var megaTexture = new AetherTexImage(TextureFormat.Bgra8888, 1024, 1024, 2, 2);
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

    }

    static void TestSpeed()
    {
        var megaTexture = new AetherTexImage(TextureFormat.Bgra8888, 8192, 8192, 3, 3);
        var bufferBitmap = new SKBitmap(300, 300, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var bufferData = GetTextureData(bufferBitmap);

        var quad = megaTexture.FullQuad;
        var stopwatch = Stopwatch.StartNew();
        var readCount = 0;
        while (true)
        {
            megaTexture.Read(quad, bufferData);
            readCount++;

            if (stopwatch.ElapsedMilliseconds >= 1000)
            {
                Console.WriteLine($"FPS: {readCount}");
                stopwatch.Restart();
                readCount = 0;
            }
        }
    }

    private static void Main(string[] args)
    {
        TestSpeed();

        Console.WriteLine("Hello, World!");
    }
}