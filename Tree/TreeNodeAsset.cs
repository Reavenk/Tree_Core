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
        }
    }
}
