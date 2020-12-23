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
        public class Node
        {
            [System.Flags]
            public enum DirtyItems
            { 
                Name            = 1 << 0,
                Selection       = 1 << 1,
                Expand          = 1 << 2,
                NewTree         = 1 << 3,
                RemoveTree      = 1 << 4,
                Reparent        = 1 << 5,
                ChildChange     = 1 << 6,
                ChangedIcons    = 1 << 7
            }

            public struct Icon
            { 
                public int id;
                public Sprite sprite;
                public Vector2 scale;
                public System.Action onClick; 
            }

            Tree ownerTree;
            public Tree OwnerTree {get => this.ownerTree; }

            string label;
            public readonly int id;

            private bool selected = false;
            private bool expanded = true;

            private DirtyItems dirtyFlags = 0;
            public DirtyItems DirtyFlags {get=>this.dirtyFlags; }

            Node parent;
            List<Node> children = null;

            List<Icon> leftIcons = null;
            List<Icon> rightIcons = null;

            public Node(Tree tree, string label)
            { 
                this.ownerTree = tree;
                if(this.ownerTree != null)
                    this.FlagDirty(DirtyItems.NewTree);

                this.label = label;
                this.id = Tree.GetNewID();
            }

            public bool _SetParent(Node node, bool nullIsRoot = true)
            { 
                if(node == null)
                {
                    if(this.parent != null)
                        this.parent.RemoveChild(node);

                    if(this.ownerTree != null && nullIsRoot == true)
                        this._SetParent(this.ownerTree.GetRoot());

                    return true;
                }
                else
                { 
                    if(node.ownerTree == null)
                        return false;

                    if (this.ownerTree != node.ownerTree)
                        return false;

                    // Check for (forbidden) cyclic referencing
                    for(Node it = node.parent; it != null; it = it.parent)
                    { 
                        if(it == this)
                            return false;
                    }

                    if(node.children == null)
                        node.children = new List<Node>();

                    node.children.Add(this);

                    node.FlagDirty(DirtyItems.ChildChange);
                    this.FlagDirty(DirtyItems.Reparent);

                    return true;
                }
            }

            public bool RemoveChild(Node node)
            { 
                if(node == null)
                    return false;

                if(this.children == null)
                    return false;

                if(this.children.Remove(node) == false)
                    return false;

                this.FlagDirty(DirtyItems.ChildChange);
                node.FlagDirty(DirtyItems.Reparent);

                node.parent = null;

                return true;
            }

            public bool Expanded
            { 
                get => this.expanded;
                set
                { 
                    if(this.expanded == value)
                        return;

                    this.expanded = value; 
                    this.FlagDirty(DirtyItems.Expand); 
                }
            }

            public bool Selected
            { 
                get => this.selected;
                set
                { 
                    if(this.selected == value)
                        return;

                    this.selected = value;
                    this.FlagDirty(DirtyItems.Selection);
                }
            }

            public string Label
            { 
                get => this.label;
                set
                { 
                    this.label = value;
                    this.FlagDirty(DirtyItems.Name);
                }
            }

            public void FlagDirty(DirtyItems dType)
            { 
                this.dirtyFlags |= dType;

                if(this.ownerTree != null)
                {
                    this.ownerTree.FlagDirty(this);

                    if((dType & DirtyItems.Selection) != 0)
                        this.ownerTree.NotifySelection(this, this.selected);
                }
            }

            public void ClearDirty()
            { 
                this.dirtyFlags = 0;
            }

            public bool IsDirty()
            { 
                return this.dirtyFlags != 0;
            }

            public bool IsInTree()
            {
                return this.ownerTree != null;
            }

            public bool HasChildren()
            { 
                return 
                    this.children != null && 
                    this.children.Count > 0;
            }

            public IEnumerable<Node> GetChildren()
            { 
                if(this.children == null)
                    yield break;

                foreach(Node n in this.children)
                    yield return n;
            }

            public IEnumerable<Icon> LeftIcons()
            { 
                if(this.leftIcons == null)
                    yield break;

                foreach(Icon ico in this.leftIcons)
                    yield return ico;
            }

            public IEnumerable<Icon> RightIcons()
            { 
                if(this.rightIcons == null)
                    yield break;

                foreach(Icon ico in this.rightIcons)
                    yield return ico;
            }

            public bool HasLeftIcons()
            {
                return this.leftIcons != null && this.leftIcons.Count > 0;
            }

            public bool HasRightIcons()
            { 
                return this.rightIcons != null && this.rightIcons.Count > 0;
            }

            public Sprite GetIconSprite(int idx)
            { 
                if(this.leftIcons != null)
                { 
                    foreach(Icon i in this.leftIcons)
                    { 
                        if(i.id == idx)
                            return i.sprite;
                    }
                }

                if(this.rightIcons != null)
                { 
                    foreach(Icon i in this.rightIcons)
                    { 
                        if(i.id == idx)
                            return i.sprite;
                    }
                }

                return null;
            }

            public bool Destroy()
            { 
                if(this.ownerTree == null || this.parent == null)
                    return false;

                this.parent.RemoveChild(this);

                Tree oldOwner = this.ownerTree;
                this.ownerTree = null;
                oldOwner.NotifyRemoval(this);
                return true;
            }
        }
    }
}