using EleCho.AetherTex.Internal;

namespace EleCho.AetherTex
{
    public sealed unsafe partial class AetherTexImage
    {
        private interface ISerializer
        {
            public void Serialize(AetherTexImage image, BinaryWriter writer);
            public AetherTexImage Deserialize(BinaryReader reader);
        }

        private class SerializerV1 : ISerializer
        {
            private SerializerV1() { }

            public static SerializerV1 Instance { get; } = new();
            private static int GetPixelBytes(TextureFormat format)
            {
                return format switch
                {
                    TextureFormat.Bgra8888 => 4,
                    TextureFormat.Rgba8888 => 4,

                    TextureFormat.Gray8 => 1,
                    TextureFormat.Gray16 => 2,
                    TextureFormat.Float32 => 4,

                    TextureFormat.BayerRggb => 1,
                    TextureFormat.BayerBggr => 1,
                    TextureFormat.BayerGbrg => 1,
                    TextureFormat.BayerGrbg => 1,

                    _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported texture format")
                };
            }

            public void Serialize(AetherTexImage image, BinaryWriter writer)
            {
                writer.Write((uint)image.Format);
                writer.Write((uint)image.TileWidth);
                writer.Write((uint)image.TileHeight);
                writer.Write((uint)image.Columns);
                writer.Write((uint)image.Rows);

                var sourceCount = image.Sources.Count;
                writer.Write((byte)sourceCount);
                foreach (var source in image.Sources)
                {
                    writer.Write(source);
                }

                var pixelBytes = GetPixelBytes(image.Format);
                var tileBufferArray = new byte[pixelBytes * image.TileWidth * image.TileHeight];

                fixed (byte* pTileBufferArray = tileBufferArray)
                {
                    var tileBuffer = new TextureData(
                        image.Format, image.TileWidth, image.TileHeight, 
                        (nint)pTileBufferArray, 
                        image.TileWidth * pixelBytes);

                    for (int i = 0; i < image.Sources.Count; i++)
                    {
                        for (int y = 0; y < image.Rows; y++)
                        {
                            for (int x = 0; x < image.Columns; x++)
                            {
                                image.ReadTile(i, tileBuffer, x, y);
                                writer.Write(tileBufferArray);
                            }
                        }
                    }
                }
            }

            public AetherTexImage Deserialize(BinaryReader reader)
            {
                var format = (TextureFormat)reader.ReadUInt32();
                var tileWidth = reader.ReadUInt32();
                var tileHeight = reader.ReadUInt32();
                var columns = reader.ReadUInt32();
                var rows = reader.ReadUInt32();

                var sourceCount = reader.ReadByte();
                var sources = new string[sourceCount];
                for (int i = 0; i < sourceCount; i++)
                {
                    sources[i] = reader.ReadString();
                }

                var image = new AetherTexImage(format, (int)tileWidth, (int)tileHeight, (int)columns, (int)rows, sources);

                var pixelBytes = GetPixelBytes(format);
                var tileBufferArray = new byte[pixelBytes * tileWidth * tileHeight];

                fixed (byte* pTileBufferArray = tileBufferArray)
                {
                    var tileBuffer = new TextureData(
                        format, (int)tileWidth, (int)tileHeight,
                        (nint)pTileBufferArray,
                        (int)(tileWidth * pixelBytes));

                    for (int i = 0; i < sourceCount; i++)
                    {
                        for (int y = 0; y < rows; y++)
                        {
                            for (int x = 0; x < columns; x++)
                            {
                                reader.Read(tileBufferArray, 0, tileBufferArray.Length);
                                image.Write(tileBuffer, i, x, y);
                            }
                        }
                    }
                }

                return image;
            }
        }
    }
}
