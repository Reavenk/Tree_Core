using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PxPre
{
    namespace Tree
    {
        [CreateAssetMenu(menuName = "PxPre/TreeProps")]
        public class TreeProps : ScriptableObject
        {
            public Sprite plate;

            public Color unselected;
            public Color selected;

            public Sprite expandSprite;
            public Sprite compressSprite;

            public float parentPlateSpacer = 5.0f;

            public Vector2 minSize = new Vector2(20.0f, 20.0f);

            public float leftMargin;
            public float rightMargin;
            public float topMargin;
            public float botMargin;

            public float indentAmt = 20.0f;
            public Vector2 startOffset = new Vector2(10.0f, 10.0f);

            public Color labelColor;
            public Font labelFont;
            public int labelFontSize;
        }
    }
}