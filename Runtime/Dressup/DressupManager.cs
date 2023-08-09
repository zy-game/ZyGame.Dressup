using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

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
        /// 初始骨架
        /// </summary>
        public string skeleton;

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

        // [NonSerialized] public List<NodeData> nodeList;
        [NonSerialized] public Action OpenFileCallback;
        [NonSerialized] public IAssetLoader assetLoader;
        [NonSerialized] public Action<string, object> Notify;
        [NonSerialized] public List<GroupInfo> groupDatas;
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
        public List<Element> normalList { get; private set; }
        public List<GroupInfo> groupDatas { get; private set; }
        public IAssetLoader AssetLoader { get; private set; }
        public Action<string, object> Notify { get; private set; }
        public Dictionary<Element, DressupComponent> components { get; }
        private List<DressGroup> basicList { get; set; }
        public Action OpenFileCallback { get; private set; }
        public Action<byte[]> LoadFileCompeltion { get; set; }
        public GroupInfo groupOptions { get; private set; }
        private const int COMBINE_TEXTURE_MAX = 2048;
        private const string COMBINE_DIFFUSE_TEXTURE = "_MainTex";

        class DressGroup
        {
            public string name;
            public GameObject gameObject;
            public List<Element> elements;
        }

        public DressupManager(DressupOptions options)
        {
            this.options = options;
            this.pid = options.pid;
            this.user = options.userId;
            this.group = options.group;
            this.camera = options.camera;
            this.Notify = options.Notify;
            this.address = options.address;
            this.normalList = options.normals;
            this.groupDatas = options.groupDatas;
            this.AssetLoader = options.assetLoader;
            this.OpenFileCallback = options.OpenFileCallback;
            this.components = new Dictionary<Element, DressupComponent>();
            this.groupOptions = groupDatas.Find(x => x.name == this.group);
            this.basicList = new List<DressGroup>();
            this.AssetLoader.LoadAsync<GameObject>(options.skeleton, options.version, options.crc, LoadSkeletonCompletion);
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
                foreach (var item in components.Values)
                {
                    item.Dispose();
                }

                foreach (var VARIABLE in basicList)
                {
                    GameObject.DestroyImmediate(VARIABLE.gameObject);
                }

                components.Clear();
                basicList.Clear();
                Notify(EventNames.CLEAR_ELMENT_DATA_COMPLATED, element);
                return;
            }

            if (!components.TryGetValue(element, out DressupComponent component))
            {
                return;
            }

            DressGroup group = basicList.Find(x => x.elements.Contains(element));
            if (group != null)
            {
                group.elements.Remove(element);
                if (group.elements.Count == 0)
                {
                    GameObject.DestroyImmediate(group.gameObject);
                    basicList.Remove(group);
                }
            }

            component.Dispose();
            components.Remove(element);
            Notify(EventNames.CLEAR_ELMENT_DATA_COMPLATED, element);
        }


        public void ShowInView(Element element)
        {
            if (!components.TryGetValue(element, out DressupComponent component))
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
                    Debug.LogError(ex);
                    Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.UPLOAD_AVATAR_ICON_FAIL);
                    return;
                }

                config.group = this.group;
                config.icon = response.data.url;
                config.md5 = response.data.md5;
                foreach (var component in components.Values)
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
            }).StartCoroutine();
        }

        public DressupData GetElementData(Element element)
        {
            if (components.TryGetValue(element, out DressupComponent component))
            {
                return component.data;
            }

            return default;
        }

        public DressupComponent GetElementComponent(Element element)
        {
            if (components.TryGetValue(element, out DressupComponent component))
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
                if (dressupData.element is Element.None)
                {
                    continue;
                }

                if (groupOptions is null)
                {
                    groupOptions = groupDatas.Find(x => x.name == dressupData.model_name);
                }

                string modleName = Path.GetFileNameWithoutExtension(dressupData.model);
                DressGroup group = basicList.Find(x => x.name == modleName);
                if (group is null)
                {
                    ClearElement(dressupData.element);
                    yield return LoadAsync<GameObject>(dressupData.model, 0, 0, args =>
                    {
                        args.SetParent(gameObject, Vector3.zero, Vector3.zero, Vector3.one);
                        basicList.Add(group = new DressGroup()
                        {
                            name = modleName,
                            gameObject = args,
                            elements = new List<Element>()
                        });
                    });
                }

                if (!components.TryGetValue(dressupData.element, out DressupComponent component))
                {
                    components.Add(dressupData.element, component = new DressupComponent(this));
                }

                if (!groupOptions.IsChild(dressupData.element))
                {
                    group.elements.Add(dressupData.element);
                    component.SetChild(group.gameObject);
                }
                else
                {
                    string[] pathList = groupOptions.GetChildPath(dressupData.element);
                    List<GameObject> children = new List<GameObject>();
                    foreach (var VARIABLE in pathList)
                    {
                        Transform transform = group.gameObject.transform.Find(VARIABLE);
                        if (transform == null)
                        {
                            Debug.Log(group.name + " Not children gameobject:" + VARIABLE);
                            continue;
                        }

                        children.Add(transform.gameObject);
                    }

                    component.SetChild(children.ToArray());
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

        private IEnumerator LoadAsync<T>(string path, uint version, uint crc, Action<T> action) where T : Object
        {
            bool m = false;
            T result = null;
            AssetLoader.LoadAsync<T>(path, version, crc, args =>
            {
                result = args;
                m = true;
            });
            yield return new WaitUntil(() => m);
            action(result);
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

            foreach (var item in components.Values)
            {
                item.SetActiveState(false);
            }
        }

        public void UploadAsset(Element element)
        {
            void Runnable_OpenFileComplated(byte[] bytes)
            {
                LoadFileCompeltion -= Runnable_OpenFileComplated;

                if (!this.components.TryGetValue(element, out DressupComponent component))
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

                if (!this.components.TryGetValue(element, out DressupComponent component))
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
                foreach (var VARIABLE in components.Values)
                {
                    if (normalList.Contains(VARIABLE.data.element))
                    {
                        continue;
                    }

                    VARIABLE.SetActiveState(false);
                }

                return;
            }

            if (!this.components.TryGetValue(element, out DressupComponent component))
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
                foreach (var VARIABLE in components.Values)
                {
                    VARIABLE.SetActiveState(true);
                }

                return;
            }

            if (!this.components.TryGetValue(element, out DressupComponent component))
            {
                return;
            }

            component.SetActiveState(true);
        }

        public void Dispose()
        {
            foreach (var item in components.Values)
            {
                item.Dispose();
            }

            components.Clear();
            GameObject.DestroyImmediate(this.gameObject);
            group = string.Empty;
            options = null;
        }
    }
}