using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZyGame.Dressup
{
    [Serializable]
    public class ChildInfo
    {
        public Element element;
        public List<string> path;
    }

    [Serializable]
    public class ElementInfo
    {
        public Element element;
        public List<ChildInfo> childs;
    }

    [Serializable]
    public class GroupInfo
    {
        public string name;
        public List<ElementInfo> elements;

        public bool IsChild(Element element)
        {
            foreach (var VARIABLE in elements)
            {
                if (VARIABLE.childs is null || VARIABLE.childs.Count is 0)
                {
                    continue;
                }

                if (VARIABLE.childs.Find(x => x.element == element) is null)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public ElementInfo GetElement(Element element)
        {
            foreach (var VARIABLE in elements)
            {
                if (VARIABLE.element == element)
                {
                    return VARIABLE;
                }

                if (VARIABLE.childs is null || VARIABLE.childs.Count is 0)
                {
                    continue;
                }

                if (VARIABLE.childs.Find(x => x.element == element) is null)
                {
                    continue;
                }

                return VARIABLE;
            }

            return default;
        }

        public string[] GetChildPath(Element element)
        {
            foreach (var VARIABLE in elements)
            {
                if (VARIABLE.element == element)
                {
                    return Array.Empty<string>();
                }

                if (VARIABLE.childs is null || VARIABLE.childs.Count is 0)
                {
                    continue;
                }

                ChildInfo child = VARIABLE.childs.Find(x => x.element == element);
                if (child is null)
                {
                    continue;
                }

                return child.path.ToArray();
            }

            return Array.Empty<string>();
        }

        public Element GetParentElement(Element element)
        {
            foreach (var VARIABLE in elements)
            {
                if (VARIABLE.childs is null || VARIABLE.childs.Count is 0)
                {
                    continue;
                }

                ChildInfo child = VARIABLE.childs.Find(x => x.element == element);
                if (child is null)
                {
                    continue;
                }

                return VARIABLE.element;
            }

            return Element.None;
        }

        public ChildInfo[] GetChildList(Element element)
        {
            foreach (var VARIABLE in elements)
            {
                if (VARIABLE.element == element)
                {
                    return VARIABLE.childs.ToArray();
                }
            }

            return default;
        }
    }


    public class InitConfig
    {
        // public List<NodeData> nodes;
        public List<Element> normal;
        public List<GroupInfo> groups;
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