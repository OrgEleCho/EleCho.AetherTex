namespace EleCho.AetherTex
{
    public enum TextureFormat
    {
        Bgra8888,
        Rgba8888,

        Gray8,
        Gray16,

        BayerRggb,
        BayerGrbg,
        BayerBggr,
        BayerGbrg,

        /// <summary>
        /// YUV444, I444, Planar
        /// </summary>
        I444,

        /// <summary>
        /// YUV422, I422, Planar
        /// </summary>
        I422,

        /// <summary>
        /// YUV420, I420, Planar
        /// </summary>
        I420,

        /// <summary>
        /// YCbCr 4:2:0, Splitted (Y plane + CbCr plane)
        /// </summary>
        YCbCr420,

        /// <summary>
        /// YCbCr 4:2:2, Splitted (Y plane + CbCr plane)
        /// </summary>
        YCbCr422,

        /// <summary>
        /// YCbCr 4:4:4, Packed (YCbCr in single texture)
        /// </summary>
        YCbCr444,

        /// <summary>
        /// YCoCg 4:2:0, Splitted (Y plane + CoCg plane)
        /// </summary>
        YCoCg420,

        /// <summary>
        /// YCoCg 4:2:2, Splitted (Y plane + CoCg plane)
        /// </summary>
        YCoCg422,

        /// <summary>
        /// YCoCg 4:4:4, Packed (YCoCg in single texture)
        /// </summary>
        YCoCg444,

        Float32,
    }
}
