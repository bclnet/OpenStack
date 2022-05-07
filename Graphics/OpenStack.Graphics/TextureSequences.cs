using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Graphics
{
    /// <summary>
    /// TextureSequences
    /// </summary>
    /// <seealso cref="System.Collections.Generic.List{OpenStack.Graphics.TextureSequences.Sequence}" />
    public class TextureSequences : List<TextureSequences.Sequence>
    {
        /// <summary>
        /// Sequence
        /// </summary>
        public class Sequence
        {
            /// <summary>
            /// Frame
            /// </summary>
            public class Frame
            {
                /// <summary>
                /// Gets or sets the start mins.
                /// </summary>
                /// <value>
                /// The start mins.
                /// </value>
                public Vector2 StartMins { get; set; }
                /// <summary>
                /// Gets or sets the start maxs.
                /// </summary>
                /// <value>
                /// The start maxs.
                /// </value>
                public Vector2 StartMaxs { get; set; }
                /// <summary>
                /// Gets or sets the end mins.
                /// </summary>
                /// <value>
                /// The end mins.
                /// </value>
                public Vector2 EndMins { get; set; }
                /// <summary>
                /// Gets or sets the end maxs.
                /// </summary>
                /// <value>
                /// The end maxs.
                /// </value>
                public Vector2 EndMaxs { get; set; }
            }

            /// <summary>
            /// Gets or sets the frames.
            /// </summary>
            /// <value>
            /// The frames.
            /// </value>
            public IList<Frame> Frames { get; set; }

            /// <summary>
            /// Gets or sets the frames per second.
            /// </summary>
            /// <value>
            /// The frames per second.
            /// </value>
            public float FramesPerSecond { get; set; }
        }
    }
}