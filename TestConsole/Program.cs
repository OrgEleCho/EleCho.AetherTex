
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

    private static unsafe void AsBayer(TextureData data, TextureFormat format, Action<TextureData> action)
    {
        byte[] bayerData = new byte[data.Width * data.Height];
        int[] patternIndices = data.Format switch
        {
            TextureFormat.Bgra8888 => format switch
            {
                TextureFormat.BayerRggb => [2, 1, 1, 0],
                TextureFormat.BayerGrbg => [1, 0, 2, 1],
                TextureFormat.BayerGbrg => [1, 2, 0, 1],
                TextureFormat.BayerBggr => [0, 1, 1, 2],
                _ => throw new ArgumentException("Invalid Bayer format", nameof(format)),
            },

            TextureFormat.Rgba8888 => format switch
            {
                TextureFormat.BayerRggb => [0, 1, 1, 2],
                TextureFormat.BayerGrbg => [1, 2, 0, 1],
                TextureFormat.BayerGbrg => [1, 0, 2, 1],
                TextureFormat.BayerBggr => [2, 1, 1, 0],
                _ => throw new ArgumentException("Invalid Bayer format", nameof(format)),
            },

            _ => throw new ArgumentException("Invalid source data format")
        };

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var positionInPattern = (y % 2) * 2 + (x % 2);
                var offset = data.RowBytes * y + 4 * x + patternIndices[positionInPattern];

                bayerData[y * data.Width + x] = *((byte*)data.BaseAddress + offset);
            }
        }

        fixed (byte* pBayerData = bayerData)
        {
            action?.Invoke(new TextureData(format, data.Width, data.Height, (nint)pBayerData, data.Width));
        }
    }

    static void TestFeature()
    {
        var bitmap = SKBitmap.Decode("test.jpg");
        var bitmap2 = new SKBitmap(4096, 4096, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var megaTexture = new AetherTexImage(TextureFormat.BayerRggb, 1024, 1024, 2, 2);
        AsBayer(GetTextureData(bitmap), TextureFormat.BayerRggb, data =>
        {
            megaTexture.Write(data, 0, 0);
            megaTexture.Write(data, 1, 0);
            megaTexture.Write(data, 0, 1);
            megaTexture.Write(data, 1, 1);
        });

        var quad = new QuadVectors(
            new Vector2(0, 0),
            new Vector2(2048, 0),
            new Vector2(2048, 2048),
            new Vector2(0, 2048));

        using var output = File.Create("output.png");
        var source = megaTexture.CreateSource("color.rgb");
        megaTexture.Read(source, quad, GetTextureData(bitmap2));
        bitmap2.Encode(output, SKEncodedImageFormat.Png, 100);
    }

    static void TestSpeed()
    {
        var megaTexture = new AetherTexImage(TextureFormat.Bgra8888, 8192, 8192, 3, 3)
        {
            Options =
            {
                EnableRenderBufferCaching = true
            }
        };

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
        TestFeature();

        Console.WriteLine("Hello, World!");
    }
}