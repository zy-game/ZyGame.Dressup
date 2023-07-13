using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static ZyGame.Replacement.Ex;
using Object = UnityEngine.Object;

namespace ZyGame.Replacement
{
    public class DressupOptions
    {
        /// <summary>
        /// �ӿڵ�ַ
        /// </summary>
        public string address;
        /// <summary>
        /// ��������
        /// </summary>
        public string skeleton;
        /// <summary>
        /// ���������汾��
        /// </summary>
        public uint version;
        /// <summary>
        /// ��������Ψһ��
        /// </summary>
        public uint crc;
        /// <summary>
        /// �ҵ�����
        /// </summary>
        public string config;
        /// <summary>
        /// �û���avatar�е�UID
        /// </summary>
        public string userId;
        /// <summary>
        /// ģ��Ʒ��
        /// </summary>
        public string quality;
        /// <summary>
        /// ��ĿID
        /// </summary>
        public int pid;

        [NonSerialized] public Camera camera;
        [NonSerialized] public IAssetLoader assetLoader;
        [NonSerialized] public List<NodeData> nodeList;
        [NonSerialized] public Action<string, object> Notify;
        [NonSerialized] public Action OpenFileCallback;
        [NonSerialized] public Vector3 cameraPosition;

    }
    public class Dressup : IDisposable
    {
        public int pid { get; private set; }

        public string user { get; private set; }
        public string address { get; private set; }
        public Camera camera { get; private set; }
        public string skeleton { get; private set; }
        public GameObject gameObject { get; private set; }
        public DressupOptions options { get; private set; }
        public List<NodeData> nodeList { get; private set; }
        public IAssetLoader AssetLoader { get; private set; }
        public Action<string, object> Notify { get; private set; }
        public Dictionary<Element, DressupComponent> dressups { get; }

        public Action OpenFileCallback { get; private set; }
        public Action<byte[]> LoadFileCompeltion { get; private set; }

        public Dressup(DressupOptions options)
        {
            this.options = options;
            this.pid = options.pid;
            this.user = options.userId;
            this.camera = options.camera;
            this.Notify = options.Notify;
            this.nodeList = options.nodeList;
            this.AssetLoader = options.assetLoader;
            this.OpenFileCallback = options.OpenFileCallback;
            this.dressups = new Dictionary<Element, DressupComponent>();
            this.skeleton = Path.GetFileNameWithoutExtension(skeleton);
            this.AssetLoader.LoadAsync<GameObject>(options.skeleton, options.version, options.crc, LoadSkeletonCompletion);
        }

        private void LoadSkeletonCompletion(GameObject result)
        {
            gameObject = result;
            gameObject.SetParent(null, Vector3.zero, Vector3.zero, Vector3.one);
            this.camera.transform.position = options.cameraPosition;
        }
        /// <summary>
        /// ������
        /// </summary>
        /// <param name="element"></param>
        public void ClearElement(Element element)
        {
            if (element is Element.None)
            {
                foreach (var item in dressups.Values)
                {
                    item.Dispose();
                }
                dressups.Clear();
                Notify(EventNames.CLEAR_ELMENT_DATA_COMPLATED, element);
                return;
            }
            if (!dressups.TryGetValue(element, out DressupComponent component))
            {
                return;
            }
            component.Dispose();
            Notify(EventNames.CLEAR_ELMENT_DATA_COMPLATED, element);
        }

