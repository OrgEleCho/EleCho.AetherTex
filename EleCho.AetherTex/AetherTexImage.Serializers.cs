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


            }

            public AetherTexImage Deserialize(BinaryReader reader)
            {

            }
        }
    }
}
