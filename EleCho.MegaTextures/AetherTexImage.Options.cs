namespace EleCho.AetherTex
{
    public sealed class AetherTexImageOptions
    {
        internal AetherTexImageOptions(AetherTexImage owner)
        {
            Owner = owner;
        }

        public AetherTexImage Owner { get; }

        public bool EnableRenderBufferCaching { get; set; }
    }
}
