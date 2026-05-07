using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVoices.Pathing
{
    internal class PathNode
    {
        internal string FullPath { get; }
        internal string Name { get; }

        private List<PathNode> ChildNodes { get; }


        internal PathNode(string name, string basePath)
        {
            FullPath = basePath;
            Name = name;
            ChildNodes = new List<PathNode>();
        }

        internal PathNode CreateChild(string folderName)
        {

            PathNode newChild = new PathNode(folderName, Path.Combine(FullPath, folderName));
            ChildNodes.Add(newChild);
            return newChild;
        }

        private PathNode GetChildHelper(string name)
        {
            try
            {
                return ChildNodes.Find(x => x.Name == name);
            }
            catch
            {
                return null;
            }
        }

        internal Boolean HasChildren() { return ChildNodes.Count > 0;}

        internal PathNode GetChild(string name)
        {
            PathNode child = GetChildHelper(name);
            if (child != null)
            {
                return child;
            }

            if (this.HasChildren())
            {
                foreach(PathNode node in this.ChildNodes) {
                    PathNode foundChild = node.GetChild(name);
                    if (foundChild != null)
                    {
                        return foundChild;
                    }
                }
            }
            
            return null;
        }


        internal void UpdatePathAndChildren(string newBasePath)
        {
            // TODO
        }

        public override string ToString() => FullPath;
    }
}
