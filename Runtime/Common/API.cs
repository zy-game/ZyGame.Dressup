using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;

namespace ZyGame.Dressup
{
    public static class API
    {
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

        public static IEnumerator Download(string url, Action<byte[]> complete)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
            if (request.isDone is false || request.result is not UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
                yield break;
            }

            complete?.Invoke(request.downloadHandler.data);
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

            /// <summary>
            /// 已发布
            /// </summary>
            Publish,

            /// <summary>
            /// 绘制中
            /// </summary>
            Drafts,

            /// <summary>
            /// 审核中
            /// </summary>
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
                yield return UploadAsset(address, user, pid, icon, iconDataBytes, (response, exception) =>
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

                yield return UploadAsset(address, user, pid, drawingData, bytes2, (response, exception) =>
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