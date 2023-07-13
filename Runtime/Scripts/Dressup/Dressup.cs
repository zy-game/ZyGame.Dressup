using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZyGame.Replacement
{
    public class DressupOptions
    {
        /// <summary>
        /// 接口地址
        /// </summary>
        public string address;
        /// <summary>
        /// 基础骨骼
        /// </summary>
        public string skeleton;
        /// <summary>
        /// 基础骨骼版本号
        /// </summary>
        public uint version;
        /// <summary>
        /// 基础骨骼唯一码
        /// </summary>
        public uint crc;
        /// <summary>
        /// 挂点配置
        /// </summary>
        public string config;
        /// <summary>
        /// 用户在avatar中的UID
        /// </summary>
        public string userId;
        /// <summary>
        /// 模型品质
        /// </summary>
        public string quality;
        /// <summary>
        /// 项目ID
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
        public GameObject combine { get; private set; }
        public GameObject boneRoot { get; private set; }
        public GameObject skinRoot { get; private set; }
        public GameObject gameObject { get; private set; }
        public DressupOptions options { get; private set; }
        public List<NodeData> nodeList { get; private set; }
        public IAssetLoader AssetLoader { get; private set; }
        public Action<string, object> Notify { get; private set; }
        public Dictionary<Element, DressupComponent> dressups { get; }
        public Action OpenFileCallback { get; private set; }
        public Action<byte[]> LoadFileCompeltion { get; private set; }

        private const int COMBINE_TEXTURE_MAX = 2048;
        private const string COMBINE_DIFFUSE_TEXTURE = "_MainTex";

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
            if (result is null)
            {
                Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.INITIALIZE_AVATAR_ERROR_NOT_FIND_THE_SKELETON);
            }
            gameObject = result;
            gameObject.SetParent(null, Vector3.zero, Vector3.zero, Vector3.one);
            boneRoot = gameObject.transform.Find("DeformationSystem/root")?.gameObject;
            skinRoot = gameObject.transform.Find("Geometry")?.gameObject;
            this.camera.transform.position = options.cameraPosition;
            Notify(EventNames.INITIALIZED_COMPLATED_EVENT, string.Empty);
        }
        /// <summary>
        /// 清理部件
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

        /// <summary>
        /// 将部位显示在视图中心
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
        /// 导出配置
        /// </summary>
        /// <param name="configName">保存的配置名</param>
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
        /// 获取部件数据
        /// </summary>
        /// <param name="element">部位枚举</param>
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
        /// 导入配置
        /// </summary>
        /// <param name="config">配置数据</param>
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
        /// 设置部件模型
        /// </summary>
        /// <param name="elementData">部位数据</param>
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
        /// 设置部件模型
        /// </summary>
        /// <param name="elementData">部位数据</param>
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
        /// 合并Avatar
        /// </summary>
        public void Combine()
        {
            float startTime = Time.realtimeSinceStartup;
            if (combine is not null)
            {
                GameObject.DestroyImmediate(combine);
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            //foreach (var mesh in dressups.Values)
            //{
            //    meshes.AddRange(mesh.GetSkinnedMeshRenderers());
            //}
            // Fetch all bones of the skeleton
            List<Transform> transforms = new List<Transform>();
            transforms.AddRange(boneRoot.GetComponentsInChildren<Transform>(true));

            List<Material> materials = new List<Material>();//the list of materials
            List<CombineInstance> combineInstances = new List<CombineInstance>();//the list of meshes
            List<Transform> bones = new List<Transform>();//the list of bones

            // Below informations only are used for merge materilas(bool combine = true)
            List<Vector2[]> oldUV = null;
            Material newMaterial = null;
            Texture2D newDiffuseTex = null;

            // Collect information from meshes
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
            // merge the texture
            List<Texture2D> Textures = new List<Texture2D>();
            for (int i = 0; i < materials.Count; i++)
            {
                Textures.Add(materials[i].GetTexture(COMBINE_DIFFUSE_TEXTURE) as Texture2D);
            }

            newDiffuseTex = new Texture2D(COMBINE_TEXTURE_MAX, COMBINE_TEXTURE_MAX, TextureFormat.RGBA32, true);
            Rect[] uvs = newDiffuseTex.PackTextures(Textures.ToArray(), 0);
            newMaterial.mainTexture = newDiffuseTex;

            // reset uv
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
            r.sharedMesh.CombineMeshes(combineInstances.ToArray(), true, false);// Combine meshes
            r.bones = bones.ToArray();// Use new bones
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
            //SkinnedMeshCombiner skinnedMeshCombiner = new SkinnedMeshCombiner();
            //skinnedMeshCombiner.allInOneParams.materialToUse = new Material(Shader.Find("UniversalRenderPipeline/Lit"));
            //skinnedMeshCombiner.CombineMeshes(SkinnedMeshCombiner.MergeMethod.AllInOne, gameObject);
            //combine = skinnedMeshCombiner.resultMergeGameObject.SetParent(skinRoot, Vector3.zero, Vector3.zero, Vector3.one);
            //Debug.Log("合并耗时 : " + (Time.realtimeSinceStartup - startTime) * 1000 + " ms");
        }

        /// <summary>
        /// 获取最接近输入值的2的N次方的数，最大不会超过1024，例如输入320会得到512
        /// </summary>
        public int get2Pow(int into)
        {
            int outo = 1;
            for (int i = 0; i < 10; i++)
            {
                outo *= 2;
                if (outo > into)
                {
                    break;
                }
            }

            return outo;
        }


        /// <summary>
        /// 上传部件资源
        /// </summary>
        /// <param name="element">部件位置</param>
        /// <param name="fileDataString">文件数据</param>
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
        /// 预览
        /// </summary>
        /// <param name="element">部位枚举</param>
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
        /// 隐藏部位
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
        /// 显示部位
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

        public NodeChild[] GetChildList(Element element)
        {
            if (nodeList is null || nodeList.Count is 0)
            {
                return Array.Empty<NodeChild>();
            }
            NodeData nodeData = nodeList.Find(x => x.element == element);
            if (nodeData is null)
            {
                return default;
            }
            return nodeData.childs.ToArray();
        }
    }
}