        /// <summary>
        /// ����λ��ʾ����ͼ����
        /// </summary>
        public void ShowInView(Element element)
        {
            if (!dressups.TryGetValue(element, out DressupComponent component))
            {
                camera.ToViewCenter(gameObject);
                return;
            }
            camera.ToViewCenter(component.gameObject);
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="configName">�����������</param>
        public void ExportConfig(string configName)
        {
            DressupConfig config = new DressupConfig();
            config.name = configName;

            Texture2D icon = Camera.main.Screenshot(512, 512, this.gameObject);
            byte[] bytes = icon.EncodeToPNG();
            Ex.RequestCreateFileData fileData = new Ex.RequestCreateFileData(config.name + "_icon.png", bytes.GetMd5(), "image/png", "2", bytes.Length);
            Ex.UploadAsset(address, user, pid, fileData, bytes, (response, ex) =>
            {
                if (ex != null)
                {
                    Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.UPLOAD_AVATAR_ICON_FAIL);
                    return;
                }
                config.group = this.skeleton;
                config.icon = response.data.url;
                config.md5 = response.data.md5;
                foreach (var component in dressups.Values)
                {
                    if (component.data is null)
                    {
                        Debug.Log("empty element data");
                        continue;
                    }
                    config.AddConfig(component.data);
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
                Notify(EventNames.EXPORT_AVATAR_CONFIG_COMPLATED, json);
            });
        }

        /// <summary>
        /// ��ȡ��������
        /// </summary>
        /// <param name="element">��λö��</param>
        public DressupData GetElementData(Element element)
        {
            if (dressups.TryGetValue(element, out DressupComponent component))
            {
                return component.data;
            }
            return default;
        }

        public DressupComponent GetElementComponent(Element element)
        {
            if (dressups.TryGetValue(element, out DressupComponent component))
            {
                return component;
            }
            return default;
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="config">��������</param>
        public void ImportConfig(string config)
        {
            if (config.IsNullOrEmpty())
            {
                Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.CONFIG_DATA_IS_NULL_OR_EMPTY);
                return;
            }
            try
            {
                DressupConfig tempConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<DressupConfig>(config);
                if (tempConfig.group.IsNullOrEmpty() || tempConfig.group != skeleton)
                {
                    Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.ELEMENT_GROUP_NOT_THE_SAME);
                    return;
                }
                ClearElement(Element.None);
                SetElementData(tempConfig.items).StartCoroutine(Notify, EventNames.IMPORT_CONFIG_COMPLATED, default(object));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Notify(EventNames.ERROR_MESSAGE_NOTICE, string.Format(ErrorInfo.IMPORT_AVATAR_CONFIG_FAIL, e.ToString()));
            }
        }

        /// <summary>
        /// ���ò���ģ��
        /// </summary>
        /// <param name="elementData">��λ����</param>
        public IEnumerator SetElementData(List<DressupData> elements)
        {
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                DressupData dressupData = elements[i];
                if (!dressups.TryGetValue(dressupData.element, out DressupComponent component))
                {
                    dressups.Add(dressupData.element, component = new DressupComponent(this));
                }

                if (IsChild(dressupData.element))
                {
                    Element element = GetParentElement(dressupData.element);
                    if (!dressups.TryGetValue(element, out DressupComponent parent))
                    {
                        continue;
                    }

                    string[] childPath = GetChildPath(dressupData.element);
                    List<GameObject> child = new List<GameObject>();
                    foreach (var item in childPath)
                    {
                        child.Add(parent.gameObject.transform.Find(item).gameObject);
                    }
                    component.SetGameObjectAsChild(child.ToArray());
                    component.DressupTexture(dressupData);
                }
                else
                {
                    component.Dressup(dressupData).StartCoroutine(() => elements.Remove(dressupData));
                }
            }
            yield return new WaitUntil(() => elements.Count == 0);
            Notify(EventNames.SET_ELEMENT_DATA_COMPLATED, default(object));
        }



        /// <summary>
        /// ���ò���ģ��
        /// </summary>
        /// <param name="elementData">��λ����</param>
        public IEnumerator SetElementData(DressupData element)
        {
            if (!dressups.TryGetValue(element.element, out DressupComponent component))
            {
                dressups.Add(element.element, component = new DressupComponent(this));
            }
            yield return component.Dressup(element);
            Notify(EventNames.SET_ELEMENT_DATA_COMPLATED, default(object));
        }

        /// <summary>
        /// �ϲ�Avatar
        /// </summary>
        public void Combine()
        {

        }

