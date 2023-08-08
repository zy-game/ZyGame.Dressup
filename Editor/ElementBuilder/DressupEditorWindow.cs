using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZyGame.Dressup;
using Object = UnityEngine.Object;

namespace ZyGame.Editor.Avatar
{
    public class DressupEditorWindow : EditorWindow
    {
        [MenuItem("Game/Dressup")]
        public static void Open()
        {
            GetWindow<DressupEditorWindow>(false, "Dressup Editor", true);
        }


        private bool isOptions;
        private string search;
        private SerializedObject options;
        private Vector2 listScroll = Vector2.zero;
        private Vector2 optionsScroll = Vector2.zero;
        private Vector2 manifestScroll = Vector2.zero;

        public void OnEnable()
        {
            options = new SerializedObject(DressupEditorOptions.instance);

            foreach (var VARIABLE in DressupEditorOptions.instance.options)
            {
                foreach (var element in VARIABLE.elements)
                {
                    if (element.icon != null)
                    {
                        continue;
                    }

                    element.icon = GetPreviewTexture((GameObject)element.target);
                }
            }
        }

        private PreviewRenderUtility m_PreviewRenderUtility;

        private Texture2D GetPreviewTexture(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return Texture2D.normalTexture;
            }

            SkinnedMeshRenderer skinned = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            Mesh mesh = skinned.sharedMesh;
            Material material = skinned.sharedMaterial;
            if (m_PreviewRenderUtility == null)
            {
                m_PreviewRenderUtility = new PreviewRenderUtility();
                InternalEditorUtility.SetCustomLighting(m_PreviewRenderUtility.lights, new Color(0.6f, 0.6f, 0.6f, 1f));
            }

            m_PreviewRenderUtility.BeginPreview(new Rect(0, 0, 256, 256), GUIStyle.none);
            m_PreviewRenderUtility.camera.targetTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
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
            string iconPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(gameObject)) + $"/{gameObject.name}_icon.png";
            File.WriteAllBytes(iconPath, texture.EncodeToPNG());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Texture2D result = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            TextureImporter textureImporter = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            textureImporter.isReadable = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SaveAndReimport();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        public void OnGUI()
        {
            Toolbar();
            switch (isOptions)
            {
                case true:
                    DrawingOptions();
                    break;
                case false:
                    DrawingElementList();
                    break;
            }
        }

