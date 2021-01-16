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
    public class Tree : UnityEngine.UI.Image
    {
        /// <summary>
        /// A counter to generate unique IDs for tree nodes. These IDs are explicitly 
        /// functional, but can be used as a convenience feature or for tracking nodes
        /// during debugging.
        /// </summary>
        static int idCounter = 0;

        /// <summary>
        /// Allocate a new id.
        /// </summary>
        /// <returns>The new ID.</returns>
        public static int GetNewID()
        { 
            return ++idCounter;
        }

        /// <summary>
        /// The Unity UI assets for the nodes.
        /// </summary>
        Dictionary<Node, TreeNodeAsset> nodeAssets = 
            new Dictionary<Node, TreeNodeAsset>();

        /// <summary>
        /// Nodes that are dirty. We may be able to cull operations when cleaning
        /// the tree if we're able to analyze what's dirty.
        /// </summary>
        HashSet<Node> dirtyItems = new HashSet<Node>();

        /// <summary>
        /// The root node. It should be accessed with GetRoot(). If the root is 
        /// null when GetRoot() is called, a default root will be created.
        /// </summary>
        Node root = null;

        /// <summary>
        /// The tree properties that specify various assets and behaviours.
        /// </summary>
        public TreeProps props;

        /// <summary>
        /// An object with StartCoroutine() implemented that can  run the dirty flag handling
        /// coroutine. If null, the Tree object itself is default.
        /// </summary>
        /// <remarks>Usually this will be null, but may be useful if the Tree can turn off but
        /// having the dirty coroutine is still desired. In this case, a managing GameObject that
        /// is known to always be active and enabled can be specified.</remarks>
        public MonoBehaviour coroutineHost;

        /// <summary>
        /// The coroutine for handling the dirty flag. If the coroutine is null, the Tree is not dirty.
        /// The coroutine will be running in coroutineHost, or this is coroutineHost is null.
        /// </summary>
        private Coroutine dirtyUpdate = null;

        /// <summary>
        /// Subscribers to receive messages when the tree and its nodes are modified, or if 
        /// selection or compression states change.
        /// </summary>
        public HashSet<ITreeHandler> subscribers = new HashSet<ITreeHandler>();

        public MonoBehaviour GetCoroutineHost() => this.coroutineHost ?? this;

        public HashSet<Node> selected = new HashSet<Node>();

        /// <summary>
        /// If true, a quad is drawn for the background of the tree. If false, no background.
        /// Since the Tree derives off a Unity image, all this does is decide whether to allow
        /// the base Image behaviour or bypass it.
        /// </summary>
        public bool imageFill = true;

        /// <summary>
        /// If true, multiple nodes can be simultaneously selected. If false, only 1 node (at most)
        /// is allowed to be selected at any given time.
        /// </summary>
        public bool allowMultiSel = true;

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper toFill)
        { 
            if(this.imageFill == false)
            {
                toFill.Clear();
                return;
            }

            base.OnPopulateMesh(toFill);
        }

        /// <summary>
        /// Get the root node.
        /// </summary>
        /// <returns>The root node.</returns>
        /// <remarks>The root node is not directly visible in tree. Its children will be the
        /// first depth rendered.</remarks>
        /// <remarks>If the tree does not have a root, one is automatically created for the tree.</remarks>
        public Node GetRoot()
        { 
            if(this.root == null)
                this.root = new Node(this, "$_ROOT");

            return this.root;
        }

        /// <summary>
        /// Add a node to the tree.
        /// </summary>
        /// <param name="label">The text display of the node.</param>
        /// <param name="parent">The parent node, or null for the root node.</param>
        /// <returns>True if the node was successfully added. Else, false.</returns>
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

        /// <summary>
        /// Check if a node is managed by the tree.
        /// </summary>
        /// <param name="node">The node to query.</param>
        /// <returns>True if the node is in the hierarchy. Else, false.</returns>
        public bool HasNode(Node node)
        { 
            return this.nodeAssets.ContainsKey(node);
        }

        /// <summary>
        /// Logs a node as dirty and flags the tree as dirty.
        /// </summary>
        /// <param name="node">The node to log.</param>
        public void FlagDirty(Node node)
        {
            this.dirtyItems.Add(node);
            this.FlagDirty();
        }

        /// <summary>
        /// Flags the tree as dirty and auto-queues the process for
        /// cleaning the tree if not already queued.
        /// </summary>
        /// <returns></returns>
        public bool FlagDirty()
        { 
            if(this.dirtyUpdate != null)
                return false;

            this.dirtyUpdate = 
                this.GetCoroutineHost().StartCoroutine(this.DirtyCoroutine());

            return true;
        }

        /// <summary>
        /// Coroutine to automatically handle processing the dirty flag.
        /// 
        /// Should only be called by FlagDirty.
        /// </summary>
        /// <returns>The IEnumerator for Unity management.</returns>
        public IEnumerator DirtyCoroutine()
        { 
            yield return new WaitForEndOfFrame();

            bool doLayout = false;

            Node.DirtyItems accumulatedDirty = 0;

            foreach (Node n in this.dirtyItems)
            { 
                Node.DirtyItems dflag = n.DirtyFlags;
                accumulatedDirty |= dflag;
                n.ClearDirty();

                if((dflag & Node.DirtyItems.RemoveTree) != 0)
                { 
                    // If we're deleting, we're also going to handle the
                    // assets for the rest of the hierarchy too, which 
                    // means we'll need to traverse it.
                    doLayout = true;
                    Queue<Node> toDel = new Queue<Node>();
                    toDel.Enqueue(n);
                    // Delete assets for it and its children - and then we're done
                    // processing this node.

                    while(toDel.Count > 0)
                    {
                        Node nDeling = toDel.Dequeue();
                        foreach(Node cn in nDeling.GetChildren())
                            toDel.Enqueue(cn);

                        TreeNodeAsset tnaDel;
                        if(this.nodeAssets.TryGetValue(nDeling, out tnaDel) == true)
                        {
                            this.nodeAssets.Remove(nDeling);
                            if(tnaDel.plate != null)
                                tnaDel.Destroy();
                        }
                    }
                }

                TreeNodeAsset tna;
                this.nodeAssets.TryGetValue(n, out tna);

                if(tna != null && tna.plate != null)
                {
                    if ((dflag & Node.DirtyItems.Selection) != 0)
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
                        if(tna.expandPlate == null)
                            doLayout = true;
                        else
                        { 
                            tna.UpdateExpandCompress(this.props, true);
                        }
                    }

                    if((dflag & (Node.DirtyItems.Reparent|Node.DirtyItems.RemoveTree|Node.DirtyItems.ChildChange|Node.DirtyItems.ChangedIcons)) != 0)
                        doLayout = true;
                }
                else
                {
                    // If it's empty, we need to do a full process of the tree to recreate it.
                    doLayout = true;
                }
            }

            if(doLayout == true)
                this.LayoutTree(accumulatedDirty);

            this.dirtyUpdate = null;
        }

        /// <summary>
        /// Layout the entire tree. This includes both the nodes and the tree plate.
        /// </summary>
        /// <remarks>The dirty flags that lead to tree layout. Only used for subscriber
        /// events data.</remarks>
        /// <returns></returns>
        public Vector2 LayoutTree(Node.DirtyItems dirtyItems = 0)
        { 
            float indent = this.props.startOffset.x;
            float y = -this.props.startOffset.y;
            float maxX = indent;

            HashSet<TreeNodeAsset> used = new HashSet<TreeNodeAsset>(this.nodeAssets.Values);
            Vector2 ceDim = this.props.CalculateExpandCompressMaxs();

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

            RectTransform thisRT = this.rectTransform;
            if (this.props.widthMode == TreeProps.WidthMode.TouchEdge)
            {
                thisRT.offsetMin = Vector2.zero;
                thisRT.offsetMax = Vector2.zero;
                //
                thisRT.sizeDelta = new Vector2(0.0f, -y);
                thisRT.anchorMin = new Vector2(0.0f, 1.0f);
                thisRT.anchorMax = new Vector2(1.0f, 1.0f);
            }
            else if(this.props.widthMode == TreeProps.WidthMode.Leave)
            {
                // don't mess with anything except our height
                thisRT.sizeDelta = new Vector2(thisRT.sizeDelta.x, -y);
            }
            else
            {
                thisRT.offsetMin = Vector2.zero;
                thisRT.offsetMax = Vector2.zero;
                //
                thisRT.sizeDelta = new Vector2(maxX, -y);
                thisRT.anchorMin = new Vector2(0.0f, 1.0f);
                thisRT.anchorMax = new Vector2(0.0f, 1.0f);
            }

            if(this.props.backgroundFill != TreeProps.BackgroundFill.Leave)
            {
                bool fill = true;
                if(this.props.backgroundFill == TreeProps.BackgroundFill.Empty)
                    fill = false;

                if(this.imageFill != fill)
                { 
                    this.imageFill = fill;
                    this.SetVerticesDirty();
                }
            }

            Vector2 finalSize = new Vector2(maxX, -y);

            if (this.subscribers != null)
            { 
                foreach(ITreeHandler ith in this.subscribers)
                    ith.OnTreeLayout(this, dirtyItems, finalSize);
            }

            return finalSize;
        }

        /// <summary>
        /// Set standard RectTransform values to a specified RectTransform.
        /// </summary>
        /// <param name="rt">The RectTransform to modify.</param>
        public static void PrepareChild(RectTransform rt)
        { 
            rt.localScale       = Vector2.one;
            rt.localRotation    = Quaternion.identity;

            rt.anchorMin        = new Vector2(0.0f, 1.0f);
            rt.anchorMax        = new Vector2(0.0f, 1.0f);
            rt.pivot            = new Vector2(0.0f, 1.0f);
        }

        /// <summary>
        /// Hides the assets for a node hierarchy.
        /// </summary>
        /// <param name="node">The node to hide the assets of its hierarchy for.</param>
        public void DisableHierarchy(Node node)
        {
            TreeNodeAsset tna;
            if (nodeAssets.TryGetValue(node, out tna) == true)
                tna.Hide();

            foreach(Node n in node.GetChildren())
                this.DisableHierarchy(n);
        }

        /// <summary>
        /// Recursive call to handle the assets of a node hierarchy.
        /// </summary>
        /// <param name="node">The node to manage assets for.</param>
        /// <param name="indent">The amount to indent for every hierarchy depth.</param>
        /// <param name="y">The current vertical placement cursor.</param>
        /// <param name="maxX">The output variable to track the rightmost node.</param>
        /// <param name="expcmpMax">The precalculated max size between the compress and expand sprites.</param>
        /// <param name="used">A collection tracking variables that were managed.</param>
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
                if(tna.expandPlate == null)
                {
                    tna.CreateExpandCompressAssets(this.rectTransform);
                    tna.UpdateExpandCompress(this.props, false);
                }
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

            // We calculate the right icons as 
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

            tna.label.rectTransform.sizeDelta = labelDim;

            float textPosX = this.props.horizPlateMargin + leftIconWid;
            float plateWidth = textPosX + labelDim.x + this.props.vertPlateMargin + rightIconWid + this.props.horizPlateMargin;
            plateWidth = Mathf.Max(plateWidth, this.props.minSize.x);

            float plateHeight = this.props.vertPlateMargin + Mathf.Max(labelDim.y, maxIconY) + this.props.vertPlateMargin;
            plateHeight = Mathf.Max(plateHeight, this.props.minSize.y, expcmpMax.y);

            float fx = TreeNodeAsset.GetNodePlateX(indent, expcmpMax.x, this.props.parentPlateSpacer);

            RectTransform rtPlate = tna.plate.rectTransform;
            rtPlate.anchoredPosition = new Vector2(fx, y);

            if (this.props.maxMode == TreeProps.MaxXMode.TouchEdge)
            {
                rtPlate.sizeDelta = new Vector2(0.0f, plateHeight);
                rtPlate.offsetMax = new Vector2(0.0f, rtPlate.offsetMax.y);
                rtPlate.anchorMin = new Vector2(0.0f, 1.0f);
                rtPlate.anchorMax = new Vector2(1.0f, 1.0f);
            }
            else
            {
                rtPlate.sizeDelta = new Vector2(plateWidth, plateHeight);
                rtPlate.anchorMin = new Vector2(0.0f, 1.0f);
                rtPlate.anchorMax = new Vector2(0.0f, 1.0f);
            }

            tna.label.rectTransform.anchoredPosition = new Vector2(textPosX, -(plateHeight - labelDim.y) * 0.5f);

            if(node.HasLeftIcons() == true)
            { 
                float xplace = this.props.horizPlateMargin;
                HashSet<string> leftIDs = tna.GetLeftIconIDs();
                foreach(Node.Icon ni in node.LeftIcons())
                { 
                    leftIDs.Remove(ni.id);
                    UnityEngine.UI.Image imgIco = tna.GetIconImage(ni.id, true);
                    imgIco.sprite = ni.sprite;
                    Vector2 sz = imgIco.sprite.rect.size;
                    imgIco.rectTransform.anchoredPosition = new Vector2(xplace, -(plateHeight - sz.y) * 0.5f);
                    xplace += sz.x;
                    xplace += this.props.iconSpacing;

                    UnityEngine.UI.Button btn = imgIco.GetComponent<UnityEngine.UI.Button>();
                    if(btn != null)
                    {
                        btn.enabled = (ni.onClick != null);
                        btn.interactable = btn.enabled;
                    }

                }
                foreach(string i in leftIDs)
                    tna.DestroyIconAssets(i);
            }
            else
                tna.ClearLeftIcons();

            // Because the props.maxMode can be TreeProps.MaxXMode.TouchEdge (docks with the right edge) - 
            // we don't want to branch so we write these layouts to be relative to the right size.
            if (node.HasRightIcons() == true)
            { 
                float xplace = -this.props.horizPlateMargin;
                HashSet<string> rightIDs = tna.GetRightIconIDs();

                // Because we're right aligned, that means we need to figure out the alignment
                // from the right to left.
                List<Node.Icon> rightIcos = new List<Node.Icon>(node.RightIcons());
                for(int i = rightIcos.Count - 1; i >= 0; --i)
                {
                    Node.Icon ni = rightIcos[i];
                    rightIDs.Remove(ni.id);

                    UnityEngine.UI.Image imgIco = tna.GetIconImage(ni.id, true);
                    Vector2 sz = imgIco.sprite.rect.size;
                    xplace -= sz.x;

                    RectTransform rtIco = imgIco.rectTransform;
                    rtIco.anchorMin = new Vector2(1.0f, 1.0f);
                    rtIco.anchorMax = new Vector2(1.0f, 1.0f);
                    rtIco.pivot = new Vector2(1.0f, 1.0f);

                    rtIco.anchoredPosition = new Vector2(-xplace - sz.x, -(plateHeight - sz.y) * 0.5f);
                    xplace -= this.props.iconNameSpacing + sz.x;
                }
                foreach(string i in rightIDs)
                    tna.DestroyIconAssets(i);
            }
            else
                tna.ClearRightIcons();

            // Aligning the expand/compress button. We do this at the end (before handling children) because
            // we need the plate calculated in the right X position before calling the correct function.
            if (tna.expandPlate != null)
            {
                tna.RealignExpandCompress(indent, expcmpMax);
                // RectTransform rtExp = tna.expandPlate.rectTransform;
                // Vector2 expSz = tna.expandPlate.sprite.rect.size;
                // rtExp.sizeDelta = expSz;
                // rtExp.anchoredPosition = new Vector2(indent + expcmpMax.x - expSz.x, y - (plateHeight - expSz.y) * 0.5f);
            }

            maxX = Mathf.Max(maxX, fx + plateWidth);
            y -= Mathf.Max(plateHeight, node.MinHeight);

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

        public void SelectNode(Node node, bool clearPrevSelection)
        { 
            if(clearPrevSelection == true)
                this.DeselectAll();

            node.Selected = true;
        }

        public void ToggleSelection(Node node, bool clearPrevSelection)
        { 
            bool oldSel = node.Selected;
            if(clearPrevSelection == true)
                this.DeselectAll();

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

        public RectTransform GetNodeRectTransform(Node n)
        {
            TreeNodeAsset tna;
            if(this.nodeAssets.TryGetValue(n, out tna) == false)
                return null;

            return tna.plate.rectTransform;
        }

        /// <summary>
        /// Called when a node is clicked.
        /// </summary>
        /// <param name="n">The node that was clicked.</param>
        /// <remarks>Might not be fully implemented yet.</remarks>
        private void NodeClickHandler(Node n)
        { 
            bool ctrlDown = Input.GetKeyDown( KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);

            this.SelectNode(n, ctrlDown == false);
        }

        /// <summary>
        /// Clear all nodes in the tree.
        /// </summary>
        /// <remarks>Includes the root.</remarks>
        public void Clear()
        { 
            this.root = null;

            foreach(TreeNodeAsset tna in this.nodeAssets.Values)
                tna.Destroy();

            this.nodeAssets.Clear();

            this.dirtyItems.Clear();
            this.FlagDirty();
        }

        /// <summary>
        /// Deselect all nodes.
        /// </summary>
        public void DeselectAll()
        { 
            List<Node> lst = new List<Node>(this.selected);
            this.selected.Clear();

            foreach(Node n in lst)
                n.Selected = false;
        }
    }
}