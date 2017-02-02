using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;

namespace WebRole1
{
    // Class used to implement a Trie tree to help quickly add and search though material.
    public class Trie
    {
        private Node root;
        private List<string> titlesList = new List<string>();

        public Trie()
        {
            root = new Node('^');
        }

        // Returns the current List of titles added
        public List<string> getTitles()
        {
            return this.titlesList;
        }

        // Adds a title to the Trie
        public void AddTitle(string title)
        {
            char[] chars = title.ToLower().ToCharArray();
            AddTitle(this.root, chars, 0);
        }

        // Helper method that recursively inserts letter by letter of the passed array of characters
        private void AddTitle(Node current, char[] letters, int index)
        {
            int stringLength = letters.Length;
            if (index < stringLength)
            {
                char letter = letters[index];
                // Checks to see if the current node is a leaf or if its children contain the letter
                if (current.children.Count == 0 || current.children.ContainsKey(letter) == false)
                {
                    current.children[letter] = new Node(letter);
                }
                current = current.children[letter];
                AddTitle(current, letters, index + 1);
            } else // End of the word
            {
                current.isLeaf = true;
            }
        }

        //Searches the trie for a passed string
        public void SearchForPrefix(string s)
        {
            titlesList.Clear();
            char[] chars = s.ToLower().ToCharArray();
            StringBuilder sb = new StringBuilder();
            SearchForPrefix(root, chars, 0, sb);
        }

        // Helper method to search the trie one character at a time
        private void SearchForPrefix(Node current, char[] letters, int index, StringBuilder currentResult)
        {
            int stringLength = letters.Length;
            // Checks to see if there are still letters in the array to go through and if the children of the current node
            // contains the letter at the current index
            if (index < stringLength && current.children.ContainsKey(letters[index]))
            {
                char prefix = letters[index];
                currentResult.Append(prefix);
                current = current.FindLetter(prefix);
                SearchForPrefix(current, letters, index + 1, currentResult);
            }
            // If there are still letters in the array to process but the current children do not contain the letter at the current index
            else if (index < stringLength && !current.children.ContainsKey(letters[index]))
            {
                ;
            } else
            {
                if (current.isLeaf) //if it is a word
                {
                    titlesList.Add(currentResult.ToString());
                }
                if (current.Letter.ToString() != "^")
                {
                    // Calls for a depth first search method
                    DFS(current, currentResult);
                }
            }
        }

        // Secondary helper method to travers the trie depth first to become more efficient
        private void DFS(Node current, StringBuilder currentResult)
        {
            foreach (char c in current.children.Keys)
            {
                //limits the list of suggestions found to 10
                if (this.titlesList.Count < 10)
                {
                    currentResult.Append(c);
                    if (current.FindLetter(c).isLeaf)
                    {
                        this.titlesList.Add(currentResult.ToString());
                    }
                    DFS(current.FindLetter(c), currentResult);
                    currentResult.Remove(currentResult.Length - 1, 1);
                }
            }
        }
    }
}