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
        private Texture2D _texture2D;

        public DressupManager dressup { get; private set; }
        public DressupData data { get; set; }
        public GameObject gameObject { get; private set; }
        public GameObject[] childs { get; private set; }

        public Texture2D texture => _texture2D;

        public DressupComponent(DressupManager dressup, GameObject gameObject)
        {
            this.dressup = dressup;
            this.gameObject = gameObject;
        }

        public DressupComponent(DressupManager manager, GameObject[] childs)
        {
            this.dressup = manager;
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

            yield return LoadAsync<Texture2D>(charge.texture, 0, 0, SetTexture2D);
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

        public void SetTexture2D(Texture texture)
        {
            _texture2D = texture as Texture2D;
            if (this.gameObject != null)
            {
                Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    // renderer.sharedMaterial.shader = Shader.Find("Universal Render Pipeline/Lit");
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
                        // renderer.sharedMaterial.shader = Shader.Find("Universal Render Pipeline/Lit");
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