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

namespace PxPre.Tree
{
    public class TreeNodeAsset
    {
        public Node node;

        public UnityEngine.UI.Image plate;
        public UnityEngine.UI.Text label;
        public UnityEngine.UI.Button btnPlate;

        public UnityEngine.UI.Image expandPlate;
        public UnityEngine.UI.Button expandButton;

        public Dictionary<int, UnityEngine.UI.Image> leftIcons;
        public Dictionary<int, UnityEngine.UI.Image> rightIcons;

        public TreeNodeAsset(Node node)
        {
            this.node = node;
        }

        public void Destroy()
        {
            if (this.plate != null)
                GameObject.Destroy(this.plate.gameObject);

            if (this.expandPlate != null)
                GameObject.Destroy(this.expandPlate.gameObject);
        }

        public void ClearLeftIcons()
        {
            if (this.leftIcons == null)
                return;

            foreach (KeyValuePair<int, UnityEngine.UI.Image> kvp in this.leftIcons)
                GameObject.Destroy(kvp.Value.gameObject);

            this.leftIcons = null;
        }

        public void ClearRightIcons()
        {
            if (this.rightIcons == null)
                return;

            foreach (KeyValuePair<int, UnityEngine.UI.Image> kvp in this.rightIcons)
                GameObject.Destroy(kvp.Value.gameObject);

            this.rightIcons = null;
        }

        public HashSet<int> GetLeftIconIDs()
        {
            return new HashSet<int>(this.leftIcons.Keys);
        }

        public HashSet<int> GetRightIconIDs()
        {
            return new HashSet<int>(this.rightIcons.Keys);
        }

        public bool DestroyIconAssets(int idx)
        {
            if (this.leftIcons != null)
            {
                UnityEngine.UI.Image img;
                if (this.leftIcons.TryGetValue(idx, out img) == true)
                {
                    GameObject.Destroy(img.gameObject);
                    this.leftIcons.Remove(idx);
                    return true;
                }
            }

            if (this.rightIcons != null)
            {
                UnityEngine.UI.Image img;
                if (this.rightIcons.TryGetValue(idx, out img) == true)
                {
                    GameObject.Destroy(img.gameObject);
                    this.rightIcons.Remove(idx);
                    return true;
                }
            }

            return false;
        }

        public UnityEngine.UI.Image GetIconImage(int idx, bool left)
        {
            if (left == true)
                return this.GetIconImage(idx, this.leftIcons);

            return this.GetIconImage(idx, this.rightIcons);
        }

        private UnityEngine.UI.Image GetIconImage(int idx, Dictionary<int, UnityEngine.UI.Image> side)
        {
            UnityEngine.UI.Image img;
            if (side.TryGetValue(idx, out img) == true)
                return img;

            GameObject go = new GameObject("Icon");
            go.transform.SetParent(this.plate.transform, false);

            img = go.AddComponent<UnityEngine.UI.Image>();
            img.sprite = node.GetIconSprite(idx);

            RectTransform rtImg = img.rectTransform;
            rtImg.sizeDelta = img.sprite.rect.size;
            Tree.PrepareChild(rtImg);

            return img;
        }

        public void Hide()
        { 
            this.Show(false);
        }

        public void Show(bool toggle = true)
        {
            if (this.plate != null)
                this.plate.gameObject.SetActive(toggle);

            if(this.expandPlate != null)
                this.expandPlate.gameObject.SetActive(toggle);
        }

        // Move the Expand/Compress button to the correct location, relative
        // to an already placed node plate.
        public bool RealignExpandCompress(TreeProps props)
        {
            if (this.plate == null || this.expandPlate == null)
                return false;

            Vector2 icoMax = props.CalculateExpandCompressMaxs();
            float indent = GetIndentOfNode(this.plate.rectTransform, icoMax.x, props.parentPlateSpacer);
            return RealignExpandCompress(indent, icoMax);
        }

        public bool RealignExpandCompress(float indent, Vector2 expcompMaxDims)
        {
            if (this.plate == null || this.expandPlate == null)
                return false;

            RectTransform rtPlate = this.plate.rectTransform;
            float y = rtPlate.anchoredPosition.y;
            float plateHeight = rtPlate.sizeDelta.y;

            RectTransform rtExp = this.expandPlate.rectTransform;
            Vector2 expSz = this.expandPlate.sprite.rect.size;
            rtExp.sizeDelta = expSz;
            rtExp.anchoredPosition = 
                new Vector2(
                    indent + (expcompMaxDims.x - expSz.x) * 0.5f, 
                    y - (plateHeight - expSz.y) * 0.5f);

            return true;
        }

        public bool CreateExpandCompressAssets(RectTransform parent)
        { 
            if(this.expandPlate != null)
                return false;

            GameObject goExpand = new GameObject("Expand");
            goExpand.transform.SetParent(parent, false);

            this.expandPlate    = goExpand.AddComponent<UnityEngine.UI.Image>();
            this.expandButton   = goExpand.AddComponent<UnityEngine.UI.Button>();
            this.expandButton.targetGraphic = this.expandPlate;
            this.expandButton.onClick.AddListener(() => { node.Expanded = !node.Expanded; });

            Tree.PrepareChild(this.expandPlate.rectTransform);

            return true;
        }

        public void UpdateExpandCompress(TreeProps props, bool realign)
        {
            this.expandPlate.sprite =
                this.node.Expanded ?
                    props.expandSprite :
                    props.compressSprite;

            if(realign == true)
                this.RealignExpandCompress(props);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        //      ALIGNMENT CALCULATION UTILITIES
        //
        // These member(s) (functions) below  are for calculating various properties. While 
        // they're not complex, the point is to have a single location for these things that 
        // are next to each other in code so various things that need to calculate offsets and
        // alignments can agree on value.
        //
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the very leftmost of the node system.
        /// </summary>
        /// <param name="props">
        /// Given the RectTransform for a node's plate, calculate the 
        /// very left of the node's UI system.</param>
        /// <returns></returns>
        /// <remarks>
        /// This returns the VERY leftmost, where the left of the expand/compress
        /// button would be (if it's there). Not to be confused with the left side of the node.
        /// </remarks>
        public static float GetIndentOfNode(RectTransform nodePlate, float icoMaxWidth, float spacer)
        { 
            return nodePlate.anchoredPosition.x - spacer - icoMaxWidth;
        }

        /// <summary>
        /// Given a standard layout information of a node, calculate the starting x position of
        /// a node plate.
        /// </summary>
        /// <param name="indent">The index. This will be a combination of the left margin for the
        /// entire Tree system, the indent-per-hierarchy-depth, the actual hierarchy depth.</param>
        /// <param name="icoMaxWidth">The maximum width between the expand and compress icons.</param>
        /// <param name="spacer">The amount of space between the expand/compress button and the
        /// the node plate.</param>
        /// <returns></returns>
        public static float GetNodePlateX(float indent, float icoMaxWidth, float spacer)
        {
            return indent + icoMaxWidth + spacer;
        }
    }
}
