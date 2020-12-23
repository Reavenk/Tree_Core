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
        public class Tree : UnityEngine.UI.Image
        {
            static int idCounter = 0;
            public static int GetNewID()
            { 
                return ++idCounter;
            }

            Dictionary<Node, TreeNodeAsset> nodeAssets = 
                new Dictionary<Node, TreeNodeAsset>();

            HashSet<Node> dirtyItems = new HashSet<Node>();

            Node root = null;

            public TreeProps props;

            public MonoBehaviour coroutineHost;
            private Coroutine dirtyUpdate = null;

            public HashSet<ITreeHandler> subscribers = new HashSet<ITreeHandler>();

            public MonoBehaviour GetCoroutineHost() => this.coroutineHost ?? this;

            public HashSet<Node> selected = new HashSet<Node>();

            public Node GetRoot()
            { 
                if(this.root == null)
                    this.root = new Node(this, "$_ROOT");

                return this.root;
            }

            public Node AddNode(string label, Node parent)
            {
                if (parent == null)
                    parent = this.GetRoot();

                if (parent.OwnerTree != this)
                    return null;

                Node node = new Node(this, label);

                node._SetParent(parent);

                this.nodeAssets.Add(node, new TreeNodeAsset(node));

                return node;
            }

            public bool HasNode(Node node)
            { 
                return this.nodeAssets.ContainsKey(node);
            }

            public void FlagDirty(Node node)
            {
                this.dirtyItems.Add(node);
                this.FlagDirty();
            }

            public bool FlagDirty()
            { 
                if(this.dirtyUpdate != null)
                    return false;

                this.dirtyUpdate = 
                    this.GetCoroutineHost().StartCoroutine(this.DirtyCoroutine());

                return true;
            }

            public IEnumerator DirtyCoroutine()
            { 
                yield return new WaitForEndOfFrame();

                bool doLayout = false;

                foreach (Node n in this.dirtyItems)
                { 
                    Node.DirtyItems dflag = n.DirtyFlags;
                    n.ClearDirty();

                    TreeNodeAsset tna;
                    this.nodeAssets.TryGetValue(n, out tna);

                    if((dflag & Node.DirtyItems.Selection) != 0)
                    { 
                        if(tna != null)
                        { 
                            if(n.Selected == true)
                                tna.plate.color = this.props.selected;
                            else
                                tna.plate.color = this.props.unselected;
                        }
                    }

                    if((dflag & Node.DirtyItems.Name) != 0)
                    { 
                        if(tna != null)
                        {
                            tna.label.text = n.Label;

                            // TODO: We might actually be able to avoid doing 
                            // a full rebuild and just redoing only our width if
                            // we know we're the only thing being renamed.
                            doLayout = true;
                        }
                    }

                    if((dflag & Node.DirtyItems.Expand) != 0)
                    {
                        doLayout = true;
                    }

                    if((dflag & (Node.DirtyItems.Reparent|Node.DirtyItems.RemoveTree|Node.DirtyItems.ChildChange|Node.DirtyItems.ChangedIcons)) != 0)
                        doLayout = true;
                }

                if(doLayout == true)
                    this.LayoutTree();

                this.dirtyUpdate = null;
            }

            public Vector2 LayoutTree()
            { 
                float indent = this.props.startOffset.x;
                float y = -this.props.startOffset.y;
                float maxX = indent;

                HashSet<TreeNodeAsset> used = new HashSet<TreeNodeAsset>(this.nodeAssets.Values);

                Vector2 compSprDim = this.props.compressSprite.rect.size;
                Vector2 expaSprDim = this.props.expandSprite.rect.size;
                Vector2 ceDim = 
                    new Vector2(
                        Mathf.Max(compSprDim.x, expaSprDim.x),
                        Mathf.Max(compSprDim.y, expaSprDim.y));

                bool atleastone = false;
                if(this.root != null && this.root.HasChildren() == true)
                {
                    foreach(Node n in this.root.GetChildren())
                    {
                        if(atleastone == true)
                            y -= this.props.vNodeSpace;

                        this.LayoutNode(n, indent, ref y, ref maxX, ceDim, used);
                        atleastone = true;
                    }
                }

                maxX += this.props.endOffset.x;
                y -= this.props.endOffset.y;

                this.rectTransform.sizeDelta = new Vector2(maxX, -y);

                return new Vector2(maxX, y);
            }

            public static void PrepareChild(RectTransform rt)
            { 
                rt.localScale       = Vector2.one;
                rt.localRotation    = Quaternion.identity;

                rt.anchorMin        = new Vector2(0.0f, 1.0f);
                rt.anchorMax        = new Vector2(0.0f, 1.0f);
                rt.pivot            = new Vector2(0.0f, 1.0f);
            }

            public void DisableHierarchy(Node node)
            {
                TreeNodeAsset tna;
                if (nodeAssets.TryGetValue(node, out tna) == true)
                    tna.Hide();

                foreach(Node n in node.GetChildren())
                    this.DisableHierarchy(n);
            }

            private void LayoutNode(Node node, float indent, ref float y, ref float maxX, Vector2 expcmpMax, HashSet<TreeNodeAsset> used)
            { 

                TreeNodeAsset tna;
                if(nodeAssets.TryGetValue(node, out tna) == false)
                {
                    tna = new TreeNodeAsset(node);
                    nodeAssets.Add(node, tna);
                }

                used.Remove(tna);

                if (tna.plate == null)
                { 
                    GameObject goPlate  = new GameObject("TreeNode");
                    goPlate.transform.SetParent(this.transform, false);
                    tna.plate           = goPlate.AddComponent<UnityEngine.UI.Image>();
                    PrepareChild(tna.plate.rectTransform);
                    tna.plate.sprite    = this.props.plate;
                    tna.plate.color     = this.props.unselected;
                    tna.plate.type      = Type.Sliced;

                    tna.btnPlate        = goPlate.AddComponent<UnityEngine.UI.Button>();
                    tna.btnPlate.targetGraphic = tna.plate;
                    tna.btnPlate.onClick.AddListener( ()=>{ this.NodeClickHandler(node); });

                    GameObject goText   = new GameObject("TreeNodeLabel");
                    goText.transform.SetParent(goPlate.transform, false);
                    tna.label           = goText.AddComponent<UnityEngine.UI.Text>();
                    PrepareChild(tna.label.rectTransform);
                    tna.label.font      = this.props.labelFont;
                    tna.label.fontSize  = this.props.labelFontSize;
                    tna.label.color     = this.props.labelColor;
                    tna.label.text      = node.Label;
                    tna.label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    tna.label.verticalOverflow = VerticalWrapMode.Overflow;
                    tna.label.alignment = TextAnchor.UpperLeft;

                    if(node.Selected == true)
                        tna.plate.color = this.props.selected;
                    else
                        tna.plate.color = this.props.unselected;
                }

                if(node.HasChildren() == true)
                { 
                    GameObject goExpand = new GameObject("Expand");
                    goExpand.transform.SetParent(this.transform, false);

                    tna.expandPlate = goExpand.AddComponent<UnityEngine.UI.Image>();
                    tna.expandButton = goExpand.AddComponent<UnityEngine.UI.Button>();
                    tna.expandButton.targetGraphic = tna.expandPlate;
                    tna.expandButton.onClick.AddListener(()=>{ node.Expanded = !node.Expanded; });
                    tna.expandPlate.sprite = node.Expanded ? this.props.expandSprite : this.props.compressSprite;

                    RectTransform rtExp = tna.expandPlate.rectTransform;
                    PrepareChild(rtExp);
                    rtExp.sizeDelta = tna.expandPlate.sprite.rect.size;

                }
                else
                { 
                    if(tna.expandPlate != null)
                    { 
                        GameObject.Destroy(tna.expandPlate);
                        tna.expandPlate = null;
                        tna.expandButton = null;
                    }
                }

                tna.Show();

                float maxIconY = 0.0f;
                float leftIconWid = 0.0f;
                float rightIconWid = 0.0f;

                foreach(Node.Icon ico in node.LeftIcons())
                { 
                    Vector2 dim = ico.sprite.rect.size;
                    maxIconY = Mathf.Max(maxIconY, dim.y);
                    if(leftIconWid > 0.0f)
                        leftIconWid += this.props.iconNameSpacing;

                    leftIconWid += dim.x;
                }
                if(leftIconWid > 0.0f)
                    leftIconWid += this.props.iconNameSpacing;

                foreach(Node.Icon ico in node.RightIcons())
                { 
                    Vector2 dim = ico.sprite.rect.size;
                    maxIconY = Mathf.Max(maxIconY, dim.y);
                    if(rightIconWid > 0.0f)
                        rightIconWid += this.props.iconSpacing;

                    rightIconWid += dim.x;
                }
                if(rightIconWid > 0.0f)
                    rightIconWid += this.props.iconNameSpacing;

                TextGenerationSettings tgs = 
                    tna.label.GetGenerationSettings(
                        new Vector2(
                            float.PositiveInfinity, 
                            float.PositiveInfinity));

                TextGenerator tg = 
                    tna.label.cachedTextGenerator;

                Vector2 labelDim = 
                    new Vector2(
                        tg.GetPreferredWidth(node.Label, tgs),
                        tg.GetPreferredHeight(node.Label, tgs));

                float textPosX = this.props.horizPlateMargin + leftIconWid;
                float plateWidth = textPosX + labelDim.x + this.props.vertPlateMargin + rightIconWid;
                plateWidth = Mathf.Max(plateWidth, this.props.minSize.x);

                float plateHeight = this.props.vertPlateMargin + Mathf.Max(labelDim.y, maxIconY) + this.props.vertPlateMargin;
                plateHeight = Mathf.Max(plateHeight, this.props.minSize.y, expcmpMax.y);

                float fx = indent + expcmpMax.x + this.props.parentPlateSpacer;
                tna.plate.rectTransform.anchoredPosition = new Vector2(fx, y);
                tna.plate.rectTransform.sizeDelta = new Vector2(plateWidth, plateHeight);

                tna.label.rectTransform.anchoredPosition = new Vector2(textPosX, -(plateHeight - labelDim.y) * 0.5f);

                // Aligning the expand/compress button
                if(tna.expandPlate != null)
                { 
                    RectTransform rtExp = tna.expandPlate.rectTransform;
                    Vector2 expSz = tna.expandPlate.sprite.rect.size;
                    rtExp.sizeDelta = expSz;
                    rtExp.anchoredPosition = new Vector2(indent + expcmpMax.x - expSz.x, y - (plateHeight - expSz.y) * 0.5f);
                }

                if(node.HasLeftIcons() == true)
                { 
                    float xplace = this.props.horizPlateMargin;
                    HashSet<int> leftIDs = tna.GetLeftIconIDs();
                    foreach(Node.Icon ni in node.LeftIcons())
                    { 
                        leftIDs.Remove(ni.id);
                        UnityEngine.UI.Image imgIco = tna.GetIconImage(ni.id, true);
                        Vector2 sz = imgIco.sprite.rect.size;
                        imgIco.rectTransform.anchoredPosition = new Vector2(xplace, -(plateHeight - sz.y) * 0.5f);
                        xplace += sz.x;
                        xplace += this.props.iconSpacing;

                    }
                    foreach(int i in leftIDs)
                        tna.DestroyIconAssets(i);
                }
                else
                    tna.ClearLeftIcons();

                if(node.HasRightIcons() == true)
                { 
                    float xplace = plateWidth;
                    HashSet<int> rightIDs = tna.GetRightIconIDs();
                    foreach(Node.Icon ni in node.RightIcons())
                    { 
                        rightIDs.Remove(ni.id);
                        UnityEngine.UI.Image imgIco = tna.GetIconImage(ni.id, true);
                        Vector2 sz = imgIco.sprite.rect.size;
                        xplace -= sz.x;
                        imgIco.rectTransform.anchoredPosition = new Vector2(xplace, -(plateHeight - sz.y) * 0.5f);
                        xplace -= this.props.iconNameSpacing;
                    }
                    foreach(int i in rightIDs)
                        tna.DestroyIconAssets(i);
                }
                else
                    tna.ClearRightIcons();

                maxX = Mathf.Max(maxX, fx + plateWidth);
                y -= plateHeight;

                if (node.HasChildren() == true)
                { 
                    if(node.Expanded == true)
                    { 
                        float nextIndent = indent + this.props.indentAmt;

                        foreach (Node nc in node.GetChildren())
                        {
                            y -= this.props.vNodeSpace; 
                            this.LayoutNode(nc, nextIndent, ref y, ref maxX, expcmpMax, used);
                        }
                    }
                    else
                    {
                        foreach (Node nc in node.GetChildren())
                            this.DisableHierarchy(nc);
                    }
                }
            }

            public void ClearAllSelections()
            { 
                // Copy because we can't modify the container we're iterating
                List<Node> seled = new List<Node>(this.selected);
                foreach(Node n in seled)
                    n.Selected = false;
            }

            public void SelectNode(Node node, bool clearPrevSelection)
            { 
                if(clearPrevSelection == true)
                    this.ClearAllSelections();

                node.Selected = true;
            }

            public void ToggleSelection(Node node, bool clearPrevSelection)
            { 
                bool oldSel = node.Selected;
                if(clearPrevSelection == true)
                    this.ClearAllSelections();

                node.Selected = !oldSel;
            }

            public void NotifySelection(Node node, bool sel)
            { 
                if(sel == false)
                    this.selected.Remove(node);
                else
                    this.selected.Add(node);

                foreach(ITreeHandler ith in this.subscribers)
                    ith.OnNodeSelected(this, node, sel);
            }

            public void NotifyRemoval(Node node)
            { 
                this.selected.Remove(node);
            }

            private void NodeClickHandler(Node n)
            { 
                bool ctrlDown = Input.GetKeyDown( KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);

                this.SelectNode(n, ctrlDown == false);
            }
        }
    }
}