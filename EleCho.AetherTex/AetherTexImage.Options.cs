namespace EleCho.AetherTex
{
    public sealed class AetherTexImageOptions
    {
        internal AetherTexImageOptions(AetherTexImage owner)
        {
            Owner = owner;
        }

        public AetherTexImage Owner { get; }

        /// <summary>
        /// 使用点采样
        /// </summary>
        public bool UsePointSampling { get; set; }

        /// <summary>
        /// 启用渲染缓冲区缓存
        /// </summary>
        public bool EnableRenderBufferCaching { get; set; }
    }
}
