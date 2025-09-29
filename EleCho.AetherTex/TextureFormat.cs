﻿namespace EleCho.AetherTex
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

        Float32,
    }
}
