// MIT License
// 
// Copyright (c) 2020 Pixel Precision, LLC
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PxPre
{
    namespace Tree
    {
        /// <summary>
        /// The behavioural and visual properties of a tree.
        /// </summary>
        [CreateAssetMenu(menuName = "PxPre/TreeProps")]
        public class TreeProps : ScriptableObject
        {
            /// <summary>
            /// Specifies how tree nodes should use their widths when sized.
            /// </summary>
            public enum MaxXMode
            { 
                /// <summary>
                /// The nodes should use their calculated widths.
                /// </summary>
                Width,

                /// <summary>
                /// The nodes should expands all the way to the end and touch
                /// the right edge.
                /// </summary>
                TouchEdge
            }

            /// <summary>
            /// Specifies how the tree should distribute its width.
            /// </summary>
            /// <remarks>
            /// Smilar to MaxXMode, but slightly out of coincidence since they're
            /// specifything different things, so we'll keep the enums different.
            /// </remarks>
            public enum WidthMode
            { 
                /// <summary>
                /// The width should be the rightmost position if 
                /// </summary>
                Width,

                /// <summary>
                /// Leave the width alone. Only the height will be modified.
                /// </summary>
                Leave,

                /// <summary>
                /// Auto-align the right side to touch the right side of Tree's 
                /// RectTransform parent.
                /// </summary>
                TouchEdge
            }

            /// <summary>
            /// Specifies if the tree should show its background or not.
            /// </summary>
            public enum BackgroundFill
            { 
                /// <summary>
                /// No background.
                /// </summary>
                Empty,

                /// <summary>
                /// Filled in background.
                /// </summary>
                Filled, 

                /// <summary>
                /// Don't override the Tree's fill property.
                /// </summary>
                Leave
            }

            /// <summary>
            /// The plate for a node.
            /// </summary>
            public Sprite plate;

            /// <summary>
            /// The color of the node when unselected.
            /// </summary>
            public Color unselected;

            /// <summary>
            /// The color of the node when selected.
            /// </summary>
            public Color selected;

            /// <summary>
            /// The sprite for when the node is expanded.
            /// </summary>
            public Sprite expandSprite;

            /// <summary>
            /// The amount of vertical space between node plates.
            /// </summary>
            public float vNodeSpace = 10.0f;

            /// <summary>
            /// The sprite for when the node is compressed.
            /// </summary>
            public Sprite compressSprite;

            /// <summary>
            /// The amount of space between the expand/compress icon, and the 
            /// node's main plate.
            /// </summary>
            public float parentPlateSpacer = 5.0f;

            /// <summary>
            /// The minimum size between a node's plate.
            /// </summary>
            public Vector2 minSize = new Vector2(20.0f, 20.0f);

            /// <summary>
            /// The amount of space on the bottom between the node's plate and the
            /// very bottom of the node's content.
            /// </summary>
            public float horizPlateMargin = 5.0f;

            /// <summary>
            /// The amount of space on the left between the node's plate and the
            /// very left of the node's content.
            /// </summary>
            public float vertPlateMargin = 5.0f;

            /// <summary>
            /// The amount to indent when going down an additional depth in the 
            /// node hierarchy.
            /// </summary>
            public float indentAmt = 20.0f;

            /// <summary>
            /// The amount of empty space to add before displaying the tree.
            /// </summary>
            public Vector2 startOffset = new Vector2(10.0f, 10.0f);

            /// <summary>
            /// The amount of empty space to add to the end of the tree.
            /// </summary>
            public Vector2 endOffset = new Vector2(10.0f, 10.0f);

            /// <summary>
            /// If there are multiple icons on one side, the horizontal
            /// spacing between them.
            /// </summary>
            public float iconSpacing = 5.0f;

            /// <summary>
            /// If there are icons, the horizontal spacing between it and the text. This
            /// applies for both the left and the right side.
            /// </summary>
            public float iconNameSpacing = 10.0f;

            /// <summary>
            /// The color of a tree node label.
            /// </summary>
            public Color labelColor;

            /// <summary>
            /// The font for a tree node label.
            /// </summary>
            public Font labelFont;

            /// <summary>
            /// The font size of a tree node label.
            /// </summary>
            public int labelFontSize;

            /// <summary>
            /// The right-alignment behaviour of nodes.
            /// </summary>
            public MaxXMode maxMode = MaxXMode.Width;

            /// <summary>
            /// The right-alignment behaviour of the tree.
            /// </summary>
            public WidthMode widthMode = WidthMode.Width;

            /// <summary>
            /// The background fill behaviour.
            /// </summary>
            public BackgroundFill backgroundFill = BackgroundFill.Filled;
        }
    }
}