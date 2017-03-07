using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    // The Node class keeps track of individual nodes within the Trie. Some information it stores includes
    // the node's char value, as wel as its chidldren nodes and whether or not it is a leaf

    class Node
    {
        public char Letter;
        public Dictionary<char, Node> children;
        public bool isLeaf;

        public Node(char c)
        {
            this.Letter = c;
            this.children = new Dictionary<char, Node>();
            this.isLeaf = false;
        }

        // Returns a node if the current node has a child node with the value of the passed character
        public Node FindLetter(char c)
        {
            if (this.children != null && this.children.ContainsKey(c))
            {
                return this.children[c];
            }
            return null;
        }
    }
}