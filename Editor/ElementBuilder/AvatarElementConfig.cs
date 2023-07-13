using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZyGame.Dressup;
using Object = UnityEngine.Object;

namespace ZyGame.Editor.Avatar
{
    [PathOptions("ProjectSettings/AvatarElementConfig.asset", PathOptions.Localtion.Project)]
    public class AvatarElementConfig : Config<AvatarElementConfig>
    {
        public Object iconOutput;
        public List<NodeData> nodes;
        public List<Element> normals;
        public List<EditorGroupData> groups;
        public List<ElementItemData> elements;

        public NodeChild[] GetChildren(Element element)
        {
            if (nodes is null || nodes.Count is 0)
            {
                return Array.Empty<NodeChild>();
            }
            NodeData nodeData = nodes.Find(x => x.basic == element);
            if (nodeData is null)
            {
                return Array.Empty<NodeChild>();
            }
            return nodeData.childs.ToArray();
        }

        public string GetNodePath(Element element)
        {
            if (nodes is null || nodes.Count is 0)
            {
                return string.Empty;
            }
            NodeData nodeData = nodes.Find(x => x.basic == element);
            if (nodeData is null)
            {
                return string.Empty;
            }
            return nodeData.path;
        }
    }

    [Serializable]
    public class EditorGroupData
    {
        public string name;
        public GameObject skelton;
        public Texture2D texture;
    }

    class OutData
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

    [Serializable]
    public class ElementItemData
    {
        public string group;
        public bool isNormal;
        public GameObject fbx;
        public Element element;
        public uint version;
        public Texture2D icon;
        public Texture2D texture;
        public List<ElementItemData> childs;

        [NonSerialized] public bool foldout;

        public ElementItemData()
        {

        }

        internal OutData GetOutData(string outpath)
        {
            return new OutData
            {
                element = (int)element,
                is_normal = isNormal,
            };
        }

        public DressupData GetDressupData()
        {
            DressupData dressupData = new DressupData();
            dressupData.element = element;
            dressupData.crc = 0;
            dressupData.version = version;
            dressupData.model_name = group;
            dressupData.publish_status = (int)Ex.PublishState.Publish;
            return dressupData;
        }
    }

    public sealed class PathOptions : Attribute
    {
        [Flags]
        public enum Localtion
        {
            Project = 1 << 1,
            Packaged = 1 << 2,
            Internal = 1 << 3,
            File = 1 << 4,
        }

        public string filepath { get; }
        public Localtion localtion { get; }

        public PathOptions(string path, Localtion localtion)
        {
            this.filepath = path;
            this.localtion = localtion;
        }
    }
    public class Config<T> : ScriptableObject where T : ScriptableObject
    {
        private Action _disposeable;
        private static T _instance;
        public static T instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        public void Dispose()
        {
            _disposeable?.Invoke();
            _instance = null;
            GC.SuppressFinalize(this);
        }

        public static T Load()
        {
            PathOptions options = typeof(T).GetCustomAttribute<PathOptions>();
            if (options is null)
            {
                throw new NullReferenceException(nameof(PathOptions));
            }
            string fileName = Path.GetFileName(options.filepath);
            T instance = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(options.filepath).FirstOrDefault() as T;
            if (instance is null)
            {
                _instance = instance = Activator.CreateInstance<T>();
                Save();
            }
            return _instance = instance;
        }

        public static void Save()
        {
            PathOptions options = typeof(T).GetCustomAttribute<PathOptions>();
            if (options is null)
            {
                throw new NullReferenceException(nameof(PathOptions));
            }
            if (_instance is null)
            {
                return;
            }
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[1] { _instance }, options.filepath, true);
        }
    }
}
