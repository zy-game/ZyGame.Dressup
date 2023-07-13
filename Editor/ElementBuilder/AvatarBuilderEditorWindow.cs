using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEditorInternal;
using UnityEngine;
using ZyGame.Replacement;
using static UnityEditor.Progress;
using Object = UnityEngine.Object;

namespace ZyGame.Editor.Avatar
{
    internal class AvatarBuilderEditorWindow : EditorWindow
    {
        private Vector2 pos;
        private string search;
        private string[] groups;
        private bool isShowSetting = false;
        private SerializedObject serializedObject;
        private Dictionary<string, List<ElementItemData>> groupList;
        private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        [MenuItem("Game/Editor/Element Build")]
        public static void Open()
        {
            GetWindow<AvatarBuilderEditorWindow>(false, "Element Build", true);
        }

        private void OnGUI()
        {

            if (serializedObject is null)
            {
                OnEnable();
            }
            OnDrawingToolbarGUI();
            pos = GUILayout.BeginScrollView(pos);
            if (isShowSetting)
            {
                ShowAvatarOptionWindow();
            }
            else
            {
                ShowAvatarElementList();
            }

            GUILayout.EndScrollView();
        }
        private void OnEnable()
        {
            serializedObject = new SerializedObject(AvatarElementConfig.instance);

            if (AvatarElementConfig.instance.elements is null)
            {
                AvatarElementConfig.instance.elements = new List<ElementItemData>();
            }

            if (AvatarElementConfig.instance.tilets is null)
            {
                AvatarElementConfig.instance.tilets = new List<TiletData>();
            }

            ResetGroupList();
            groups = AvatarElementConfig.instance.tilets.Select(x => x.name).ToArray();
        }

        private void ResetGroupList()
        {
            groupList = new Dictionary<string, List<ElementItemData>>();
            for (int i = 0; i < AvatarElementConfig.instance.tilets.Count; i++)
            {
                if (AvatarElementConfig.instance.tilets[i].name.IsNullOrEmpty())
                {
                    continue;
                }
                if (groupList.ContainsKey(AvatarElementConfig.instance.tilets[i].name))
                {
                    continue;
                }
                groupList.Add(AvatarElementConfig.instance.tilets[i].name, new List<ElementItemData>());
            }
            for (int i = 0; i < AvatarElementConfig.instance.elements.Count; i++)
            {
                ElementItemData itemData = AvatarElementConfig.instance.elements[i];
                if (itemData.group.IsNullOrEmpty())
                {
                    TiletData tiletData = AvatarElementConfig.instance.tilets.FirstOrDefault();
                    itemData.group = tiletData?.name;
                }
                if (groupList.TryGetValue(itemData.group, out List<ElementItemData> list))
                {
                    list.Add(itemData);
                }
            }
        }

        private void OnDrawingToolbarGUI()
        {

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                if (isShowSetting)
                {
                    AvatarElementConfig.instance.iconOutput = null;
                    AvatarElementConfig.instance.tilets?.Clear();
                    AvatarElementConfig.instance.nodes?.Clear();
                    AvatarElementConfig.instance.normal?.Clear();
                }
                else
                {
                    AvatarElementConfig.instance.elements?.Clear();
                }
                AvatarElementConfig.Save();
                OnEnable();
            }
            if (GUILayout.Button("Setting", EditorStyles.toolbarButton))
            {
                isShowSetting = !isShowSetting;
            }
            if (GUILayout.Button("Build", EditorStyles.toolbarButton))
            {
                GenericMenu menu = new GenericMenu();
                Dictionary<string, List<ElementItemData>> groups = new Dictionary<string, List<ElementItemData>>();
                for (int i = 0; i < AvatarElementConfig.instance.elements.Count; i++)
                {
                    if (!groups.TryGetValue(AvatarElementConfig.instance.elements[i].group, out List<ElementItemData> list))
                    {
                        groups.Add(AvatarElementConfig.instance.elements[i].group, list = new List<ElementItemData>());
                    }
                    list.Add(AvatarElementConfig.instance.elements[i]);

                }
                menu.AddItem(new GUIContent("All"), false, () => { OnBuild(AvatarElementConfig.instance.elements.ToArray()); });
                foreach (var item in groups)
                {
                    List<ElementItemData> packageConfigs = item.Value;
                    menu.AddItem(new GUIContent("Build Group/" + item.Key), false, () => { OnBuild(packageConfigs.ToArray()); });
                }
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();
        }


