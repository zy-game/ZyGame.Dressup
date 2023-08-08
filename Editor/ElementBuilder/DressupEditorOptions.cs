using System;
using System.Collections.Generic;
using UnityEngine;
using ZyGame.Dressup;
using Object = UnityEngine.Object;

namespace ZyGame.Editor.Avatar
{
    [PathOptions("ProjectSettings/DressupOptions.asset", PathOptions.Localtion.Project)]
    public class DressupEditorOptions : Config<DressupEditorOptions>
    {
        [Header("默认部件枚举")] public List<Element> normals;
        [Header("部件组设置")] public List<GroupOptions> options;
        public List<DressupOptionsData> dressups;

        public void RemoveDressup(DressupOptionsData data)
        {
            dressups.Remove(data);
            DressupEditorOptions.Save();
        }

        public void AddDressup(DressupOptionsData data)
        {
            dressups.Add(data);
            DressupEditorOptions.Save();
        }
    }

    [Serializable]
    public class ChildOptions
    {
        [Header("部件枚举")] public Element element;
        [Header("部件路径")] public List<string> path;
    }

    [Serializable]
    public class ElementOptions
    {
        [Header("部件枚举")] public Element element;
        [Header("部件模型")] public Object target;
        [Header("部件图标")] public Texture2D icon;
        [Header("子部件列表")] public List<ChildOptions> childs;
    }

    [Serializable]
    public class GroupOptions
    {
        [Header("资源组名")] public string name;
        [Header("基础骨架")] public Object skeleton;
        [Header("部件配置列表")] public List<ElementOptions> elements;

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

        public ElementOptions GetElement(Element element)
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
    }

    [Serializable]
    public class DressupOptionsData
    {
        [NonSerialized] public bool isOn;
        [NonSerialized] public bool foldout;
        public string group;
        public Texture2D icon;
        public Element element;
        public Texture2D texture;
    }

    class OutDressupData
    {
        public int element;
        public bool is_normal;
        public string icon;
        public string texture;
        public string group;
        public string model;
        public uint version;
        public uint crc;
    }
}