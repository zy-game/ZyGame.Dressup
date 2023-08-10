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
     public class DressupComponent : IDisposable
     {
         private Texture2D _texture2D;
    
         public DressupManager dressup { get; private set; }
         public DressupData data { get; set; }
         public GameObject gameObject { get; private set; }
         public GameObject[] childs { get; private set; }
    
         public Texture2D texture => _texture2D;
    
         public DressupComponent(DressupManager manager)
         {
             this.dressup = manager;
         }
    
         public void SetChild(GameObject gameObject)
         {
             this.gameObject = gameObject;
         }
    
         public void SetChild(GameObject[] childs)
         {
             
             this.childs = childs;
         }
    
         public IEnumerator DressupTexture(DressupData charge)
         {
             if (charge == null || string.IsNullOrEmpty(charge.texture))
             {
                 Debug.Log("Not set Texture Data:" + charge.element);
                 yield break;
             }
    
             if (this.gameObject is null && this.childs is null && this.childs.Length is 0)
             {
                 Debug.Log("Not Dressup GameObject:" + charge.element);
                 yield break;
             }
    
             if (charge.publish_status == 2)
             {
                 yield return CombineDrawingData(charge);
             }
             else
             {
                 yield return LoadAsync<Texture2D>(charge.texture, 0, 0, SetTexture2D);
             }
         }
    
         private IEnumerator LoadAsync<T>(string path, uint version, uint crc, Action<T> action) where T : Object
         {
             bool m = false;
             T result = null;
             dressup.AssetLoader.LoadAsync<T>(path, version, crc, args =>
             {
                 result = args;
                 m = true;
             });
             yield return new WaitUntil(() => m);
             action(result);
         }
    
         public static Texture2D Combines(List<DrawingData> layers, Texture2D org)
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
                         dressup.Notify(EventNames.ERROR_MESSAGE_NOTICE, string.Format(ErrorInfo.NOT_FIND_THE_ELEMENT_ASSET, temp.texture) + 4);
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
    
                     SetTexture2D(Combines(layers, args));
                 });
             }
         }
    
         public void SetTexture2D(Texture texture)
         {
             _texture2D = texture as Texture2D;
             if (this.gameObject != null)
             {
                 Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
                 if (renderer != null)
                 {
                     renderer.sharedMaterial.mainTexture = texture;
                     Debug.Log("[SET  TEXTURE]" + renderer.name + " -> " + texture.name);
                 }
             }
             else
             {
                 for (int i = 0; i < childs.Length; i++)
                 {
                     Renderer renderer = childs[i].GetComponentInChildren<Renderer>();
                     if (renderer != null)
                     {
                         Debug.Log("[SET CHILD TEXTURE]" + renderer.name + " -> " + texture.name);
                         renderer.sharedMaterial.mainTexture = texture;
                     }
                 }
             }
         }
    
    
         public SkinnedMeshRenderer[] GetSkinnedMeshRenderers()
         {
             List<SkinnedMeshRenderer> skinneds = new List<SkinnedMeshRenderer>();
             if (this.childs is not null && this.childs.Length > 0)
             {
                 for (int i = 0; i < this.childs.Length; i++)
                 {
                     skinneds.Add(this.childs[i].GetComponent<SkinnedMeshRenderer>());
                 }
             }
    
             if (gameObject is not null)
             {
                 skinneds.AddRange(this.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>());
             }
    
             return skinneds.ToArray();
         }
    
         public void SetActiveState(bool state)
         {
             if (gameObject == null)
             {
                 foreach (var VARIABLE in childs)
                 {
                     VARIABLE.SetActive(state);
                 }
    
                 return;
             }
    
             this.gameObject.SetActive(state);
         }
    
         public void Dispose()
         {
             if (gameObject != null)
             {
                 GameObject.DestroyImmediate(gameObject);
             }
    
             this.data = null;
             this.dressup = null;
             this.gameObject = null;
             this._texture2D = null;
             this.childs = Array.Empty<GameObject>();
         }
     }
}