        private void ShowAvatarOptionWindow()
        {
            EditorGUI.BeginChangeCheck();
            AvatarElementConfig.instance.iconOutput = EditorGUILayout.ObjectField("Icon Texture Output", AvatarElementConfig.instance.iconOutput, typeof(Object), false);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("normal"), new GUIContent("Normal List"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tilets"), new GUIContent("Group List"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nodes"), new GUIContent("None List"), true);
            if (GUILayout.Button("Export Json Data"))
            {
                string folder = EditorPrefs.GetString("element_output", Application.dataPath);
                string temp = EditorUtility.OpenFolderPanel("选择输出路径...", folder, string.Empty);
                if (temp.IsNullOrEmpty())
                {
                    return;
                }
                File.WriteAllText(temp + "/nodeList.json", Newtonsoft.Json.JsonConvert.SerializeObject(AvatarElementConfig.instance.nodes));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                AvatarElementConfig.Save();
                OnEnable();
            }
        }
        private void ShowAvatarElementList()
        {

            Rect rect = EditorGUILayout.BeginVertical();
            if (AvatarElementConfig.instance.elements is null)
            {
                OnEnable();
            }

            foreach (var item in groupList)
            {
                if (!foldouts.TryGetValue(item.Key, out bool state))
                {
                    foldouts.Add(item.Key, false);
                }
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                foldouts[item.Key] = EditorGUILayout.Foldout(foldouts[item.Key], item.Key);
                GUILayout.FlexibleSpace();
                GUILayout.Label(item.Value.Count.ToString());
                GUILayout.EndHorizontal();
                if (foldouts[item.Key])
                {
                    List<ElementItemData> group = item.Value; /*AvatarElementConfig.instance.elements.Where(x => x?.group == item.Key).ToList();*/

                    for (int j = group.Count - 1; j >= 0; j--)
                    {
                        if (search.IsNullOrEmpty() is false && group[j].fbx.name.Contains(search) is false)
                        {
                            continue;
                        }
                        ShowElementData(group[j], false);
                    }
                }
            }
            CheckMouseDragEvent(rect);
            CheckMouseDragdropEvent(rect);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

        }