        void Toolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.FlexibleSpace();
                search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(300));
                if (GUILayout.Button("Options", EditorStyles.toolbarButton))
                {
                    isOptions = !isOptions;
                }

                if (GUILayout.Button("Build", EditorStyles.toolbarButton))
                {
                    OnBuild(DressupEditorOptions.instance.dressups.ToArray());
                }

                GUILayout.EndHorizontal();
            }
        }


        void DrawingOptions()
        {
            optionsScroll = GUILayout.BeginScrollView(optionsScroll);
            {
                EditorGUI.BeginChangeCheck();
                {
                    GUILayout.BeginVertical("Normal", EditorStyles.helpBox);
                    {
                        GUILayout.Space(20);
                        EditorGUILayout.PropertyField(options.FindProperty("normals"), true);
                        GUILayout.EndVertical();
                    }
                    GUILayout.BeginVertical("Element Options", EditorStyles.helpBox);
                    {
                        GUILayout.Space(20);
                        EditorGUILayout.PropertyField(options.FindProperty("options"), true);
                        GUILayout.EndVertical();
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        options.ApplyModifiedProperties();
                        DressupEditorOptions.Save();
                    }
                }
                GUILayout.EndScrollView();
            }
        }

        void DrawingElementList()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(5);
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(5);
                    GUILayout.BeginVertical("OL box NoExpand", GUILayout.Width(300), GUILayout.Height(position.height - 30));
                    {
                        listScroll = GUILayout.BeginScrollView(listScroll);
                        {
                            DrawingElementGroupList();
                            GUILayout.EndScrollView();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndVertical();
                }


                Rect contians = EditorGUILayout.BeginVertical();
                {
                    GUILayout.Space(5);
                    GUILayout.BeginVertical("OL box NoExpand", GUILayout.Width(position.width - 310), GUILayout.Height(position.height - 30));
                    {
                        manifestScroll = GUILayout.BeginScrollView(manifestScroll, false, true);
                        {
                            DrawingDressupElementDataList();
                            GUILayout.EndScrollView();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndVertical();
                }

                CheckMouseDragEvent(contians);
                CheckMouseDragdropEvent(contians);
                GUILayout.EndHorizontal();
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

            if (DressupEditorOptions.instance.options is null || DressupEditorOptions.instance.options.Count is 0)
            {
                EditorUtility.DisplayDialog("Tips", "还没有设置分组，请先设置分组！", "OK");
                return;
            }


            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                if (DragAndDrop.objectReferences[i] is Texture2D texture)
                {
                    DressupOptionsData data = new DressupOptionsData();
                    data.texture = texture;
                    data.element = selection;

                    string group = DressupEditorOptions.instance.options.First()?.name;
                    if (group.IsNullOrEmpty())
                    {
                        EditorUtility.DisplayDialog("Tips", "还没有设置分组名，请先设置分组名！", "OK");
                        return;
                    }

                    data.group = group;
                    DressupEditorOptions.instance.dressups.Add(data);
                }
            }

            DressupEditorOptions.Save();
            OnEnable();
        }


        private Element selection;

        private void DrawingElementGroupList()
        {
            for (int i = 1; i <= (int)Element.Eyebrow; i++)
            {
                Rect contians = EditorGUILayout.BeginVertical();
                {
                    Color back = GUI.color;
                    GUI.color = ((Element)i) == selection ? Color.cyan : back;
                    GUILayout.Label(((Element)i).ToString(), "LargeBoldLabel", GUILayout.Width(270));
                    GUI.color = back;
                    GUILayout.Space(5);
                    Color color = GUI.color;
                    GUI.color = ((Element)i) == selection ? new Color(1f, 0.92156863f, 0.015686275f, .5f) : new Color(0, 0, 0, .2f);
                    GUILayout.Box("", "WhiteBackground", GUILayout.Width(285), GUILayout.Height(1));
                    GUI.color = color;

                    if (contians.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
                    {
                        selection = ((Element)i);
                        this.Repaint();
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.Space(5);
            }
        }

        private void DrawingDressupElementDataList()
        {
            List<DressupOptionsData> list = DressupEditorOptions.instance.dressups.Where(x => x.element == selection).ToList();
            if (list is null || list.Count is 0)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                DressupOptionsData dressup = list[i];
                if (dressup.texture == null)
                {
                    DressupEditorOptions.instance.dressups.Remove(dressup);
                    this.Repaint();
                }

                if (dressup.element is Element.None)
                {
                    dressup.element = Element.Blush;
                    DressupEditorOptions.Save();
                }

                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        {
                            GUILayout.Space(5);
                            dressup.isOn = GUILayout.Toggle(dressup.isOn, "");
                            GUILayout.EndVertical();
                        }
                        string name = $"{dressup.texture.name} ({AssetDatabase.GetAssetPath(dressup.texture)})";
                        if (GUILayout.Button(name, "LargeBoldLabel"))
                        {
                            dressup.foldout = !dressup.foldout;
                            this.Repaint();
                        }

                        GUILayout.FlexibleSpace();
                        string[] arr = DressupEditorOptions.instance.options.Select(x => x.name).ToArray();
                        int index = DressupEditorOptions.instance.options.FindIndex(x => x.name == dressup.group);
                        GUILayout.Label("分组：", GUILayout.Width(50));
                        int temp = EditorGUILayout.Popup(index, arr, EditorStyles.toolbarDropDown);
                        if (temp != index)
                        {
                            dressup.group = arr[temp];
                            DressupEditorOptions.Save();
                        }

                        GUILayout.BeginVertical();
                        {
                            GUILayout.Space(5);
                            if (GUILayout.Button("", "PaneOptions"))
                            {
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Build"), false, () => { OnBuild(dressup); });
                                menu.AddItem(new GUIContent("Delete"), false, () => { DressupEditorOptions.instance.RemoveDressup(dressup); });
                                menu.ShowAsContext();
                            }

                            GUILayout.EndVertical();
                        }

                        GUILayout.EndHorizontal();
                    }
                    GUILayout.Space(5);
                    Color color = GUI.color;
                    GUI.color = new Color(0, 0, 0, .2f);
                    GUILayout.Box("", "WhiteBackground", GUILayout.Height(1));
                    GUI.color = color;
                    GUILayout.EndVertical();
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(23);
                    Texture2D icon = (Texture2D)EditorGUILayout.ObjectField("", dressup.icon, typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100));
                    if (icon != dressup.icon)
                    {
                        dressup.icon = icon;
                        DressupEditorOptions.Save();
                    }

                    GroupOptions groupOptions = DressupEditorOptions.instance.options.Find(x => x.name == dressup.group);
                    if (groupOptions is not null)
                    {
                        ElementOptions elementOptions = groupOptions.GetElement(dressup.element);
                        if (elementOptions is not null)
                        {
                            if (elementOptions.icon == null)
                            {
                                elementOptions.icon = GetPreviewTexture((GameObject)elementOptions.target);
                            }

                            dressup.icon = elementOptions.icon;
                        }
                    }

                    GUILayout.Label(dressup.texture, GUILayout.Width(100), GUILayout.Height(100));
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(5);
            }
        }

        private void OnBuild(params DressupOptionsData[] optionsDatas)
        {
            if (optionsDatas is null || optionsDatas.Length is 0)
            {
                EditorUtility.DisplayDialog("Tips", "Build Complete!", "OK");
                return;
            }

            if (DressupEditorOptions.instance.options is null || DressupEditorOptions.instance.options.Count is 0)
            {
                EditorUtility.DisplayDialog("Tips", "Not Set Element Group!", "OK");
                return;
            }

            List<BundleDependencis> builds = new List<BundleDependencis>();

            if (EditorUtility.DisplayDialog("Tips", "是否需要打包模型资源?", "是", "否"))
            {
                for (int i = 0; i < DressupEditorOptions.instance.options.Count; i++)
                {
                    GroupOptions options = DressupEditorOptions.instance.options[i];
                    if (builds.Find(x => x.target == options.skeleton) is null)
                    {
                        builds.Add(new BundleDependencis(options.skeleton));
                    }

                    if (options.elements is null || options.elements.Count is 0)
                    {
                        continue;
                    }

                    foreach (var VARIABLE in options.elements)
                    {
                        if (builds.Find(x => x.target == VARIABLE.target) is null)
                        {
                            builds.Add(new BundleDependencis(VARIABLE.target));
                        }
                    }
                }
            }

            if (builds.Count > 0)
            {
                string output = Application.dataPath + "/../output/assets";
                if (Directory.Exists(output) is false)
                {
                    Directory.CreateDirectory(output);
                }

                BuildPipeline.BuildAssetBundles(output, builds.Select(x => x.build).ToArray(), BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
                for (int i = 0; i < DressupEditorOptions.instance.options.Count; i++)
                {
                    GroupOptions options = DressupEditorOptions.instance.options[i];
                    string bundleName = $"{GetAssetPath(options.skeleton)}";
                    string targetPath = String.Empty;
                    if (File.Exists($"{output}/{bundleName}"))
                    {
                        targetPath = $"{Application.dataPath}/../output/{options.name}/bundles/{bundleName}";
                        if (Directory.Exists(Path.GetDirectoryName(targetPath)) is false)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        }

                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                        }

                        File.Copy($"{output}/{bundleName}", targetPath);
                    }

                    if (options.elements is null || options.elements.Count is 0)
                    {
                        continue;
                    }

                    foreach (var VARIABLE in options.elements)
                    {
                        bundleName = $"{GetAssetPath(VARIABLE.target)}";
                        targetPath = $"{Application.dataPath}/../output/{options.name}/bundles/{bundleName}";
                        if (Directory.Exists(Path.GetDirectoryName(targetPath)) is false)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        }

                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                        }

                        File.Copy($"{output}/{bundleName}", targetPath);
                    }
                }

                Directory.Delete(output, true);
            }

            Dictionary<string, List<OutDressupData>> outList = new Dictionary<string, List<OutDressupData>>();
            for (int i = 0; i < optionsDatas.Length; i++)
            {
                DressupOptionsData optionsData = optionsDatas[i];
                GroupOptions groupOptions = DressupEditorOptions.instance.options.Find(x => x.name == optionsData.group);
                if (groupOptions is null)
                {
                    EditorUtility.DisplayDialog("Errored", "没有找到对应的部件组配置！Element:" + optionsData.element + " Name:" + optionsData.texture.name, "Ok");
                    return;
                }

                ElementOptions elementOptions = groupOptions.GetElement(optionsData.element);
                if (elementOptions is null)
                {
                    EditorUtility.DisplayDialog("Errored", "没有找到对应的部件配置！Element:" + optionsData.element + " Name:" + optionsData.texture.name, "Ok");
                    return;
                }

                Texture2D texture2D = default;

                //todo 移动部件图片
                if (optionsData.texture == null)
                {
                    EditorUtility.DisplayDialog("Errored", "部件没有指定图片！Element:" + optionsData.element, "Ok");
                    return;
                }

                texture2D = new Texture2D(optionsData.texture.width, optionsData.texture.height, TextureFormat.RGBA32, false);
                texture2D.SetPixels(optionsData.texture.GetPixels());
                string texturePath = $"{Application.dataPath}/../output/{optionsData.group}/element/{GetAssetPath(optionsData.texture)}.png";
                if (Directory.Exists(Path.GetDirectoryName(texturePath)) is false)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(texturePath));
                }

                if (File.Exists(texturePath))
                {
                    File.Delete(texturePath);
                }

                File.WriteAllBytes(texturePath, texture2D.EncodeToPNG());

                //todo 移动部件ICON
                if (optionsData.icon == null)
                {
                    if (elementOptions.icon == null)
                    {
                        elementOptions.icon = GetPreviewTexture((GameObject)elementOptions.target);
                    }

                    optionsData.icon = elementOptions.icon;
                    if (optionsData.icon == null)
                    {
                        EditorUtility.DisplayDialog("Errored", "部件没有指定Icon！Element:" + optionsData.element + " Name:" + optionsData.texture.name, "Ok");
                        return;
                    }
                }

                texture2D = new Texture2D(optionsData.icon.width, optionsData.icon.height, TextureFormat.RGBA32, false);
                texture2D.SetPixels(optionsData.icon.GetPixels());
                string iconPath = $"{Application.dataPath}/../output/{optionsData.group}/icons/{GetAssetPath(optionsData.texture)}_icon.png";
                if (Directory.Exists(Path.GetDirectoryName(iconPath)) is false)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(iconPath));
                }

                if (File.Exists(iconPath))
                {
                    File.Delete(iconPath);
                }

                File.WriteAllBytes(iconPath, texture2D.EncodeToPNG());

                if (!outList.TryGetValue(optionsData.group, out List<OutDressupData> list))
                {
                    outList.Add(optionsData.group, list = new List<OutDressupData>());
                }


                list.Add(new OutDressupData
                {
                    icon = $"{optionsData.group}/icons/{GetAssetPath(optionsData.texture)}_icon.png",
                    is_normal = DressupEditorOptions.instance.normals.Contains(optionsData.element),
                    element = (int)optionsData.element,
                    group = optionsData.group,
                    model = $"{optionsData.group}/bundles/{GetAssetPath(elementOptions.target)}",
                    texture = optionsData.texture == null ? string.Empty : $"{optionsData.group}/element/{GetAssetPath(optionsData.texture)}.png",
                    version = 0,
                    crc = 0
                });
            }

            foreach (var VARIABLE in outList)
            {
                string jsonPath = Application.dataPath + "/../output/" + VARIABLE.Key + "/elements.json";
                File.WriteAllText(jsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(VARIABLE.Value));
            }

            InitConfig initConfig = new InitConfig();
            initConfig.groups = new List<GroupInfo>();
            foreach (var group in DressupEditorOptions.instance.options)
            {
                GroupInfo info = new GroupInfo();
                info.name = group.name;
                info.elements = new List<ElementInfo>();
                foreach (var element in group.elements)
                {
                    ElementInfo elementInfo = new ElementInfo();
                    elementInfo.element = element.element;
                    elementInfo.childs = new List<ChildInfo>();
                    if (element.childs is null)
                    {
                        continue;
                    }

                    foreach (var child in element.childs)
                    {
                        elementInfo.childs.Add(new ChildInfo()
                        {
                            element = child.element,
                            path = child.path
                        });
                    }

                    info.elements.Add(elementInfo);
                }

                initConfig.groups.Add(info);
            }

            initConfig.normal = DressupEditorOptions.instance.normals;
            File.WriteAllText(Application.dataPath + "/../output/avatar_setting.json", JsonConvert.SerializeObject(initConfig));
            EditorUtility.DisplayDialog("Tips", "Build Complete!", "OK");
        }

        private static string GetAssetPath(Object target)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target)).ToLower();
        }

        class BundleDependencis
        {
            public Object target;
            public AssetBundleBuild build;

            public BundleDependencis(Object target)
            {
                this.target = target;
                build = new AssetBundleBuild()
                {
                    assetBundleName = GetAssetPath(this.target),
                    assetNames = new[] { AssetDatabase.GetAssetPath(this.target) }
                };
            }
        }
    }
}