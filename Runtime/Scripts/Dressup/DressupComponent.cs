using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZyGame.Dressup
{
    public class DressupComponent : IDisposable
    {
        public DressupManager dressup { get; private set; }
        public DressupData data { get; set; }
        public GameObject gameObject { get; private set; }
        public GameObject[] childs { get; private set; }

        public DressupComponent(DressupManager dressup)
        {
            this.dressup = dressup;
        }

        public IEnumerator DressupTextureAndGameObject(DressupData charge)
        {
            yield return DressupGameObject(charge);
            yield return DressupTexture(charge);
        }

        public IEnumerator DressupGameObject(DressupData charge)
        {
            if (charge == null || string.IsNullOrEmpty(charge.model))
            {
                yield break;
            }
            if (charge.model != data?.model && dressup.IsChild(charge.element) is false)
            {
                yield return LoadAsync<GameObject>(charge, args => SetGameObject(args, charge, true));
            }
        }

        public IEnumerable DressupTexture(DressupData charge)
        {
            if (charge == null || string.IsNullOrEmpty(charge.texture))
            {
                yield break;
            }
            if (this.gameObject is null || this.childs is null || this.childs.Length is 0)
            {
                yield break;
            }
            yield return LoadAsync<Texture2D>(charge, args => SetTexture2D(args, charge));

        }

        private IEnumerator LoadAsync<T>(DressupData dressupData, Action<T> action) where T : Object
        {
            bool m = false;
            T result = null;
            dressup.AssetLoader.LoadAsync<T>(dressupData.model, dressupData.version, dressupData.crc, args =>
            {
                result = args;
                m = true;
            });
            yield return new WaitUntil(() => m);
            action(result);
        }

        public void SetTexture2D(Texture2D texture, DressupData dressupData)
        {
            if (this.gameObject is not null)
            {
                Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial.mainTexture = texture;
                }
                Debug.Log("[SET CHILD TEXTURE]" + renderer.name + " -> " + dressupData.texture);
            }
            else
            {
                for (int i = 0; i < childs.Length; i++)
                {
                    Renderer renderer = childs[i].GetComponentInChildren<Renderer>();
                    Debug.Log("[SET CHILD TEXTURE]" + renderer.name + " -> " + dressupData.texture);
                    if (renderer != null)
                    {
                        renderer.sharedMaterial.mainTexture = texture;
                    }
                }
            }
        }

        public void SetGameObjectAsChild(params GameObject[] childs)
        {
            this.childs = childs;
            Debug.Log("[SET CHILD OBJECT]" + string.Join(",", childs.Select(x => x.name).ToArray()));
        }

        public void SetGameObject(GameObject gameObject, DressupData dressupData, bool isRoot)
        {
            if (this.gameObject is not null)
            {
                return;
            }
            this.gameObject = gameObject;
            if (isRoot is false)
            {
                return;
            }
            this.gameObject.SetParent(dressup.gameObject, Vector3.zero, Vector3.zero, Vector3.one);
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
            this.gameObject.SetActive(state);
        }

        public void Dispose()
        {
            GameObject.DestroyImmediate(gameObject);
            this.data = null;
            this.dressup = null;
            this.gameObject = null;
        }
    }
}