        private void ShowElementData(ElementItemData itemData, bool isChild)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label(itemData.icon, GUILayout.Width(100), GUILayout.Height(100));
            GUILayout.BeginVertical();
            GUILayout.Label(itemData.fbx.name);
            GUILayout.Label(AssetDatabase.GetAssetPath(itemData.fbx));
            Element t = (Element)EditorGUILayout.EnumPopup("Element", itemData.element);
            if (t != itemData.element)
            {
                itemData.element = t;
                itemData.isNormal = AvatarElementConfig.instance.normal.Contains(t);
                AvatarElementConfig.Save();
            }
            string g = groups[EditorGUILayout.Popup("Group", Array.IndexOf(groups, itemData.group), groups)];
            if (g != itemData.group)
            {
                itemData.group = g;
                AvatarElementConfig.Save();
                OnEnable();
                this.Repaint();
            }
            NodeChild[] nodeChilds = AvatarElementConfig.instance.GetChildren(itemData.element);
            if (nodeChilds is not null && nodeChilds.Length is not 0)
            {
                if (itemData.childs is null)
                {
                    itemData.childs = new List<ElementItemData>();
                }
                foreach (var child in nodeChilds)
                {
                    if (child.path is null || child.path.Count is 0)
                    {
                        Debug.Log("the child is not set path:" + child.element);
                    }
                    ElementItemData childData = itemData.childs.Find(x => x.element == child.element);
                    if (childData is null)
                    {
                        childData = new ElementItemData()
                        {
                            isNormal = AvatarElementConfig.instance.normal.Contains(t),
                            icon = itemData.icon,
                            childs = new List<ElementItemData>(),
                            element = child.element,
                            fbx = itemData.fbx.transform.Find(child.path[0]).gameObject,
                            group = itemData.group,
                            version = itemData.version,
                        };
                        Renderer renderer = childData.fbx.GetComponent<Renderer>();
                        childData.texture = (Texture2D)renderer.sharedMaterial.mainTexture;
                        itemData.childs.Add(childData);
                        AvatarElementConfig.Save();
                    }
                }
                if (itemData.childs is not null && itemData.childs.Count is not 0)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUI.enabled = false;
                    GUILayout.Label("Childs");
                    for (int i = 0; i < itemData.childs.Count; i++)
                    {
                        itemData.childs[i].group = itemData.group;
                        ShowElementData(itemData.childs[i], true);
                    }
                    GUI.enabled = true;
                    GUILayout.EndVertical();
                }
            }
            if (itemData.texture is null)
            {
                string path = AvatarElementConfig.instance.GetNodePath(itemData.element);
                Renderer renderer = null;
                if (path.IsNullOrEmpty() is false)
                {
                    renderer = itemData.fbx.transform.Find(path).GetComponent<Renderer>();
                }
                else
                {
                    renderer = itemData.fbx.GetComponentInChildren<Renderer>();
                }
                itemData.texture = (Texture2D)renderer.sharedMaterial.mainTexture;
                AvatarElementConfig.Save();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Delete"))
            {
                if (isChild)
                {
                    itemData.childs.Remove(itemData);
                }
                else
                {
                    AvatarElementConfig.instance.elements.Remove(itemData);
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(itemData.icon));
                }

                AvatarElementConfig.Save();
                OnEnable();
                this.Repaint();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        private void OnBuild(params ElementItemData[] elements)
        {
            string folder = EditorPrefs.GetString("element_output", Application.dataPath);
            string temp = EditorUtility.OpenFolderPanel("选择输出路径...", folder, string.Empty);
            if (temp.IsNullOrEmpty())
            {
                return;
            }
            if (temp.Equals(folder) is false)
            {
                EditorPrefs.SetString("element_output", folder = temp);
            }

            foreach (var item in elements)
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.fbx)) as ModelImporter;
                modelImporter.isReadable = true;
                modelImporter.importBlendShapes = item.element == Element.Head;
                modelImporter.importBlendShapeNormals = item.element == Element.Head ? ModelImporterNormals.Import : ModelImporterNormals.None;
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                modelImporter.SaveAndReimport();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
            for (int i = 0; i < elements.Length; i++)
            {
                builds.Add(new AssetBundleBuild()
                {
                    assetBundleName = elements[i].fbx.name.ToLower() + ".assetbundle",
                    assetNames = new string[] { AssetDatabase.GetAssetPath(elements[i].fbx) }
                });
            }
            string bundlePath = $"{folder}/assetbundles";
            CreateDirectory(bundlePath);
            BuildPipeline.BuildAssetBundles(bundlePath, builds.ToArray(), BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            Dictionary<string, List<OutData>> jsons = new Dictionary<string, List<OutData>>();
            foreach (var item in elements)
            {
                if (!jsons.TryGetValue(item.group, out List<OutData> datas))
                {
                    jsons.Add(item.group, datas = new List<OutData>());
                }
                datas.Add(WriteData(bundlePath, folder, item));
                if (item.childs is null || item.childs.Count is 0)
                {
                    continue;
                }

                foreach (var c in item.childs)
                {
                    datas.Add(WriteData(bundlePath, folder, c));
                }
            }
            for (int i = 0; i < elements.Length; i++)
            {
                string bundleFilePath = $"{bundlePath}/{elements[i].fbx.name.ToLower()}.assetbundle";
                if (File.Exists(bundleFilePath) is false)
                {
                    continue;
                }
                string dest = $"{folder}/{elements[i].group}/bundles/{Path.GetFileName(bundleFilePath)}";
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }
                CreateDirectory(Path.GetDirectoryName(dest));
                File.Move(bundleFilePath, dest);
            }
            foreach (var item in jsons)
            {
                File.WriteAllText($"{folder}/{item.Key}/elements.json", Newtonsoft.Json.JsonConvert.SerializeObject(item.Value));
            }
            Directory.Delete(bundlePath, true);
            AvatarElementConfig.Save();
        }

        private OutData WriteData(string bundlePath, string folder, ElementItemData itemData)
        {
            string iconPath = $"{folder}/{itemData.group}/icons/{itemData.fbx.name}_icon.png";
            string elementPath = $"{folder}/{itemData.group}/element/";
            CreateDirectory(Path.GetDirectoryName(iconPath));
            CreateDirectory(Path.GetDirectoryName(elementPath));

            Texture2D texture2D = new Texture2D(itemData.texture.width, itemData.texture.height, TextureFormat.RGBA32, false);
            texture2D.SetPixels(itemData.texture.GetPixels());
            File.WriteAllBytes($"{elementPath}/{itemData.texture.name}.png", texture2D.EncodeToPNG());


            string path = $"{bundlePath}/{itemData.fbx.name.ToLower()}.assetbundle";
            BuildPipeline.GetCRCForAssetBundle(path, out uint crc);
            OutData outData = new OutData
            {
                icon = $"{itemData.group}/icons/{itemData.fbx.name}_icon.png",
                is_normal = itemData.isNormal,
                element = (int)itemData.element,
                group = itemData.group,
                model = itemData.fbx.name + ".assetbundle",
                texture = itemData.texture.name,
                version = ++itemData.version,
                crc = crc
            };
            byte[] bytes = itemData.icon.EncodeToPNG();
            File.WriteAllBytes(iconPath, bytes);
            return outData;
        }

        private void CreateDirectory(string path)
        {
            if (Directory.Exists(path) is false)
            {
                Directory.CreateDirectory(path);
            }
        }

        private void CheckMouseDragEvent(Rect rect)
        {
            if (Rect.zero.Equals(rect) is not true && rect.Contains(UnityEngine.Event.current.mousePosition) is not true)
            {
                return;
            }
            if (UnityEngine.Event.current.type is not EventType.DragUpdated)
            {
                return;
            }
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }
        private void CheckMouseDragdropEvent(Rect rect)
        {
            if (rect.Contains(UnityEngine.Event.current.mousePosition) is not true)
            {
                return;
            }
            if (UnityEngine.Event.current.type is not EventType.DragPerform)
            {
                return;
            }

            for (int i = 0; i < DragAndDrop.paths.Length; i++)
            {
                if (Path.GetExtension(DragAndDrop.paths[i]).IsNullOrEmpty())
                {
                    continue;
                }
                if (AvatarElementConfig.instance.elements.Find(x => AssetDatabase.GetAssetPath(x.fbx).Equals(DragAndDrop.paths[i])) is not null)
                {
                    continue;
                }
                ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(DragAndDrop.paths[i]);
                if (importer is not null)
                {
                    importer.isReadable = true;
                    importer.importBlendShapes = true;
                    importer.importVisibility = true;
                    importer.importCameras = true;
                    importer.importLights = true;
                    importer.sortHierarchyByName = true;
                    importer.meshCompression = ModelImporterMeshCompression.Off;
                    importer.meshOptimizationFlags = MeshOptimizationFlags.Everything;
                    importer.addCollider = false;
                    importer.keepQuads = false;
                    importer.weldVertices = true;
                    importer.indexFormat = ModelImporterIndexFormat.Auto;
                    importer.importBlendShapeNormals = ModelImporterNormals.Import;
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                    importer.skinWeights = ModelImporterSkinWeights.Standard;
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                    importer.SaveAndReimport();
                }

                ElementItemData element = new ElementItemData
                {
                    isNormal = false,
                    element = Element.None,
                    fbx = AssetDatabase.LoadAssetAtPath<GameObject>(DragAndDrop.paths[i]),
                    group = groups.FirstOrDefault(),
                    childs = new List<ElementItemData>(),
                };
                GetPreviewTexture(element);
                AssetDatabase.ExtractAsset(element.fbx, AssetDatabase.GetAssetPath(element.fbx));
                Renderer[] renderer = element.fbx.GetComponentsInChildren<Renderer>();
                if (renderer is null)
                {
                    Debug.Log("The FBX is Not find Renderer:" + element.fbx.name + " Path:" + AssetDatabase.GetAssetPath(element.fbx));
                    continue;
                }
                foreach (var r in renderer)
                {
                    if (r.sharedMaterial.mainTexture is null)
                    {
                        continue;
                    }
                    string p = AssetDatabase.GetAssetPath(r.sharedMaterial.mainTexture);
                    TextureImporter textureImporter = AssetImporter.GetAtPath(p) as TextureImporter;
                    textureImporter.isReadable = true;
                    textureImporter.mipmapEnabled = false;
                    textureImporter.SaveAndReimport();

                }
                AvatarElementConfig.instance.elements.Add(element);
            }
            AvatarElementConfig.Save();
            OnEnable();
        }

        private void GetPreviewTexture(ElementItemData element)
        {
            if (element.icon is null)
            {

                MeshFilter meshFilter = element.fbx.GetComponentInChildren<MeshFilter>();
                Mesh mesh = default;
                Material material = null;
                if (meshFilter is not null)
                {
                    mesh = meshFilter.sharedMesh;
                }
                else
                {
                    SkinnedMeshRenderer skinned = element.fbx.GetComponentInChildren<SkinnedMeshRenderer>();
                    mesh = skinned is null ? null : skinned.sharedMesh;
                }
                if (mesh is not null)
                {
                    PreviewRenderUtility m_PreviewRenderUtility = new PreviewRenderUtility();
                    m_PreviewRenderUtility.BeginPreview(new Rect(0, 0, 256, 256), GUIStyle.none);
                    m_PreviewRenderUtility.camera.targetTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
                    material = element.fbx.GetComponentInChildren<Renderer>()?.sharedMaterial;
                    mesh.RecalculateBounds();
                    m_PreviewRenderUtility.camera.transform.position = mesh.bounds.center;
                    m_PreviewRenderUtility.camera.transform.rotation = Quaternion.Euler(new Vector3(-45, 45, 0));
                    m_PreviewRenderUtility.camera.transform.position = m_PreviewRenderUtility.camera.transform.forward * 3f;
                    m_PreviewRenderUtility.camera.transform.LookAt(mesh.bounds.center, Vector3.up);
                    float distance = Vector3.Distance(m_PreviewRenderUtility.camera.transform.position, mesh.bounds.center);
                    Bounds meshBound = mesh.bounds;
                    m_PreviewRenderUtility.camera.fieldOfView = 2.0f * Mathf.Atan(Mathf.Max(meshBound.size.y, meshBound.size.x, meshBound.size.z) * 0.55f / distance) * Mathf.Rad2Deg;
                    m_PreviewRenderUtility.DrawMesh(mesh, Matrix4x4.identity, material, 0);
                    m_PreviewRenderUtility.camera.Render();
                    m_PreviewRenderUtility.EndPreview();
                    Texture2D texture = m_PreviewRenderUtility.camera.targetTexture.ReadTexture2D();//(Texture2D)m_PreviewRenderUtility.EndPreview();
                    m_PreviewRenderUtility.Cleanup();
                    string iconPath = AssetDatabase.GetAssetPath(AvatarElementConfig.instance.iconOutput) + "/" + element.fbx.name + "_icon.png";
                    File.WriteAllBytes(iconPath, texture.EncodeToPNG());
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    element.icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
                    TextureImporter textureImporter = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                    textureImporter.isReadable = true;
                    textureImporter.mipmapEnabled = false;
                    textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    textureImporter.SaveAndReimport();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                else
                {
                    element.icon = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                }
            }
        }
    }
}