        /// <summary>
        /// �ϴ�������Դ
        /// </summary>
        /// <param name="element">����λ��</param>
        /// <param name="fileDataString">�ļ�����</param>
        public void UploadAsset(Element element)
        {
            void Runnable_OpenFileComplated(byte[] bytes)
            {
                LoadFileCompeltion = null;

                if (!this.dressups.TryGetValue(element, out DressupComponent component))
                {
                    return;
                }

                Texture2D texture = new Texture2D(512, 512);
                texture.LoadImage(bytes);
                component.SetTexture2D(texture, component.data);
                Ex.UploadElementData(address, user, pid, Guid.NewGuid().ToString(), string.Empty, bytes, component.gameObject, component.data, Ex.PublishState.Process, args =>
                {
                    if (args == null)
                    {
                        Notify(EventNames.UPLOAD_ELEMENT_ASSET_COMPLATED, string.Empty);
                        return;
                    }
                    Notify(EventNames.UPLOAD_ELEMENT_ASSET_COMPLATED, Newtonsoft.Json.JsonConvert.SerializeObject(args));
                });
            }
            LoadFileCompeltion = Runnable_OpenFileComplated;
            OpenFileCallback();
        }

        /// <summary>
        /// Ԥ��
        /// </summary>
        /// <param name="element">��λö��</param>
        public void PreviewAsset(Element element)
        {
            void Runnable_OpenFileComplated(byte[] bytes)
            {
                LoadFileCompeltion = null;
                if (bytes == null || bytes.Length == 0)
                {
                    return;
                }

                if (!this.dressups.TryGetValue(element, out DressupComponent component))
                {
                    return;
                }

                Texture2D texture = new Texture2D(512, 512);
                texture.LoadImage(bytes);
                component.SetTexture2D(texture, component.data);
                ShowInView(element);
            }
            LoadFileCompeltion = Runnable_OpenFileComplated;
            OpenFileCallback();
        }

        /// <summary>
        /// ���ز�λ
        /// </summary>
        /// <param name="element"></param>
        public void DisableElement(Element element)
        {
            if (element == Element.None)
            {
                foreach (var VARIABLE in dressups.Values)
                {
                    VARIABLE.SetActiveState(false);
                }

                return;
            }

            if (!this.dressups.TryGetValue(element, out DressupComponent component))
            {
                return;
            }

            component.SetActiveState(false);
        }

        /// <summary>
        /// ��ʾ��λ
        /// </summary>
        /// <param name="element"></param>
        public void EnableElement(Element element)
        {
            if (element == Element.None)
            {
                foreach (var VARIABLE in dressups.Values)
                {
                    VARIABLE.SetActiveState(true);
                }

                return;
            }

            if (!this.dressups.TryGetValue(element, out DressupComponent component))
            {
                return;
            }

            component.SetActiveState(true);
        }

        public void Dispose()
        {
            foreach (var item in dressups.Values)
            {
                item.Dispose();
            }
            dressups.Clear();
            GameObject.DestroyImmediate(this.gameObject);
            skeleton = string.Empty;
            options = null;
        }

        public bool IsChild(Element element)
        {
            if (nodeList is null || nodeList.Count is 0)
            {
                return false;
            }
            foreach (var item in nodeList)
            {
                NodeChild nodeChild = item.childs.Find(x => x.element == element);
                if (nodeChild is null)
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        public string[] GetChildPath(Element element)
        {
            if (nodeList is null || nodeList.Count is 0)
            {
                return Array.Empty<string>();
            }
            foreach (var item in nodeList)
            {
                NodeChild nodeChild = item.childs.Find(x => x.element == element);
                if (nodeChild is null)
                {
                    continue;
                }
                return nodeChild.path.ToArray();
            }
            return Array.Empty<string>();
        }

        public Element GetParentElement(Element element)
        {
            if (nodeList is null || nodeList.Count is 0)
            {
                return Element.None;
            }
            foreach (var item in nodeList)
            {
                NodeChild nodeChild = item.childs.Find(x => x.element == element);
                if (nodeChild is null)
                {
                    continue;
                }
                return item.element;
            }
            return Element.None;
        }
    }
}