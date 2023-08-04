using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZyGame.Dressup
{
    public class DressupOptions
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string address;

        /// <summary>
        /// 资源组
        /// </summary>
        public string group;

        /// <summary>
        /// 骨架版本
        /// </summary>
        public uint version;

        /// <summary>
        /// 骨架crc
        /// </summary>
        public uint crc;

        /// <summary>
        /// UID
        /// </summary>
        public string userId;

        /// <summary>
        /// 模型质量
        /// </summary>
        public string quality;

        /// <summary>
        /// 项目ID
        /// </summary>
        public int pid;


        [NonSerialized] public Camera camera;
        [NonSerialized] public List<Element> normals;
        [NonSerialized] public Vector3 cameraPosition;
        [NonSerialized] public List<NodeData> nodeList;
        [NonSerialized] public Action OpenFileCallback;
        [NonSerialized] public IAssetLoader assetLoader;
        [NonSerialized] public Action<string, object> Notify;
        [NonSerialized] public List<ElementGroupData> groupDatas;
    }

    public class DressupManager : IDisposable
    {
        public int pid { get; private set; }
        public string user { get; private set; }
        public string group { get; private set; }
        public string address { get; private set; }
        public Camera camera { get; private set; }
        public GameObject combine { get; private set; }
        public GameObject boneRoot { get; private set; }
        public GameObject skinRoot { get; private set; }
        public GameObject gameObject { get; private set; }
        public DressupOptions options { get; private set; }
        public List<NodeData> nodeList { get; private set; }
        public List<Element> normalList { get; private set; }
        public List<ElementGroupData> groupDatas { get; private set; }
        public IAssetLoader AssetLoader { get; private set; }
        public Action<string, object> Notify { get; private set; }
        public Dictionary<Element, DressupComponent> dressups { get; }
        public Action OpenFileCallback { get; private set; }
        public Action<byte[]> LoadFileCompeltion { get; set; }

        private const int COMBINE_TEXTURE_MAX = 2048;
        private const string COMBINE_DIFFUSE_TEXTURE = "_MainTex";

        public DressupManager(DressupOptions options)
        {
            this.options = options;
            this.pid = options.pid;
            this.user = options.userId;
            this.group = options.group;
            this.camera = options.camera;
            this.Notify = options.Notify;
            this.nodeList = options.nodeList;
            this.normalList = options.normals;
            this.groupDatas = options.groupDatas;
            this.AssetLoader = options.assetLoader;
            this.OpenFileCallback = options.OpenFileCallback;
            this.dressups = new Dictionary<Element, DressupComponent>();
            ElementGroupData groupData = groupDatas.Find(x => x.name == options.group);
            if (groupData is null)
            {
                Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.INITIALIZE_AVATAR_ERROR_NOT_FIND_THE_SKELETON);
                return;
            }

            this.AssetLoader.LoadAsync<GameObject>(groupData.skelton, options.version, options.crc, LoadSkeletonCompletion);
        }

        public bool IsChild(Element element)
        {
            if (nodeList is null || nodeList.Count is 0)
            {
                return false;
            }

            foreach (var item in nodeList)
            {
                if (item.group != group)
                {
                    continue;
                }

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
                if (item.group != group)
                {
                    continue;
                }

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
                if (item.group != group)
                {
                    continue;
                }

                NodeChild nodeChild = item.childs.Find(x => x.element == element);
                if (nodeChild is null)
                {
                    continue;
                }

                return item.basic;
            }

            return Element.None;
        }

        public NodeChild[] GetChildList(Element element)
        {
            if (nodeList is null || nodeList.Count is 0)
            {
                return Array.Empty<NodeChild>();
            }

            NodeData nodeData = nodeList.Find(x => x.basic == element && x.group == group);
            if (nodeData is null)
            {
                return default;
            }

            return nodeData.childs.ToArray();
        }

        private void LoadSkeletonCompletion(GameObject result)
        {
            if (result is null)
            {
                Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.INITIALIZE_AVATAR_ERROR_NOT_FIND_THE_SKELETON);
            }

            gameObject = result;
            gameObject.SetParent(null, Vector3.zero, Vector3.zero, Vector3.one);
            boneRoot = gameObject.transform.Find("joint/root")?.gameObject;
            skinRoot = gameObject.transform.Find("mesh")?.gameObject;
            if (this.camera is not null)
            {
                this.camera.transform.position = options.cameraPosition;
            }

            Notify(EventNames.INITIALIZED_COMPLATED_EVENT, string.Empty);
        }


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

            NodeChild[] children = GetChildList(element);
            if (children is not null && children.Length is not 0)
            {
                foreach (var item in children)
                {
                    if (dressups.TryGetValue(item.element, out DressupComponent childComponent))
                    {
                        childComponent.Dispose();
                        dressups.Remove(item.element);
                    }
                }
            }

            component.Dispose();
            dressups.Remove(element);
            Notify(EventNames.CLEAR_ELMENT_DATA_COMPLATED, element);
        }


        public void ShowInView(Element element)
        {
            if (!dressups.TryGetValue(element, out DressupComponent component))
            {
                camera?.ToViewCenter(gameObject);
                return;
            }

            if (component.gameObject == null)
            {
                camera?.ToViewCenter(component.childs[0]);
            }
            else
            {
                camera?.ToViewCenter(component.gameObject);
            }
        }

        public void ExportConfig(string configName)
        {
            DressupConfig config = new DressupConfig();
            config.name = configName;

            Texture2D icon = Camera.main.Screenshot(512, 512, this.gameObject);
            byte[] bytes = icon.EncodeToPNG();
            API.RequestCreateFileData fileData = new API.RequestCreateFileData(config.name + "_icon.png", bytes.GetMd5(), "image/png", "2", bytes.Length);
            API.UploadAsset(address, user, pid, fileData, bytes, (response, ex) =>
            {
                if (ex != null)
                {
                    Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.UPLOAD_AVATAR_ICON_FAIL);
                    return;
                }

                config.group = this.group;
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
                if (tempConfig.group.IsNullOrEmpty() || tempConfig.group != group)
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


        public IEnumerator SetElementData(List<DressupData> elements)
        {
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                DressupData dressupData = elements[i];
                if (!dressups.TryGetValue(dressupData.element, out DressupComponent component))
                {
                    if (!IsChild(dressupData.element))
                    {
                        dressups.Add(dressupData.element, component = new DressupComponent(this));
                    }
                    else
                    {
                        Element parentElement = GetParentElement(dressupData.element);
                        DressupComponent parent = GetElementComponent(parentElement);
                        if (parent is null)
                        {
                            Debug.Log("Not set parent element:" + parentElement);
                            continue;
                        }

                        string[] pathList = GetChildPath(dressupData.element);
                        List<GameObject> children = new List<GameObject>();
                        foreach (var VARIABLE in pathList)
                        {
                            Transform transform = parent.gameObject.transform.Find(VARIABLE);
                            if (transform == null)
                            {
                                Debug.Log("Not children gameobject" + VARIABLE);
                                continue;
                            }

                            children.Add(transform.gameObject);
                        }

                        dressups.Add(dressupData.element, component = new DressupComponent(this, children.ToArray()));
                    }
                }

                if (IsChild(dressupData.element) is false)
                {
                    if (component.data is null || component.data.model.Equals(dressupData.model) is false)
                    {
                        yield return component.DressupGameObject(dressupData);
                    }
                }

                if (component is null)
                {
                    continue;
                }

                if (component.data is null || component.data.texture.Equals(dressupData.texture) is false)
                {
                    yield return component.DressupTexture(dressupData);
                }

                component.data = dressupData;
            }

            ShowInView(Element.None);
            Notify(EventNames.SET_ELEMENT_DATA_COMPLATED, default(object));
        }


        public void Combine()
        {
            float startTime = Time.realtimeSinceStartup;
            if (combine is not null)
            {
                GameObject.DestroyImmediate(combine);
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            List<Transform> transforms = new List<Transform>();
            transforms.AddRange(boneRoot.GetComponentsInChildren<Transform>(true));
            List<Material> materials = new List<Material>(); //the list of materials
            List<CombineInstance> combineInstances = new List<CombineInstance>(); //the list of meshes
            List<Transform> bones = new List<Transform>(); //the list of bones
            List<Vector2[]> oldUV = null;
            Material newMaterial = null;
            Texture2D newDiffuseTex = null;
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                SkinnedMeshRenderer smr = skinnedMeshRenderers[i];
                if (newMaterial is null)
                {
                    newMaterial = new Material(smr.sharedMaterial.shader);
                    newMaterial.name = "Combine Mesh Material";
                }

                materials.AddRange(smr.materials); // Collect materials
                // Collect meshes
                for (int sub = 0; sub < smr.sharedMesh.subMeshCount; sub++)
                {
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = smr.sharedMesh;
                    ci.subMeshIndex = sub;
                    combineInstances.Add(ci);
                }

                // Collect bones
                for (int j = 0; j < smr.bones.Length; j++)
                {
                    int tBase = 0;
                    for (tBase = 0; tBase < transforms.Count; tBase++)
                    {
                        if (smr.bones[j].name.Equals(transforms[tBase].name))
                        {
                            bones.Add(transforms[tBase]);
                            break;
                        }
                    }
                }
            }

            // merge materials
            oldUV = new List<Vector2[]>();
            List<Texture2D> Textures = new List<Texture2D>();
            for (int i = 0; i < materials.Count; i++)
            {
                Textures.Add(materials[i].GetTexture(COMBINE_DIFFUSE_TEXTURE) as Texture2D);
            }

            newDiffuseTex = new Texture2D(COMBINE_TEXTURE_MAX, COMBINE_TEXTURE_MAX, TextureFormat.RGBA32, true);
            Rect[] uvs = newDiffuseTex.PackTextures(Textures.ToArray(), 0);
            newMaterial.mainTexture = newDiffuseTex;
            Vector2[] uva, uvb;
            for (int j = 0; j < combineInstances.Count; j++)
            {
                uva = (Vector2[])(combineInstances[j].mesh.uv);
                uvb = new Vector2[uva.Length];
                for (int k = 0; k < uva.Length; k++)
                {
                    uvb[k] = new Vector2((uva[k].x * uvs[j].width) + uvs[j].x, (uva[k].y * uvs[j].height) + uvs[j].y);
                }

                oldUV.Add(combineInstances[j].mesh.uv);
                combineInstances[j].mesh.uv = uvb;
            }

            combine = new GameObject("Combine Mesh").SetParent(skinRoot, Vector3.zero, Vector3.zero, Vector3.one);
            SkinnedMeshRenderer r = combine.AddComponent<SkinnedMeshRenderer>();
            r.sharedMesh = new Mesh();
            r.sharedMesh.name = "Combine Mesh Renderer";
            r.sharedMesh.CombineMeshes(combineInstances.ToArray(), true, false); // Combine meshes
            r.bones = bones.ToArray(); // Use new bones
            r.material = newMaterial;
            r.rootBone = boneRoot.transform;
            for (int i = 0; i < combineInstances.Count; i++)
            {
                combineInstances[i].mesh.uv = oldUV[i];
            }

            foreach (var item in dressups.Values)
            {
                item.SetActiveState(false);
            }
        }

        public void UploadAsset(Element element)
        {
            void Runnable_OpenFileComplated(byte[] bytes)
            {
                LoadFileCompeltion -= Runnable_OpenFileComplated;

                if (!this.dressups.TryGetValue(element, out DressupComponent component))
                {
                    return;
                }

                Texture2D texture = new Texture2D(512, 512);
                texture.LoadImage(bytes);
                component.SetTexture2D(texture);
                API.UploadElementData(address, user, pid, Guid.NewGuid().ToString(), string.Empty, bytes, component.gameObject, component.data, API.PublishState.Process, args =>
                {
                    if (args == null)
                    {
                        Notify(EventNames.UPLOAD_ELEMENT_ASSET_COMPLATED, string.Empty);
                        return;
                    }

                    Notify(EventNames.UPLOAD_ELEMENT_ASSET_COMPLATED, Newtonsoft.Json.JsonConvert.SerializeObject(args));
                });
            }

            LoadFileCompeltion += Runnable_OpenFileComplated;
            OpenFileCallback();
        }


        public void PreviewAsset(Element element)
        {
            void Runnable_OpenFileComplated(byte[] bytes)
            {
                LoadFileCompeltion -= Runnable_OpenFileComplated;
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
                component.SetTexture2D(texture);
                ShowInView(element);
            }

            LoadFileCompeltion += Runnable_OpenFileComplated;
            OpenFileCallback();
        }

        public void DisableElement(Element element)
        {
            if (element == Element.None)
            {
                foreach (var VARIABLE in dressups.Values)
                {
                    if (normalList.Contains(VARIABLE.data.element))
                    {
                        continue;
                    }

                    VARIABLE.SetActiveState(false);
                }

                return;
            }

            if (!this.dressups.TryGetValue(element, out DressupComponent component))
            {
                return;
            }

            if (normalList.Contains(element))
            {
                return;
            }

            component.SetActiveState(false);
        }

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
            group = string.Empty;
            options = null;
        }
    }
}