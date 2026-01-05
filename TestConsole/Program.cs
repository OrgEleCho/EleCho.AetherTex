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
    private static void RgbToYuv(byte r, byte g, byte b, out byte y, out byte u, out byte v)
    {
        // 根据常见的BT.601标准将RGB转换为YUV
        double dY = 0.29882 * r + 0.58681 * g + 0.114363 * b;
        double dU = -0.172485 * r - 0.338718 * g + 0.511207 * b + 128;
        double dV = 0.51155 * r - 0.42811 * g - 0.08343 * b + 128;

        // 将计算结果分别限制在[0, 255]范围内并转换为字节
        y = (byte)Math.Clamp((int)Math.Round(dY), 0, 255);
        u = (byte)Math.Clamp((int)Math.Round(dU), 0, 255);
        v = (byte)Math.Clamp((int)Math.Round(dV), 0, 255);
    }

    private static unsafe void MakeGradient(int width, int height, Action<TextureData> action)
    {
        byte[] data = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var baseOffset = width * 4 * y + 4 * x;
                data[baseOffset] = (byte)(x % 255);
                data[baseOffset + 1] = (byte)(y % 255);
                data[baseOffset + 2] = (byte)(y % 255);
                data[baseOffset + 3] = (byte)255;
            }
        }

        fixed (byte* pData = data)
        {
            action?.Invoke(new TextureData(TextureFormat.Bgra8888, width, height, (nint)pData, width * 4));
        }
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

    private static unsafe void AsYuv420p(TextureData data, Action<TextureData> action)
    {
        var yDataStream = new MemoryStream();
        var uDataStream = new MemoryStream();
        var vDataStream = new MemoryStream();

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var pPixel = (byte*)(data.BaseAddress + data.RowBytes * y + 4 * x);
                RgbToYuv(pPixel[2], pPixel[1], pPixel[0], out var colorY, out var colorU, out var colorV);

                yDataStream.WriteByte(colorY);

                if (x % 2 == 0 &&
                    y % 2 == 0)
                {
                    uDataStream.WriteByte(colorU);
                    vDataStream.WriteByte(colorV);
                }
            }
        }

        yDataStream.Seek(0, SeekOrigin.Begin);
        uDataStream.Seek(0, SeekOrigin.Begin);
        vDataStream.Seek(0, SeekOrigin.Begin);
        var yuvDataStream = new MemoryStream();
        yDataStream.CopyTo(yuvDataStream);
        uDataStream.CopyTo(yuvDataStream);
        vDataStream.CopyTo(yuvDataStream);

        var yuvData = yuvDataStream.ToArray();

        fixed (byte* pYuv420Data = yuvData)
        {
            action?.Invoke(new TextureData(TextureFormat.I420, data.Width, data.Height, (nint)pYuv420Data, data.Width));
        }
    }

    private static unsafe void AsYuv422p(TextureData data, Action<TextureData> action)
    {
        var yDataStream = new MemoryStream();
        var uDataStream = new MemoryStream();
        var vDataStream = new MemoryStream();

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var pPixel = (byte*)(data.BaseAddress + data.RowBytes * y + 4 * x);
                RgbToYuv(pPixel[2], pPixel[1], pPixel[0], out var colorY, out var colorU, out var colorV);

                yDataStream.WriteByte(colorY);

                if (x % 2 == 0)
                {
                    uDataStream.WriteByte(colorU);
                    vDataStream.WriteByte(colorV);
                }
            }
        }

        yDataStream.Seek(0, SeekOrigin.Begin);
        uDataStream.Seek(0, SeekOrigin.Begin);
        vDataStream.Seek(0, SeekOrigin.Begin);
        var yuvDataStream = new MemoryStream();
        yDataStream.CopyTo(yuvDataStream);
        uDataStream.CopyTo(yuvDataStream);
        vDataStream.CopyTo(yuvDataStream);

        var yuvData = yuvDataStream.ToArray();

        fixed (byte* pYuv420Data = yuvData)
        {
            action?.Invoke(new TextureData(TextureFormat.I422, data.Width, data.Height, (nint)pYuv420Data, data.Width));
        }
    }

    private static unsafe void AsYuv444p(TextureData data, Action<TextureData> action)
    {
        var yDataStream = new MemoryStream();
        var uDataStream = new MemoryStream();
        var vDataStream = new MemoryStream();

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var pPixel = (byte*)(data.BaseAddress + data.RowBytes * y + 4 * x);
                RgbToYuv(pPixel[2], pPixel[1], pPixel[0], out var colorY, out var colorU, out var colorV);

                yDataStream.WriteByte(colorY);
                uDataStream.WriteByte(colorU);
                vDataStream.WriteByte(colorV);
            }
        }

        yDataStream.Seek(0, SeekOrigin.Begin);
        uDataStream.Seek(0, SeekOrigin.Begin);
        vDataStream.Seek(0, SeekOrigin.Begin);
        var yuvDataStream = new MemoryStream();
        yDataStream.CopyTo(yuvDataStream);
        uDataStream.CopyTo(yuvDataStream);
        vDataStream.CopyTo(yuvDataStream);

        var yuvData = yuvDataStream.ToArray();

        fixed (byte* pYuv420Data = yuvData)
        {
            action?.Invoke(new TextureData(TextureFormat.I444, data.Width, data.Height, (nint)pYuv420Data, data.Width));
        }
    }

    static void TestFeature()
    {
        var bitmap = SKBitmap.Decode("test.jpg");
        var bitmap2 = new SKBitmap(4096, 4096, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var megaTexture = new AetherTexImage(TextureFormat.Bgra8888, 1024, 1024, 2, 2);
        megaTexture.Write(GetTextureData(bitmap), 0, 0);
        megaTexture.Write(GetTextureData(bitmap), 1, 0);
        megaTexture.Write(GetTextureData(bitmap), 0, 1);
        megaTexture.Write(GetTextureData(bitmap), 1, 1);

        var transform = TransformMatrix.PerspectiveTransform(
            new Vector2(0, 0), new Vector2(100, 100),
            new Vector2(2048, 0), new Vector2(1948, 100),
            new Vector2(0, 2048), new Vector2(300, 1748),
            new Vector2(2048, 2048), new Vector2(1748, 1748));

        var quad = new QuadVectors(
            new Vector2(0, 0),
            new Vector2(2048, 0),
            new Vector2(2048, 2048),
            new Vector2(0, 2048));

        using var output = File.Create("output.png");
        var source = megaTexture.CreateSource("color.rgb");
        megaTexture.Read(source, transform, quad, GetTextureData(bitmap2));
        bitmap2.Encode(output, SKEncodedImageFormat.Png, 100);
    }

    static void TestEdgeFeature()
    {
        var bitmap = SKBitmap.Decode("test.jpg");
        var bitmap2 = new SKBitmap(4096, 4096, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var megaTexture = new AetherTexImage(TextureFormat.Bgra8888, 1024, 1024, 256, 2, 2);
        megaTexture.Write(GetTextureData(bitmap), 0, 0);
        megaTexture.Write(GetTextureData(bitmap), 0, 1);
        megaTexture.Write(GetTextureData(bitmap), 1, 0);
        megaTexture.Write(GetTextureData(bitmap), 1, 1);

        var quad = megaTexture.FullQuad;

        using var output = File.Create("output.png");
        var source = megaTexture.CreateSource("color.rgb");
        megaTexture.Read(source, quad, GetTextureData(bitmap2));
        bitmap2.Encode(output, SKEncodedImageFormat.Png, 100);
    }

    static void TestBayerEdgeFeature()
    {
        var bitmap = SKBitmap.Decode("test.jpg");
        var bitmap2 = new SKBitmap(4096, 4096, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var megaTexture = new AetherTexImage(TextureFormat.BayerRggb, 1024, 1024, 256, 2, 2);
        AsBayer(GetTextureData(bitmap), TextureFormat.BayerRggb, data =>
        {
            megaTexture.Write(data, 0, 0);
            megaTexture.Write(data, 1, 0);
            megaTexture.Write(data, 0, 1);
            megaTexture.Write(data, 1, 1);
        });

        var quad = megaTexture.FullQuad;

        using var output = File.Create("output.png");
        var source = megaTexture.CreateSource("color.rgb");
        megaTexture.Read(source, quad, GetTextureData(bitmap2));
        bitmap2.Encode(output, SKEncodedImageFormat.Png, 100);
    }

    static void TestYuvEdgeFeature()
    {
        var bitmap = SKBitmap.Decode("test.jpg");
        var bitmap2 = new SKBitmap(4096, 4096, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var megaTexture = new AetherTexImage(TextureFormat.I420, 1080, 1080, 256, 2, 2);
        AsYuv420p(GetTextureData(bitmap), data =>
        {
            megaTexture.Write(data, 0, 0);
            megaTexture.Write(data, 1, 0);
            megaTexture.Write(data, 0, 1);
            megaTexture.Write(data, 1, 1);
        });

        var quad = megaTexture.FullQuad;

        using var output = File.Create("output.png");
        var source = megaTexture.CreateSource("color.rgb");
        megaTexture.Read(source, quad, GetTextureData(bitmap2));
        bitmap2.Encode(output, SKEncodedImageFormat.Png, 100);
    }

    static void TestBayerFeature()
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

    static void TestYuvFeature()
    {
        var bitmap = SKBitmap.Decode("test_small.jpg");
        var bitmap2 = new SKBitmap(4096, 4096, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var megaTexture = new AetherTexImage(TextureFormat.I422, 540, 540, 2, 2);
        AsYuv422p(GetTextureData(bitmap), data =>
        {
            megaTexture.Write(data, 0, 0);
            megaTexture.Write(data, 1, 0);
            megaTexture.Write(data, 0, 1);
            megaTexture.Write(data, 1, 1);
        });

        var quad = new QuadVectors(
            new Vector2(0, 0),
            new Vector2(1080, 0),
            new Vector2(1080, 1080),
            new Vector2(0, 1080));

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

    static void TestWriteBgra8888IntoYuv420()
    {
        var bitmap = SKBitmap.Decode("PM5544.png");
        var bitmap2 = SKBitmap.Decode("Test.jpg");
        var texture = new AetherTexImage(TextureFormat.Yuv420, bitmap.Width, bitmap.Height, 2, 2);

        var bufferData = GetTextureData(bitmap);
        var bufferData2 = GetTextureData(bitmap2);
        texture.Write(bufferData, 0, 0, 0);
        texture.Write(bufferData, 0, 1, 1);
        texture.Write(bufferData2, 0, 1, 0);
        texture.Write(bufferData2, 0, 0, 1);

        var bitmapOutput = new SKBitmap(texture.Width, texture.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var bufferDataOutput = GetTextureData(bitmapOutput);

        texture.Read(texture.FullQuad, bufferDataOutput);

        using var output = File.Create("output.png");
        bitmapOutput.Encode(output, SKEncodedImageFormat.Png, 100);
    }

    private static void Main(string[] args)
    {
        TestWriteBgra8888IntoYuv420();

        Console.WriteLine("Hello, World!");
    }
}