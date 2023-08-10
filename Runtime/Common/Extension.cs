using System.Text;
using System;
using UnityEngine;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace ZyGame
{
    public static class Extension
    {
        private static Mono mono;

        class Mono : MonoBehaviour
        {
        }

        public static Color ToColor(this string hex)
        {
            hex = hex.Replace("0x", string.Empty);
            hex = hex.Replace("#", string.Empty);
            hex = hex.Replace("O", "0");
            hex = hex.Replace("o", "0");
            byte a = byte.MaxValue;
            byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
            }

            return new Color32(r, g, b, a);
        }

        public static Color ReadColor(this BinaryReader reader)
        {
            Color color = Color.white;
            color.r = reader.ReadSingle();
            color.g = reader.ReadSingle();
            color.b = reader.ReadSingle();
            color.a = reader.ReadSingle();
            return color;
        }

        public static void Write(this BinaryWriter writer, Color color)
        {
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }

        public static void DrawTexture(this RenderTexture renderTexture, Rect rect, Texture texture, Material material)
        {
            RenderTexture.active = renderTexture;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);
            if (material != null)
            {
                Graphics.DrawTexture(rect, texture, material);
            }
            else
            {
                Graphics.DrawTexture(rect, texture);
            }

            GL.PopMatrix();
            RenderTexture.active = null;
        }

        public static Texture2D ReadTexture2D(this RenderTexture texture)
        {
            RenderTexture.active = texture;
            Texture2D prev = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            prev.name = texture.name + "_" + Guid.NewGuid().ToString();
            prev.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            prev.Apply();
            RenderTexture.active = null;
            return prev;
        }

        public static void Clear(this RenderTexture renderTexture)
        {
            RenderTexture.active = renderTexture;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);
            GL.Clear(false, true, Color.clear);
            GL.PopMatrix();
            RenderTexture.active = null;
        }

        public static void ResetMaterialShader(this GameObject gameObject, string fail = "Unlit/Texture")
        {
            Renderer[] renderer = gameObject.GetComponentsInChildren<Renderer>();
            if (renderer == null)
            {
                return;
            }

            for (int i = 0; i < renderer.Length; i++)
            {
                Shader shader = Shader.Find(renderer[i].sharedMaterial.shader.name);
                if (shader == null)
                {
                    shader = Shader.Find(fail);
                }

                renderer[i].sharedMaterial.shader = shader;
            }
        }

        public static MeshCollider GenericMeshCollider(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return default;
            }

            SkinnedMeshRenderer skinned = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            MeshCollider collider = null;
            if (skinned == null)
            {
                Renderer renderer = gameObject.GetComponentInChildren<MeshRenderer>();
                collider = renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = renderer.GetComponentInChildren<MeshFilter>().sharedMesh;
                gameObject.SetActive(true);
            }
            else
            {
                collider = skinned.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = skinned.sharedMesh;
            }

            return collider;
        }

        public static void DestroyMeshCollider(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            MeshCollider[] colliders = gameObject.GetComponentsInChildren<MeshCollider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                GameObject.DestroyImmediate(colliders[i]);
            }
        }


        private static void EnsureMonoIsCreate()
        {
            if (mono is not null)
            {
                return;
            }

            mono = new GameObject("Mono").AddComponent<Mono>();
        }

        public static void StartCoroutine(this IEnumerator enumerator)
        {
            EnsureMonoIsCreate();
            mono.StartCoroutine(enumerator);
        }

        public static void StartCoroutine(this IEnumerator enumerator, Action action)
        {
            EnsureMonoIsCreate();

            IEnumerator Waiting()
            {
                yield return enumerator;
                action?.Invoke();
            }

            mono.StartCoroutine(Waiting());
        }

        public static void StartCoroutine<T>(this IEnumerator enumerator, Action<T> action, T args)
        {
            EnsureMonoIsCreate();

            IEnumerator Waiting()
            {
                yield return enumerator;
                action?.Invoke(args);
            }

            mono.StartCoroutine(Waiting());
        }

        public static void StartCoroutine<T, T2>(this IEnumerator enumerator, Action<T, T2> action, T args, T2 args2)
        {
            EnsureMonoIsCreate();

            IEnumerator Waiting()
            {
                yield return enumerator;
                action?.Invoke(args, args2);
            }

            mono.StartCoroutine(Waiting());
        }

        public static bool IsNullOrEmpty(this string target)
        {
            return string.IsNullOrEmpty(target);
        }

        public static string GetMd5(this byte[] bytes)
        {
            try
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("md5file() fail, error:" + ex.Message);
            }
        }

        public static GameObject SetParent(this GameObject gameObject, GameObject parent, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            if (gameObject == null)
            {
                return default;
            }

            if (parent != null)
            {
                gameObject.transform.SetParent(parent.transform);
            }

            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = Quaternion.Euler(rotation);
            gameObject.transform.localScale = scale;
            return gameObject;
        }

        public static Vector3 ToCameraCenter(this GameObject gameObject)
        {
            return Camera.main.ToViewCenter(gameObject);
        }


        public static Vector3 ToViewCenter(this Camera camera, GameObject gameObject)
        {
            if (camera == null || gameObject == null)
            {
                return Vector3.zero;
            }


            Bounds meshBound = GetBoundingBox(gameObject); //renderer.sharedMesh.bounds;
            // renderer.sharedMesh.RecalculateBounds();
            camera.transform.position = new Vector3(0, meshBound.center.y, 2);
            float distance = Vector3.Distance(camera.transform.position, meshBound.center);
            camera.fieldOfView = 2.0f * Mathf.Atan(Mathf.Max(meshBound.size.y, meshBound.size.x, meshBound.size.z) * 0.55f / distance) * Mathf.Rad2Deg;
            return new Vector3(0, meshBound.center.y, 2);
        }

        /// <summary>
        /// 获取物体包围盒
        /// </summary>
        /// <param name="obj">父物体</param>
        /// <returns>物体包围盒</returns>
        public static Bounds GetBoundingBox(this GameObject obj)
        {
            var bounds = new Bounds();
            if (obj == null)
            {
                return bounds;
            }

            var renders = obj.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (renders == null || renders.Length is 0)
            {
                return bounds;
            }

            var boundscenter = Vector3.zero;
            foreach (var item in renders)
            {
                item.sharedMesh.RecalculateBounds();
                boundscenter += item.bounds.center;
            }

            if (renders.Length > 0)
                boundscenter /= renders.Length;
            bounds = new Bounds(boundscenter, Vector3.zero);
            foreach (var item in renders)
            {
                bounds.Encapsulate(item.bounds);
            }

            return bounds;
        }

        public static Vector3 FocusCameraOnGameObject(Camera c, GameObject go)
        {
            Bounds b = GetBoundingBox(go);
            Vector3 max = b.size;
            // Get the radius of a sphere circumscribing the bounds
            float radius = max.magnitude / 2f;
            // Get the horizontal FOV, since it may be the limiting of the two FOVs to properly encapsulate the objects
            float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(c.fieldOfView * Mathf.Deg2Rad / 2f) * c.aspect) * Mathf.Rad2Deg;
            // Use the smaller FOV as it limits what would get cut off by the frustum        
            float fov = Mathf.Min(c.fieldOfView, horizontalFOV);
            float dist = radius / (Mathf.Sin(fov * Mathf.Deg2Rad / 2f)) + 0.4f;
            c.transform.localPosition = new Vector3(c.transform.localPosition.x, c.transform.localPosition.y, -dist);
            if (c.orthographic)
                c.orthographicSize = radius;

            var pos = new Vector3(c.transform.localPosition.x, c.transform.localPosition.y, dist);
            return pos;
        }

        public static Texture2D Screenshot(this Camera camera, int width, int height, GameObject gameObject)
        {
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            Vector3 scale = camera.transform.localScale;
            float view = camera.fieldOfView;

            camera.ToViewCenter(gameObject);
            RenderTexture renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
            camera.targetTexture = renderTexture;
            RenderTexture.active = camera.targetTexture;
            camera.Render();
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.name = gameObject.name.Replace("(Clone)", "");
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            camera.transform.position = position;
            camera.transform.rotation = rotation;
            camera.transform.localScale = scale;
            camera.fieldOfView = view;
            return texture;
        }
    }
}