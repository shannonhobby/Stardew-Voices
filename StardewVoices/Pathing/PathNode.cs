using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVoices.Pathing
{
    public class PathNode
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

        internal void CreateJsonFile<T>(string fileName, T model) where T : class
        {
            this.CreateChild(fileName);
            FileManipulationHelpers.CreateNewJsonFile(this.GetChild(fileName).FullPath, model);
        }

        internal void CreateFile(string fileName, string text)
        {
            this.CreateChild(fileName);
            FileManipulationHelpers.CreateNewFile(this.GetChild(fileName).FullPath, text);
        }

        internal void CreateFolder(string folderName)
        {
            this.CreateChild(folderName);
            FileManipulationHelpers.CreateNewFolder(this.GetChild(folderName).FullPath);
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

        public override string ToString() => FullPath;
    }
}
