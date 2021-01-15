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
        /// An entry in the Tree.
        /// </summary>
        /// <remarks>These are mostly conceptual entities with properties and heirarchy
        /// tracking. The actual UI assets for these elements exist in the Tree as a map,
        /// with the Node used as the key to those assets.</remarks>
        public class Node
        {
            /// <summary>
            /// Bitflags to track why the node is dirty. The flags are cleared when the Tree
            /// processes them during cleaning.
            /// </summary>
            [System.Flags]
            public enum DirtyItems
            { 
                /// <summary>
                /// The name has changed.
                /// </summary>
                Name            = 1 << 0,

                /// <summary>
                /// The node's selection state has changed.
                /// </summary>
                Selection       = 1 << 1,

                /// <summary>
                /// The node's compression/expansion state has changed.
                /// </summary>
                Expand          = 1 << 2,

                /// <summary>
                /// The node has been added to a tree.
                /// </summary>
                NewTree         = 1 << 3,

                /// <summary>
                /// The node has been remove from a tree.
                /// </summary>
                RemoveTree      = 1 << 4,

                /// <summary>
                /// The node has been reparented.
                /// </summary>
                Reparent        = 1 << 5,

                /// <summary>
                /// The node children stae has changed. Either one or more child
                /// was added or removed.
                /// </summary>
                ChildChange     = 1 << 6,

                /// <summary>
                /// The node's icon collection has changed.
                /// </summary>
                ChangedIcons    = 1 << 7,

                // The minimum height has changed
                MinHeight = 1 << 8
            }

            /// <summary>
            /// An icon displayed in the node.
            /// </summary>
            public struct Icon
            { 
                /// <summary>
                /// The id, used to identify the icon when requesting
                /// a modification.
                /// </summary>
                public int id;

                /// <summary>
                /// The sprite;
                /// </summary>
                public Sprite sprite;

                /// <summary>
                /// The sprite's scale.
                /// </summary>
                public Vector2 scale;

                /// <summary>
                /// The action to perform is the icon is clicked. Set as null to disable
                /// interactivity.
                /// </summary>
                public System.Action onClick; 
            }

            /// <summary>
            /// The tree that we belong to - or false if the node is orphaned/deactivated.
            /// </summary>
            Tree ownerTree;

            /// <summary>
            /// Public accessor to the Tree owner.
            /// </summary>
            public Tree OwnerTree {get => this.ownerTree; }

            /// <summary>
            /// The string that should be displayed.
            /// </summary>
            string label;

            /// <summary>
            /// Unique identifier.
            /// </summary>
            public readonly int id;

            /// <summary>
            /// If true, the node should be selected. Else, false.
            /// </summary>
            private bool selected = false;

            /// <summary>
            /// If true, the node should be expanded in the tree. If false, the 
            /// node should be compressed.
            /// </summary>
            private bool expanded = true;

            /// <summary>
            /// The dirty flags.
            /// </summary>
            private DirtyItems dirtyFlags = 0;

            /// <summary>
            /// Public accessor to the dirty flags.
            /// </summary>
            public DirtyItems DirtyFlags {get=>this.dirtyFlags; }

            /// <summary>
            /// The Node's parent in the tree hierarchy.
            /// </summary>
            Node parent;

            /// <summary>
            /// The node's children in the tree hierarchy.
            /// </summary>
            List<Node> children = null;

            /// <summary>
            /// The icons displayed to the left of the label.
            /// </summary>
            List<Icon> leftIcons = null;

            /// <summary>
            /// The icons displayed to the right of the label.
            /// </summary>
            List<Icon> rightIcons = null;

            float minHeight = 0;
            public float MinHeight 
            {
                get=>this.minHeight;
                set
                { 
                    if(this.minHeight == value)
                        return;

                    this.minHeight = value;
                    this.FlagDirty(DirtyItems.MinHeight);
                }
            }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="tree">The tree that's managing the node.</param>
            /// <param name="label">The starting label value.</param>
            public Node(Tree tree, string label)
            { 
                this.ownerTree = tree;
                if(this.ownerTree != null)
                    this.FlagDirty(DirtyItems.NewTree);

                this.label = label;
                this.id = Tree.GetNewID();
            }

            /// <summary>
            /// Set the parent of the node.
            /// Should only be used internall by the Node, or by the Tree.
            /// </summary>
            /// <param name="node">The parent's new node.</param>
            /// <param name="nullIsRoot">
            /// If true, a null value defaults to the tree's root node.
            /// If false, a null value detaches the node from the heirarchy.</param>
            /// <returns>True, if successful. Else, false.</returns>
            public bool _SetParent(Node node, bool nullIsRoot = true)
            { 
                if(node == null)
                {
                    if(this.parent != null)
                        this.parent.RemoveChild(node);

                    if(this.ownerTree != null && nullIsRoot == true)
                        this._SetParent(this.ownerTree.GetRoot());

                    this.parent = null;
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

                    this.parent = node;
                    node.FlagDirty(DirtyItems.ChildChange);
                    this.FlagDirty(DirtyItems.Reparent);

                    return true;
                }
            }

            /// <summary>
            /// Remove a child.
            /// </summary>
            /// <param name="node">The node to remove.</param>
            /// <returns>True, if successful. Else, false.</returns>
            /// <remarks>Also sets the appropriate dirty flags and notifies the Tree.</remarks>
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
                node.FlagDirty(DirtyItems.RemoveTree);

                node.parent = null;

                return true;
            }

            /// <summary>
            /// Public property for the expanded state. Automatically set the dirty flags
            /// and flags its Tree as dirty.
            /// </summary>
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

            /// <summary>
            /// Public property for the selection state.
            /// </summary>
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

            /// <summary>
            /// Public property for the label.
            /// </summary>
            public string Label
            { 
                get => this.label;
                set
                { 
                    this.label = value;
                    this.FlagDirty(DirtyItems.Name);
                }
            }

            /// <summary>
            /// Set dirty flags.
            /// </summary>
            /// <param name="dType"></param>
            /// <remarks>Also notifies tree for logging the node as dirty, and flags the tree as dirty.</remarks>
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

            /// <summary>
            /// Clear the dirty state of the node.
            /// </summary>
            public void ClearDirty()
            { 
                this.dirtyFlags = 0;
            }

            /// <summary>
            /// Check if the node is dirty.
            /// </summary>
            /// <returns>True, if the node is dirty. Else, false.</returns>
            public bool IsDirty()
            { 
                return this.dirtyFlags != 0;
            }

            /// <summary>
            /// Check if the node is in a tree.
            /// </summary>
            /// <returns>True, if the node is in a tree. Else, false.</returns>
            public bool IsInTree()
            {
                return this.ownerTree != null;
            }

            /// <summary>
            /// Check if the node has any children.
            /// </summary>
            /// <returns>True, if the node has any children. Else, false.</returns>
            public bool HasChildren()
            { 
                return 
                    this.children != null && 
                    this.children.Count > 0;
            }

            /// <summary>
            /// Enumerate through the node's children.
            /// </summary>
            /// <returns>An iterator through the node's children.</returns>
            public IEnumerable<Node> GetChildren()
            { 
                if(this.children == null)
                    yield break;

                foreach(Node n in this.children)
                    yield return n;
            }

            /// <summary>
            /// Enumerate the left icons, from left-to-right order.
            /// </summary>
            /// <returns>An iterator though the left icons.</returns>
            public IEnumerable<Icon> LeftIcons()
            { 
                if(this.leftIcons == null)
                    yield break;

                foreach(Icon ico in this.leftIcons)
                    yield return ico;
            }

            /// <summary>
            /// Enumerate the right icons, from left-to-right order.
            /// </summary>
            /// <returns>An iterator through the right icons.</returns>
            public IEnumerable<Icon> RightIcons()
            { 
                if(this.rightIcons == null)
                    yield break;

                foreach(Icon ico in this.rightIcons)
                    yield return ico;
            }

            /// <summary>
            /// Check if the node has any left icons.
            /// </summary>
            /// <returns>True, if the node has any left icons. Else, false.</returns>
            public bool HasLeftIcons()
            {
                return this.leftIcons != null && this.leftIcons.Count > 0;
            }

            /// <summary>
            /// Check if the node has any right icons.
            /// </summary>
            /// <returns>True, if the node has any right icons. Else, false.</returns>
            public bool HasRightIcons()
            { 
                return this.rightIcons != null && this.rightIcons.Count > 0;
            }

            /// <summary>
            /// Get the sprite of an icon.
            /// </summary>
            /// <param name="idx">The ID of the icon.</param>
            /// <returns>The sprite of the specified icon, or null if the icon could
            /// not be found.</returns>
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

            /// <summary>
            /// Destroy the node.
            /// </summary>
            /// <remarks>Also destroys children nodes.</remarks>
            /// <returns>True if successful. Else, false.</returns>
            public bool Destroy()
            { 
                if(this.ownerTree == null || this.parent == null)
                    return false;

                this.parent.RemoveChild(this);

                Tree oldOwner = this.ownerTree;
                this.ownerTree = null;
                oldOwner.NotifyRemoval(this);

                DestroyChildren();
                return true;
            }

            /// <summary>
            /// Destory all children nodes.
            /// </summary>
            /// <returns>True if successful. Else, false.</returns>
            public bool DestroyChildren()
            {
                if (this.ownerTree == null || this.parent == null)
                    return false;

                if(this.HasChildren() == true)
                { 
                    List<Node> childrenCpy = new List<Node>(this.children);
                    foreach(Node n in childrenCpy)
                        n.Destroy();
                }

                return true;
            }
        }
    }
}