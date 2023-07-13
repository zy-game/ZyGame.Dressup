using System.Collections.Generic;
using System.Text;
using System;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.Scripting;
using System.Security.Cryptography;

namespace ZyGame.Replacement
{
    public static class Ex
    {
        private static Mono mono;
        class Mono : MonoBehaviour
        {

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

        public static void ToCameraCenter(this GameObject gameObject)
        {
            Camera.main.ToViewCenter(gameObject);
        }



        public static void ToViewCenter(this Camera camera, GameObject gameObject)
        {
            if (camera == null || gameObject == null)
            {
                return;
            }
            var bound = gameObject.GetBoundingBox();
            var center = FocusCameraOnGameObject(camera, gameObject);
            camera.transform.localPosition = new Vector3(bound.center.x, bound.center.y, center.z);
            camera.transform.LookAt(bound.center, Vector3.up);
            camera.fieldOfView = 2.0f * Mathf.Atan(Mathf.Max(bound.size.y, bound.size.x) * 0.5f / Vector3.Distance(camera.transform.position, bound.center)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 获取物体包围盒
        /// </summary>
        /// <param name="obj">父物体</param>
        /// <returns>物体包围盒</returns>
        public static Bounds GetBoundingBox(this GameObject obj)
        {
            var bounds = new Bounds();
            if (obj != null)
            {
                var renders = obj.GetComponentsInChildren<Renderer>();
                if (renders != null)
                {
                    var boundscenter = Vector3.zero;
                    foreach (var item in renders)
                    {
                        boundscenter += item.bounds.center;
                    }

                    if (renders.Length > 0)
                        boundscenter /= renders.Length;
                    bounds = new Bounds(boundscenter, Vector3.zero);
                    foreach (var item in renders)
                    {
                        bounds.Encapsulate(item.bounds);
                    }
                }
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



        public static void SetRequestHeaders(this UnityWebRequest request, Dictionary<string, List<string>> headers)
        {
            if (request == null || headers == null || headers.Count <= 0)
            {
                return;
            }

            if (headers != null && headers.Count > 0)
            {
                foreach (var item in headers)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        request.SetRequestHeader(item.Key, item.Value[i]);
                    }
                }
            }
        }

        public class RequestCreateFileData
        {
            public string sid;
            public string name;
            public string md5;
            public int size;
            public string type;
            public string audit_status;

            [Preserve]
            public RequestCreateFileData()
            {
            }

            [Preserve]
            public RequestCreateFileData(string name, string md5, string type, string status, int size)
            {
                this.sid = "1";
                this.name = name;
                this.size = size;
                this.type = type;
                this.audit_status = status;
                this.md5 = md5;
            }
        }

        public class UploadData
        {
            public string id;
            public string sid;
            public string md5;
            public string name;
            public string type;
            public int size;
            public string audit_status;
            public string @object;
            public string url;
            [Preserve]
            public UploadData()
            {
            }
        }

        public class UploadAssetResponse
        {
            public int code;
            public string msg;
            public UploadData data;
            [Preserve]
            public UploadAssetResponse()
            {
            }
        }

        public class ResponseCreateFile
        {
            public int code;
            public string msg;
            public Data data;

            public UploadAssetResponse Generic()
            {
                return new UploadAssetResponse()
                {
                    code = 200,
                    msg = string.Empty,
                    data = new UploadData()
                    {
                        name = data.matter.name,
                        sid = data.matter.sid,
                        md5 = data.matter.md5,
                        type = data.matter.type,
                        url = data.matter.url,
                        size = data.matter.size,
                    }
                };
            }

            [Preserve]
            public ResponseCreateFile()
            {
            }
        }

        public class Data
        {
            public int code;
            public string msg;
            public Matter matter;
            public Dictionary<string, List<string>> headers;
            public string up_link;

            [Preserve]
            public Data()
            {
            }
        }

        public class Matter
        {
            public string id;
            public string sid;
            public string md5;
            public string name;
            public string type;
            public int size;
            public string audit_status;
            public string @object;
            public string url;

            [Preserve]
            public Matter()
            {
            }
        }

        public static IEnumerator UploadAsset(string address, string user, int pid, RequestCreateFileData requestCreate, byte[] bytes, Action<UploadAssetResponse, Exception> callback)
        {
            string postData = Newtonsoft.Json.JsonConvert.SerializeObject(requestCreate);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
            using UnityWebRequest request = UnityWebRequest.Post(address + "avatar/resource/v1/matter/create", postData);
            Debug.LogFormat("{0} {1}", request.url, postData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("userid", user);
            request.SetRequestHeader("pid", pid.ToString());
            yield return request.SendWebRequest();
            if (!request.isDone || request.result != UnityWebRequest.Result.Success)
            {
                callback(null, new Exception(request.error + "\n" + request.downloadHandler.text));
                yield break;
            }
            Debug.Log(request.downloadHandler.text);
            ResponseCreateFile responseCreateFile = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseCreateFile>(request.downloadHandler.text);
            if (responseCreateFile.code != 200)
            {
                callback(null, new Exception(responseCreateFile.msg));
                yield break;
            }
            if (string.IsNullOrEmpty(responseCreateFile.data.up_link))
            {
                callback(responseCreateFile.Generic(), null);
                yield break;
            }

            using var request1 = UnityWebRequest.Put(responseCreateFile.data.up_link, bytes);
            request1.SetRequestHeaders(responseCreateFile.data.headers);
            yield return request1.SendWebRequest();
            if (!request1.isDone || request1.result != UnityWebRequest.Result.Success)
            {
                callback(null, new Exception(request1.error + "\n" + request1.downloadHandler.text));
                yield break;
            }

            using var request2 = UnityWebRequest.Post(address + "avatar/resource/v1/matter/done?name=" + requestCreate.name, string.Empty);
            request2.SetRequestHeader("userid", user);
            request2.SetRequestHeader("pid", pid.ToString());
            yield return request2.SendWebRequest();
            if (!request2.isDone || request2.result != UnityWebRequest.Result.Success)
            {
                callback(null, new Exception(string.Format("{0}\n{1}\n{2}", request2.url, request2.error, request2.downloadHandler.text)));
            }
            else
            {
                Debug.Log(request2.downloadHandler.text);
                UploadAssetResponse response = Newtonsoft.Json.JsonConvert.DeserializeObject<UploadAssetResponse>(request2.downloadHandler.text);
                if (response.code != 200)
                {
                    callback(null, new Exception(response.msg));
                }
                else
                {
                    callback(response, null);
                }
            }
        }
        public enum PublishState : byte
        {
            None,
            Publish,
            Drafts,
            Process,
        }
        public static void UploadElementData(string address, string user, int pid, string id, string name, byte[] bytes2, GameObject gameObject, DressupData dressupData, PublishState state, Action<DressupData> onCompleted)
        {
            Executed().StartCoroutine();
            IEnumerator Executed()
            {
                Texture2D texture2D = Camera.main.Screenshot(256, 256, gameObject);
                byte[] iconDataBytes = texture2D.EncodeToPNG();
                RequestCreateFileData icon = new RequestCreateFileData(name + "_icon.png", iconDataBytes.GetMd5(), "image/png", "2", iconDataBytes.Length);
                UploadAssetResponse iconResponse = null;
                yield return Ex.UploadAsset(address, user, pid, icon, iconDataBytes, (response, exception) =>
                {
                    if (exception is not null)
                    {
                        onCompleted(null);
                        return;
                    }

                    iconResponse = response;
                });
                if (iconResponse == null)
                {
                    onCompleted(null);
                    yield break;
                }

                RequestCreateFileData drawingData = new RequestCreateFileData(name + ".png", bytes2.GetMd5(), "image/png", "2", bytes2.Length);

                yield return Ex.UploadAsset(address, user, pid, drawingData, bytes2, (response, exception) =>
                {
                    if (exception != null)
                    {
                        onCompleted(null);
                        return;
                    }

                    DressupData createElementData = new DressupData();
                    createElementData.name = name;
                    createElementData.id = id;
                    createElementData.texture = response.data.url;
                    createElementData.icon = iconResponse.data.url;
                    createElementData.model = dressupData.model;
                    createElementData.element = dressupData.element;
                    createElementData.model_name = dressupData.model_name;
                    createElementData.publish_status = (byte)state;
                    onCompleted(createElementData);
                });
            }
        }
    }
}