using Newtonsoft.Json;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using ZyGame.Dressup;
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
            serializedObject = new SerializedObject(AvatarElementConfig.Load());

            if (AvatarElementConfig.instance.elements is null)
            {
                AvatarElementConfig.instance.elements = new List<ElementItemData>();
            }

            if (AvatarElementConfig.instance.groups is null)
            {
                AvatarElementConfig.instance.groups = new List<EditorGroupData>();
            }

            ResetGroupList();
            groups = AvatarElementConfig.instance.groups.Select(x => x.name).ToArray();
        }

        private void ResetGroupList()
        {
            groupList = new Dictionary<string, List<ElementItemData>>();
            for (int i = 0; i < AvatarElementConfig.instance.groups.Count; i++)
            {
                if (AvatarElementConfig.instance.groups[i].name.IsNullOrEmpty())
                {
                    continue;
                }

                if (groupList.ContainsKey(AvatarElementConfig.instance.groups[i].name))
                {
                    continue;
                }

                groupList.Add(AvatarElementConfig.instance.groups[i].name, new List<ElementItemData>());
            }

            for (int i = AvatarElementConfig.instance.elements.Count - 1; i >= 0; i--)
            {
                ElementItemData itemData = AvatarElementConfig.instance.elements[i];
                if (itemData.fbx is null || itemData.texture is null)
                {
                    AvatarElementConfig.instance.elements.Remove(itemData);
                    continue;
                }

                if (itemData.icon is null)
                {
                    GetPreviewTexture(itemData);
                }

                if (itemData.group.IsNullOrEmpty())
                {
                    EditorGroupData tiletData = AvatarElementConfig.instance.groups.FirstOrDefault();
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
                    AvatarElementConfig.instance.groups?.Clear();
                    AvatarElementConfig.instance.nodes?.Clear();
                    AvatarElementConfig.instance.normals?.Clear();
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

            EditorGUILayout.PropertyField(serializedObject.FindProperty("normals"), new GUIContent("Normal List"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("groups"), new GUIContent("Group List"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nodes"), new GUIContent("None List"), true);
            if (GUILayout.Button("Export Json Data"))
            {
                string folder = EditorPrefs.GetString("element_output", Application.dataPath);
                string temp = EditorUtility.OpenFolderPanel("选择输出路径...", folder, string.Empty);
                if (temp.IsNullOrEmpty())
                {
                    return;
                }

                List<ElementGroupData> groups = new List<ElementGroupData>();
                foreach (var item in AvatarElementConfig.instance.groups)
                {
                    groups.Add(new ElementGroupData
                    {
                        name = item.name,
                        skelton = item.skelton.name
                    });
                }

                // InitConfig write = new InitConfig()
                // {
                //     nodes = AvatarElementConfig.instance.nodes,
                //     groups = groups,
                //     normal = AvatarElementConfig.instance.normals,
                // };

                // File.WriteAllText(temp + "/avatar_setting.json", JsonConvert.SerializeObject(write));
                EditorUtility.DisplayDialog("Tips", "Export Avatar Config Completion", "Ok");
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

                        ShowElementData(group[j]);
                    }
                }
            }

            CheckMouseDragEvent(rect);
            CheckMouseDragdropEvent(rect);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void ShowElementData(ElementItemData itemData)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();

            GUILayout.Label(itemData.icon, GUILayout.Width(100), GUILayout.Height(100));
            GUILayout.Label(itemData.texture, GUILayout.Width(100), GUILayout.Height(100));
            GUILayout.BeginVertical();
            GUILayout.Label(itemData.fbx.name);
            GUILayout.Label(AssetDatabase.GetAssetPath(itemData.fbx));
            Element t = (Element)EditorGUILayout.EnumPopup("Element", itemData.element);
            if (t != itemData.element)
            {
                itemData.element = t;
                Renderer renderer = null;
                if (AvatarElementConfig.instance.IsChild(t, itemData.group))
                {
                    Debug.Log("重置????" + itemData.element);
                    string[] pathList = AvatarElementConfig.instance.GetChildPath(t, itemData.group);
                    if (pathList is not null && pathList.Length > 0)
                    {
                        Transform transform = itemData.fbx.transform.Find(pathList[0]);
                        renderer = transform?.GetComponent<Renderer>();
                    }
                }
                else
                {
                    NodeData nodeData = AvatarElementConfig.instance.nodes.Find(x => x.basic == itemData.element && x.group == itemData.group);
                    if (nodeData is null)
                    {
                        renderer = itemData.fbx?.GetComponentInChildren<SkinnedMeshRenderer>();
                    }
                    else
                    {
                        Transform transform = itemData.fbx.transform.Find(nodeData.path);
                        renderer = transform?.GetComponent<Renderer>();
                    }

                    if (renderer != null)
                    {
                        itemData.texture = (Texture2D)renderer.sharedMaterial.mainTexture;
                    }
                }

                if (renderer != null)
                {
                    itemData.texture = (Texture2D)renderer.sharedMaterial.mainTexture;
                }

                itemData.icon = AssetPreview.GetAssetPreview(itemData.fbx);
                itemData.isNormal = AvatarElementConfig.instance.normals.Contains(t);
                AvatarElementConfig.Save();
            }

            string g = groups[EditorGUILayout.Popup("Group", Mathf.Clamp(Array.IndexOf(groups, itemData.group), 0, groupList.Count), groups)];
            if (g != itemData.group)
            {
                itemData.group = g;
                AvatarElementConfig.Save();
                OnEnable();
                this.Repaint();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Delete"))
            {
                AvatarElementConfig.instance.elements.Remove(itemData);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(itemData.icon));
                AvatarElementConfig.Save();
                OnEnable();
                this.Repaint();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }


        private void OnBuild(params ElementItemData[] elements)
        {
            string folder = EditorPrefs.GetString("element_output", Application.dataPath);
            string temp = EditorUtility.OpenFolderPanel("选择输出路径...", folder, string.Empty);
            if (temp.IsNullOrEmpty())
            {
                return;
            }

            EditorPrefs.SetString("element_output", folder = temp);

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
                if (AvatarElementConfig.instance.IsChild(elements[i].element, elements[i].group))
                {
                    continue;
                }

                builds.Add(new AssetBundleBuild()
                {
                    assetBundleName = $"{elements[i].fbx.name.ToLower()}.assetbundle",
                    assetNames = new string[] { AssetDatabase.GetAssetPath(elements[i].fbx) }
                });
            }

            foreach (var g in AvatarElementConfig.instance.groups)
            {
                if (builds.Where(x => x.assetBundleName == $"{g.skelton.name.ToLower()}.assetbundle").Count() > 0)
                {
                    continue;
                }

                builds.Add(new AssetBundleBuild()
                {
                    assetBundleName = $"{g.skelton.name.ToLower()}.assetbundle",
                    assetNames = new string[] { AssetDatabase.GetAssetPath(g.skelton) }
                });
            }


            string bundlePath = $"{folder}/assetbundles";
            CreateDirectory(bundlePath);
            BuildPipeline.BuildAssetBundles(bundlePath, builds.ToArray(), BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            Dictionary<string, List<OutData>> jsons = new Dictionary<string, List<OutData>>();
            foreach (var item in elements)
            {
                item.version++;

                if (!jsons.TryGetValue(item.group, out List<OutData> datas))
                {
                    jsons.Add(item.group, datas = new List<OutData>());
                }

                OutData parent = WriteData(bundlePath, folder, item);
                if (parent is null)
                {
                    continue;
                }

                datas.Add(parent);
            }

            foreach (var item in AvatarElementConfig.instance.groups)
            {
                OutData outData = WriteData(bundlePath, folder, new ElementItemData()
                {
                    isNormal = true,
                    icon = item.texture,
                    element = Element.None,
                    group = item.name,
                    fbx = item.skelton,
                    texture = null,
                    version = 0,
                    
                });
                if (jsons.TryGetValue(item.name, out List<OutData> list))
                {
                    list.Add(outData);
                }
            }


            foreach (var item in jsons)
            {
                File.WriteAllText($"{folder}/{item.Key}/elements.json", Newtonsoft.Json.JsonConvert.SerializeObject(item.Value));
            }

            Directory.Delete(bundlePath, true);
            AvatarElementConfig.Save();
        }

        private OutData WriteData(string bundlePath, string folder, ElementItemData itemData, uint crc = 0, uint version = 0)
        {
            string iconPath = $"{folder}/{itemData.group}/icons/{itemData.fbx.name}_icon.png";
            string elementPath = $"{folder}/{itemData.group}/element/";
            string bundleFilePath = String.Empty;

            if (itemData.element is Element.None)
            {
                bundleFilePath = $"{bundlePath}/{itemData.fbx.name.ToLower()}.assetbundle";
            }
            else
            {
                bundleFilePath = $"{bundlePath}/{itemData.fbx.name.ToLower()}.assetbundle";
            }

            //todo 移动资源包
            if (File.Exists(bundleFilePath))
            {
                string path = $"{bundlePath}/{itemData.fbx.name.ToLower()}.assetbundle";
                BuildPipeline.GetCRCForAssetBundle(path, out crc);
                string dest = $"{folder}/{itemData.group}/bundles/{Path.GetFileName(bundleFilePath)}";
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }

                CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(bundleFilePath, dest);
            }

            CreateDirectory(Path.GetDirectoryName(iconPath));
            CreateDirectory(Path.GetDirectoryName(elementPath));
            string texturePath = string.Empty;
            //todo 移动部件图片
            if (itemData.texture != null)
            {
                Texture2D texture2D = new Texture2D(itemData.texture.width, itemData.texture.height, TextureFormat.RGBA32, false);
                texture2D.SetPixels(itemData.texture.GetPixels());
                texturePath = $"{elementPath}/{itemData.texture.name}.png";
                File.WriteAllBytes(texturePath, texture2D.EncodeToPNG());
            }

            //todo 移动部件ICON
            if (itemData.icon != null)
            {
                File.WriteAllBytes(iconPath, itemData.icon.EncodeToPNG());
            }

            if (itemData.element is Element.None)
            {
                bundleFilePath = $"{itemData.group}/bundles/{itemData.fbx.name}.assetbundle";
            }
            else
            {
                bundleFilePath = $"{itemData.group}/bundles/{itemData.fbx.name}.assetbundle";
            }

            return new OutData
            {
                icon = $"{itemData.group}/icons/{itemData.fbx.name.ToLower()}_icon.png",
                is_normal = itemData.isNormal,
                element = (int)itemData.element,
                group = itemData.group,
                model = bundleFilePath,
                texture = itemData.texture == null ? string.Empty : $"{itemData.group}/element/{itemData.texture.name}.png",
                version = version == 0 ? itemData.version : version,
                crc = crc
            };
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

            if (Event.current.type is not EventType.DragUpdated)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }

        private void CheckMouseDragdropEvent(Rect rect)
        {
            if (rect.Contains(Event.current.mousePosition) is not true)
            {
                return;
            }

            if (Event.current.type is not EventType.DragPerform)
            {
                return;
            }

            Debug.Log(AvatarElementConfig.instance.iconOutput);
            if (AvatarElementConfig.instance.iconOutput == null || AvatarElementConfig.instance.iconOutput is default(Object))
            {
                if (EditorUtility.DisplayDialog("Error", "Not Setting Icon Output Path", "OK"))
                {
                    isShowSetting = true;
                }

                return;
            }

            for (int i = 0; i < DragAndDrop.paths.Length; i++)
            {
                if (Path.GetExtension(DragAndDrop.paths[i]).IsNullOrEmpty())
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
                    // childs = new List<ElementItemData>(),
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
                    Texture2D texture = m_PreviewRenderUtility.camera.targetTexture.ReadTexture2D(); //(Texture2D)m_PreviewRenderUtility.EndPreview();
                    m_PreviewRenderUtility.Cleanup();
                    string iconPath = AssetDatabase.GetAssetPath(AvatarElementConfig.instance.iconOutput) + $"/{element.fbx.name}_icon.png";
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