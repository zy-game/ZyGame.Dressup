using System;
using System.Collections.Generic;

namespace ZyGame.Dressup
{
    [Serializable]
    public class NodeData
    {
        public Element basic;
        public string path;
        public string group;
        public List<NodeChild> childs;
    }

    [Serializable]
    public class NodeChild
    {
        public Element element;
        public List<string> path;
    }

    [Serializable]
    public class ElementGroupData
    {
        public string name;
        public string skelton;
    }

    public class InitConfig
    {
        public List<NodeData> nodes;
        public List<Element> normal;
        public List<ElementGroupData> groups;
    }

    /// <summary>
    /// Avatar数据
    /// </summary>
    public partial class DressupConfig
    {
        public DressupConfig()
        {
            items = new List<DressupData>();
        }

        public void Dispose()
        {
            Clear();
        }

        public void AddConfig(DressupData config)
        {
            this.items.Add(config);
        }

        public void RemoveConfig(DressupData config)
        {
            this.items.Remove(config);
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}