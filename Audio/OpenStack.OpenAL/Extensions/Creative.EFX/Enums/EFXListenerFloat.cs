namespace System.NumericsX.OpenAL.Extensions.Creative.EFX
{
    /// <summary>
    /// A list of valid <see cref="float"/> Listener/GetListener parameters.
    /// </summary>
    public enum EFXListenerFloat
    {
        /// <summary>
        /// centimeters 0.01f
        /// meters 1.0f
        /// kilometers 1000.0f
        /// Range [float.MinValue .. float.MaxValue]
        /// Default: 1.0f.
        ///
        /// This setting is critical if Air Absorption effects are enabled because the amount of Air
        /// Absorption applied is directly related to the real-world distance between the Source and the Listener.
        /// </summary>
        EfxMetersPerUnit = 0x20004,
    }
}
