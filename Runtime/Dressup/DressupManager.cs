using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using ZyGame.Drawing;
using Object = UnityEngine.Object;

namespace ZyGame.Dressup
{
    public interface IEventNotify
    {
        void Notify(string evtName, object evtData = null);
        void Register(string evtName, Action<object> action, bool isOnce = true);
        void Unregister(string evtName, Action<object> action);
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

        public IEventNotify EventNotify { get; private set; }
        // public Action<string, object> Notify { get; private set; }

        // public Dictionary<Element, DressupComponent> components { get; }
        private List<DressGroup> basicList { get; set; }
        public Action OpenFileCallback { get; private set; }
        public GroupInfo groupOptions { get; private set; }

        private Dictionary<Element, DressupData> dataList = new Dictionary<Element, DressupData>();
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
            this.address = options.address;
            this.EventNotify = options.Notify;
            this.normalList = options.normals;
            this.groupDatas = options.groupDatas;
            this.AssetLoader = options.assetLoader;
            this.OpenFileCallback = options.OpenFileCallback;
            // this.components = new Dictionary<Element, DressupComponent>();
            this.groupOptions = groupDatas.Find(x => x.name == this.group);
            this.basicList = new List<DressGroup>();
            this.AssetLoader.LoadAsync<GameObject>(options.skeleton, options.version, options.crc, LoadSkeletonCompletion);
        }

        private void LoadSkeletonCompletion(GameObject result)
        {
            if (result is null)
            {
                EventNotify.Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.INITIALIZE_AVATAR_ERROR_NOT_FIND_THE_SKELETON);
            }

            gameObject = result;
            gameObject.SetParent(null, Vector3.zero, Vector3.zero, Vector3.one);
            boneRoot = gameObject.transform.Find("joint/root")?.gameObject;
            skinRoot = gameObject.transform.Find("mesh")?.gameObject;
            if (this.camera is not null)
            {
                this.camera.transform.position = options.cameraPosition;
            }

