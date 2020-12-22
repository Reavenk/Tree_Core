using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PxPre
{
    namespace Tree
    {
        public class Tree : UnityEngine.UI.Image
        {
            class Node
            {
                public Node parent;
                public int idx;
                public string text;

                public bool selected = false;
                public bool expanded = true;
                public List<Node> children = null;
            }

            class TreeNodeAssets
            { 
                public Node node;

                public UnityEngine.UI.Image plate;
                public UnityEngine.UI.Text label;
                public UnityEngine.UI.Button btnPlate;

                public UnityEngine.UI.Image expandPlate;
                public UnityEngine.UI.Button expandButton;
            }

            Dictionary<int, Node> nodeLookup = 
                new Dictionary<int, Node>();

            Dictionary<Node, TreeNodeAssets> nodeAssets = 
                new Dictionary<Node, TreeNodeAssets>();

            List<Node> rootNodes = 
                new List<Node>();

            public TreeProps props;

            private Coroutine dirtyUpdate = null;

            static int idctr = 0;

            static int GetNewID()
            { 
                ++idctr;
                return idctr;
            }

            public int AddNode(string text, int parent)
            { 
                if(parent == -1)
                    return this.AddNodeToRoot(text);

                Node nodeParent;
                if(this.nodeLookup.TryGetValue(parent, out nodeParent) == false)
                    return -1;

                Node node = new Node();
                node.text = text;
                node.idx = GetNewID();

                if(nodeParent.children == null)
                    nodeParent.children = new List<Node>();

                node.parent = nodeParent;

                return node.idx;
            }

            public int AddNodeToRoot(string text)
            { 
                Node node = new Node();
                node.text = text;
                node.idx = GetNewID();

                this.rootNodes.Add(node);

                this.FlagDirty();
                return node.idx;
            }

            public bool HasNode(int idx)
            { 
                return this.nodeLookup.ContainsKey(idx);
            }

            public bool MoveNode(int idChild, int idNewParent)
            { 
                //Node nchild;
                //if(this.nodeLookup.TryGetValue(idChild, out nchild) == false)
                //    return false;
                //
                //if(idNewParent == -1)
                //{ 
                //
                //}
                // TODO

                return false;
            }

            public bool RemoveNode(int idx)
            {
                if(this.nodeLookup.Remove(idx) == true)
                { 
                    this.FlagDirty();
                    return true;
                }
                return false;

            }

            public string GetNodeText(int idx)
            {
                Node node;
                if(this.nodeLookup.TryGetValue(idx, out node) == false)
                    return string.Empty;

                return node.text;
            }

            public bool SetNodeText(int idx, string text)
            { 
                Node node;
                if(this.nodeLookup.TryGetValue(idx, out node) == false)
                    return false;

                node.text = text;
                return true;
            }

            public bool ExpandNode(int idx, bool expand, bool hierarchy)
            {    
                Node node;
                if(this.nodeLookup.TryGetValue(idx, out node) == false)
                    return false;

                this.ExpandNode(node, expand, hierarchy);
                return true;
            }

            private void ExpandNode(Node node, bool expand, bool hierarchy)
            {
                node.expanded = expand;

                if(hierarchy == true)
                { 
                    if(node.children != null)
                    { 
                        foreach(Node n in node.children)
                            this.ExpandNode(n, expand, true);
                    }
                }
            }

            public bool FlagDirty()
            { 
                if(this.dirtyUpdate != null)
                    return false;

                this.dirtyUpdate = 
                    this.StartCoroutine(this.DirtyCoroutine());

                return true;
            }

            public IEnumerator DirtyCoroutine()
            { 
                yield return new WaitForEndOfFrame();

                this.LayoutTree();

                this.dirtyUpdate = null;
            }

            public Vector2 LayoutTree()
            { 
                float indent = this.props.startOffset.x;
                float y = -this.props.startOffset.y;
                float maxX = indent;

                HashSet<TreeNodeAssets> used = new HashSet<TreeNodeAssets>(this.nodeAssets.Values);

                foreach(Node n in this.rootNodes)
                    this.LayoutNode(n, indent, ref y, ref maxX, used);

                foreach(TreeNodeAssets a in used)
                { 
                    this.nodeAssets.Remove(a.node);
                    if(a.plate.gameObject != null)
                        GameObject.Destroy(a.plate.gameObject);

                    if(a.expandPlate.gameObject != null)
                        GameObject.Destroy(a.expandPlate.gameObject);
                }

                return new Vector2(maxX, y);
            }

            static void PrepareChild(RectTransform rt)
            { 
                rt.localScale       = Vector2.one;
                rt.localRotation    = Quaternion.identity;

                rt.anchorMin        = new Vector2(0.0f, 1.0f);
                rt.anchorMax        = new Vector2(0.0f, 1.0f);
                rt.pivot            = new Vector2(0.0f, 1.0f);
            }

            private void LayoutNode(Node node, float indent, ref float y, ref float maxX, HashSet<TreeNodeAssets> used)
            { 
                TreeNodeAssets tna;
                if(nodeAssets.TryGetValue(node, out tna) == false)
                {
                    tna = new TreeNodeAssets();

                    GameObject goPlate  = new GameObject("TreeNode");
                    goPlate.transform.SetParent(this.transform);
                    tna.plate           = goPlate.AddComponent<UnityEngine.UI.Image>();
                    PrepareChild(tna.plate.rectTransform);
                    tna.plate.sprite    = this.props.plate;
                    tna.plate.color     = this.props.unselected;
                    tna.plate.type      = Type.Sliced;

                    tna.btnPlate        = goPlate.AddComponent<UnityEngine.UI.Button>();
                    tna.btnPlate.targetGraphic = tna.plate;

                    GameObject goText   = new GameObject("TreeNodeLabel");
                    goText.transform.SetParent(goPlate.transform);
                    tna.label           = goText.AddComponent<UnityEngine.UI.Text>();
                    PrepareChild(tna.expandPlate.rectTransform);
                    tna.label.font      = this.props.labelFont;
                    tna.label.fontSize  = this.props.labelFontSize;
                    tna.label.color     = this.props.labelColor;
                    tna.label.text      = node.text;
                    tna.label.alignment = TextAnchor.UpperLeft;

                    nodeAssets.Add(node, tna);
                }

                TextGenerationSettings tgs = 
                    tna.label.GetGenerationSettings(
                        new Vector2(
                            float.PositiveInfinity, 
                            float.PositiveInfinity));

                TextGenerator tg = 
                    tna.label.cachedTextGenerator;

                Vector2 labelDim = 
                    new Vector2(
                        tg.GetPreferredWidth(node.text, tgs),
                        tg.GetPreferredHeight(node.text, tgs));

                float plateWidth = this.props.leftMargin + labelDim.x + this.props.rightMargin;
                float plateHeight = this.props.topMargin + labelDim.y + this.props.botMargin;

                float fx = indent + this.props.expandSprite.rect.width + this.props.parentPlateSpacer;
                tna.plate.rectTransform.anchoredPosition = new Vector2(fx, y);
                tna.plate.rectTransform.sizeDelta = new Vector2(plateWidth, plateHeight);

                if(node.children != null)
                { 
                    if(node.expanded == true)
                    { 
                        float nextIndent = indent + this.props.indentAmt;

                        foreach (Node nc in node.children)
                            this.LayoutNode(nc, nextIndent, ref y, ref maxX, used);
                    }
                    else
                    { 
                    }
                }
            }
        }
    }
}