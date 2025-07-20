# AtherTex

DirectX based extrem large image affine transform and pixel processing toolkit.



## Usage

**When should I use this?**

If you frequently need to read a small piece of an image at a specified location from a particularly large image, or if you need to perform operations on every pixel of the image, this library is a good choice

**How to use?**

```cs
// create an image
var atImage = new AetherTexImage(TextureFormat.Bgra8888, 1024, 1024, 1, 1);

// write data into it
var bitmap = SKBitmap.Decode("test.jpg");
var buffer = new TextureData(TextureFormat.Bgra8888, bitmap.Width, bitmap.Height, bitmap.GetPixels(), bitmap.RowBytes);
atImage.Write(GetTextureData(bitmap), 0, 0);

// read data from specified quad
var quad = new QuadVectors(
    new Vector2(0, 0),
    new Vector2(512, 0),
    new Vector2(512, 512),
    new Vector2(0, 512));
var outputImage = new SKBitmap(512, 512, SKColorType.Bgra8888, SKAlphaType.Unpremul);
var outputBuffer = new TextureData(TextureFormat.Bgra8888, outputImage.Width, outputImage.Height, outputImage.GetPixels(), outputImage.RowBytes);
atImage.Read(quad, outputBuffer);
```

If your image is extrem large, then write tile by tile.

```cs
// create an image
var atImage = new AetherTexImage(TextureFormat.Bgra8888, 8192, 8192, 4, 4);

// then write
for (int y = 0; y < atImage.Rows; y++)
{
    for (int x = 0; x < atImage.Columns; x++)
    {
        var buffer = ...;
        atImage.Write(buffer, x, y);
    }
}
```