            EventNotify.Notify(EventNames.INITIALIZED_COMPLATED_EVENT, string.Empty);
        }

        public bool HaveDressup(Element element)
        {
            return dataList.ContainsKey(element);
        }

        public GameObject GetDressupGameObject(Element element)
        {
            GameObject result = default;
            DressGroup dressGroup = basicList.Find(x => x.elements.Contains(element));
            if (dressGroup is null)
            {
                return result;
            }

            if (!groupOptions.IsChild(element))
            {
                result = dressGroup.gameObject;
            }
            else
            {
                string[] pathList = groupOptions.GetChildPath(element);
                List<GameObject> children = new List<GameObject>();
                foreach (var VARIABLE in pathList)
                {
                    Transform transform = dressGroup.gameObject.transform.Find(VARIABLE);
                    if (transform == null)
                    {
                        Debug.Log(dressGroup.name + " Not children gameobject:" + VARIABLE);
                        continue;
                    }

                    children.Add(transform.gameObject);
                }

                result = children.FirstOrDefault();
            }

            return result;
        }

        public Texture2D GetTexture2D(Element element)
        {
            Texture2D result = default;
            DressGroup dressGroup = basicList.Find(x => x.elements.Contains(element));
            if (dressGroup is null)
            {
                return result;
            }

            if (!groupOptions.IsChild(element))
            {
                SkinnedMeshRenderer skinnedMeshRenderer = dressGroup.gameObject.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (skinnedMeshRenderer is null)
                {
                    return result;
                }

                result = (Texture2D)skinnedMeshRenderer.sharedMaterial.mainTexture;
            }
            else
            {
                string[] pathList = groupOptions.GetChildPath(element);
                List<GameObject> children = new List<GameObject>();
                foreach (var VARIABLE in pathList)
                {
                    Transform transform = dressGroup.gameObject.transform.Find(VARIABLE);
                    if (transform == null)
                    {
                        Debug.Log(dressGroup.name + " Not children gameobject:" + VARIABLE);
                        continue;
                    }

                    children.Add(transform.gameObject);
                }

                GameObject temp = children.FirstOrDefault();
                if (temp is null)
                {
                    return result;
                }

                SkinnedMeshRenderer skinnedMeshRenderer = temp.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (skinnedMeshRenderer is null)
                {
                    return result;
                }

                result = (Texture2D)skinnedMeshRenderer.sharedMaterial.mainTexture;
            }

            return result;
        }

        public void SetTexture2D(Element element, Texture render)
        {
            DressGroup dressGroup = basicList.Find(x => x.elements.Contains(element));
            if (dressGroup is null)
            {
                return;
            }

            if (!groupOptions.IsChild(element))
            {
                SkinnedMeshRenderer skinnedMeshRenderer = dressGroup.gameObject.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (skinnedMeshRenderer is null)
                {
                    return;
                }

#if UNITY_EDITOR
                skinnedMeshRenderer.sharedMaterial.shader = Shader.Find("Unlit/Texture");
#endif
                skinnedMeshRenderer.sharedMaterial.mainTexture = render;
            }
            else
            {
                string[] pathList = groupOptions.GetChildPath(element);
                List<GameObject> children = new List<GameObject>();
                foreach (var VARIABLE in pathList)
                {
                    Transform transform = dressGroup.gameObject.transform.Find(VARIABLE);
                    if (transform == null)
                    {
                        Debug.Log(dressGroup.name + " Not children gameobject:" + VARIABLE);
                        continue;
                    }

                    children.Add(transform.gameObject);
                }

                GameObject temp = children.FirstOrDefault();
                if (temp is null)
                {
                    return;
                }

                SkinnedMeshRenderer skinnedMeshRenderer = temp.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (skinnedMeshRenderer is null)
                {
                    return;
                }
#if UNITY_EDITOR
                skinnedMeshRenderer.sharedMaterial.shader = Shader.Find("Unlit/Texture");
#endif
                skinnedMeshRenderer.sharedMaterial.mainTexture = render;
            }
        }

        public void ShowInView(Element element)
        {
            DressGroup group = basicList.Find(x => x.elements.Contains(element));
            if (group is null)
            {
                camera?.ToViewCenter(gameObject);
                return;
            }

            camera?.ToViewCenter(group.gameObject);
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
                    EventNotify.Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.UPLOAD_AVATAR_ICON_FAIL);
                    return;
                }

                config.group = this.group;
                config.icon = response.data.url;
                config.md5 = response.data.md5;
                foreach (var component in dataList.Values)
                {
                    if (component is null)
                    {
                        Debug.Log("empty element data");
                        continue;
                    }

                    config.AddConfig(component);
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
                EventNotify.Notify(EventNames.EXPORT_AVATAR_CONFIG_COMPLATED, json);
            }).StartCoroutine();
        }

        public DressupData GetElementData(Element element)
        {
            if (dataList.TryGetValue(element, out DressupData component))
            {
                return component;
            }

            return default;
        }


        public void ImportConfig(string config)
        {
            if (config.IsNullOrEmpty())
            {
                EventNotify.Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.CONFIG_DATA_IS_NULL_OR_EMPTY);
                return;
            }

            try
            {
                DressupConfig tempConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<DressupConfig>(config);
                if (tempConfig.group.IsNullOrEmpty() || tempConfig.group != group)
                {
                    EventNotify.Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.ELEMENT_GROUP_NOT_THE_SAME);
                    return;
                }

                ClearElement(Element.None);
                SetElementData(tempConfig.items, () => EventNotify.Notify(EventNames.IMPORT_CONFIG_COMPLATED, default(object)));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EventNotify.Notify(EventNames.ERROR_MESSAGE_NOTICE, string.Format(ErrorInfo.IMPORT_AVATAR_CONFIG_FAIL, e.ToString()));
            }
        }

        public void ClearElement(Element element)
        {
            if (element is Element.None)
            {
                foreach (var VARIABLE in basicList)
                {
                    GameObject.DestroyImmediate(VARIABLE.gameObject);
                }

                dataList.Clear();
                basicList.Clear();
                EventNotify.Notify(EventNames.CLEAR_ELMENT_DATA_COMPLATED, element);
                return;
            }

            if (!dataList.TryGetValue(element, out DressupData component))
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

            dataList.Remove(element);
            EventNotify.Notify(EventNames.CLEAR_ELMENT_DATA_COMPLATED, element);
        }

        private bool isRunning = false;
        private Queue<TaskData> taskList = new Queue<TaskData>();

        class TaskData
        {
            public Action callback;
            public List<DressupData> elements;
        }

        public void SetElementData(List<DressupData> elements, Action action)
        {
            taskList.Enqueue(new TaskData() { elements = elements, callback = action });
            if (isRunning)
            {
                return;
            }

            SetElementDataTask().StartCoroutine();
        }

        private IEnumerator SetElementDataTask()
        {
            if (taskList.Count is 0)
            {
                yield break;
            }

            isRunning = true;
            TaskData task = taskList.Dequeue();
            List<DressupData> elements = task.elements;

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
                if (!dataList.TryGetValue(dressupData.element, out DressupData component))
                {
                    yield return LoadAsync<GameObject>(dressupData.model, 0, 0, args =>
                    {
                        args.SetParent(gameObject, Vector3.zero, Vector3.zero, Vector3.one);
                        basicList.Add(new DressGroup()
                        {
                            name = modleName,
                            gameObject = args,
                            elements = new List<Element>() { dressupData.element }
                        });
                    });
                }
                else
                {
                    DressGroup group = basicList.Find(x => x.name == modleName);
                    if (group is null)
                    {
                        ClearElement(dressupData.element);
                        yield return LoadAsync<GameObject>(dressupData.model, 0, 0, args =>
                        {
                            args.SetParent(gameObject, Vector3.zero, Vector3.zero, Vector3.one);
                            basicList.Add(new DressGroup()
                            {
                                name = modleName,
                                gameObject = args,
                                elements = new List<Element>() { dressupData.element }
                            });
                        });
                    }
                }

                if (component is null || component.texture.Equals(dressupData.texture) is false)
                {
                    if (dressupData.publish_status == 2)
                    {
                        yield return CombineDrawingData(dressupData);
                    }
                    else
                    {
                        yield return LoadAsync<Texture2D>(dressupData.texture, 0, 0, args => { SetTexture2D(dressupData.element, args); });
                    }
                }

                dataList[dressupData.element] = dressupData;
            }

            ShowInView(Element.None);
            isRunning = false;
            task.callback?.Invoke();
            EventNotify.Notify(EventNames.SET_ELEMENT_DATA_COMPLATED, default(object));
            SetElementDataTask().StartCoroutine();
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

        public static Texture2D CombineTexture(List<DrawingData> layers, Texture2D org)
        {
            RenderTexture render = new RenderTexture(layers[0].width, layers[0].height, 0, RenderTextureFormat.Default);
            render.Clear();
            render.DrawTexture(new Rect(0, 0, org.width, org.height), org, null);
            for (int i = 0; i < layers.Count; i++)
            {
                render.DrawTexture(new Rect(0, 0, layers[i].width, layers[i].height), layers[i].texture, null);
            }

            return render.ReadTexture2D();
        }

        IEnumerator CombineDrawingData(DressupData data)
        {
            UnityWebRequest request = UnityWebRequest.Get(data.texture);
            yield return request.SendWebRequest();
            if (request.isDone is false || request.result is not UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                yield break;
            }

            List<DrawingData> layers = new List<DrawingData>();
            using (BinaryReader reader = new BinaryReader(new MemoryStream(request.downloadHandler.data)))
            {
                Element element = (Element)reader.ReadByte();
                DressupData temp = Newtonsoft.Json.JsonConvert.DeserializeObject<DressupData>(reader.ReadString());
                yield return LoadAsync<Texture2D>(temp.texture, 0, 0, args =>
                {
                    if (args == null)
                    {
                        EventNotify.Notify(EventNames.ERROR_MESSAGE_NOTICE, string.Format(ErrorInfo.NOT_FIND_THE_ELEMENT_ASSET, temp.texture) + 4);
                        return;
                    }

                    byte layerCount = reader.ReadByte();
                    for (int j = 0; j < layerCount; j++)
                    {
                        layers.Add(DrawingData.GenerateToBinary(reader));
                    }

                    if (layers.Count <= 0)
                    {
                        return;
                    }

                    SetTexture2D(data.element, CombineTexture(layers, args));
                });
            }
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

            basicList.ForEach(x => x.gameObject.SetActive(false));
        }

        public void UploadAsset(Element element)
        {
            void Runnable_OpenFileComplated(object args)
            {
                byte[] bytes = (byte[])args;
                if (!this.dataList.TryGetValue(element, out DressupData component))
                {
                    return;
                }

                Texture2D texture = new Texture2D(512, 512);
                texture.LoadImage(bytes);
                SetTexture2D(element, texture);
                API.UploadElementData(address, user, pid, Guid.NewGuid().ToString(), string.Empty, bytes, GetDressupGameObject(element), component, API.PublishState.Process, args =>
                {
                    if (args == null)
                    {
                        EventNotify.Notify(EventNames.UPLOAD_ELEMENT_ASSET_COMPLATED, string.Empty);
                        return;
                    }

                    EventNotify.Notify(EventNames.UPLOAD_ELEMENT_ASSET_COMPLATED, Newtonsoft.Json.JsonConvert.SerializeObject(args));
                });
            }

            EventNotify.Register(EventNames.OPEN_FILE_COMPLATED, Runnable_OpenFileComplated);
            OpenFileCallback();
        }


        public void PreviewAsset(Element element)
        {
            void Runnable_OpenFileComplated(object args)
            {
                byte[] bytes = (byte[])args;
                if (bytes == null || bytes.Length == 0)
                {
                    return;
                }

                if (!this.dataList.TryGetValue(element, out DressupData component))
                {
                    return;
                }

                Texture2D texture = new Texture2D(512, 512);
                texture.LoadImage(bytes);
                SetTexture2D(element, texture);
                ShowInView(element);
            }

            EventNotify.Register(EventNames.OPEN_FILE_COMPLATED, Runnable_OpenFileComplated);
            OpenFileCallback();
        }

        public void DisableElement(Element element)
        {
            if (element == Element.None)
            {
                List<DressGroup> groups = new List<DressGroup>();
                foreach (var VARIABLE in normalList)
                {
                    groups.AddRange(basicList.Where(x => x.elements.Contains(VARIABLE)));
                }

                basicList.ForEach(x => x.gameObject.SetActive(false));
                groups.ForEach(x => x.gameObject.SetActive(true));
                return;
            }

            if (normalList.Contains(element))
            {
                return;
            }

            DressGroup dressGroup = basicList.Find(x => x.elements.Contains(element));
            if (!groupOptions.IsChild(element))
            {
                dressGroup.gameObject.SetActive(false);
            }
            else
            {
                string[] pathList = groupOptions.GetChildPath(element);
                List<GameObject> children = new List<GameObject>();
                foreach (var VARIABLE in pathList)
                {
                    Transform transform = dressGroup.gameObject.transform.Find(VARIABLE);
                    if (transform == null)
                    {
                        continue;
                    }

                    transform.gameObject.SetActive(false);
                }
            }
        }

        public void EnableElement(Element element)
        {
            if (element == Element.None)
            {
                basicList.ForEach(x => x.gameObject.SetActive(true));
                return;
            }

            GetDressupGameObject(element)?.SetActive(true);
        }

        public void Dispose()
        {
            dataList.Clear();
            basicList.ForEach(x => GameObject.DestroyImmediate(x.gameObject));
            basicList.Clear();
            GameObject.DestroyImmediate(this.gameObject);
            group = string.Empty;
            options = null;
        }
    }
}