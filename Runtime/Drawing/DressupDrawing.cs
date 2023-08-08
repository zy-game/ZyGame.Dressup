using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ZyGame.Dressup;

namespace ZyGame.Drawing
{
    public enum PaintBrush
    {
        /// <summary>
        /// 钢笔
        /// </summary>
        Pen = 1,

        /// <summary>
        /// 刷子
        /// </summary>
        Brush = 2,

        /// <summary>
        /// 橡皮檫
        /// </summary>
        Rubber = 3,

        /// <summary>
        /// 拖拽
        /// </summary>
        Drag = 4,

        /// <summary>
        /// 油漆桶
        /// </summary>
        PaintBucket = 5,
    }

    public enum Changed
    {
        WaitSave,
        Saved,
    }

    class CacheData
    {
        public Texture2D next;
        public Texture2D prev;
        public RenderTexture target;
    }

    public class DressupDrawing : IDisposable
    {
        public Changed changed { get; set; } = Changed.Saved;
        public Element element { get; set; }
        public DrawingData current { get; set; }
        public Texture2D original { get; set; }
        public RenderTexture render { get; set; }
        public DressupComponent component { get; set; }
        public RenderTexture drawingRender { get; set; }
        public PaintBrush brush { get; set; }
        public GameObject gameObject => component.gameObject == null ? component.childs[0] : component.gameObject;
        public List<DrawingData> layers { get; set; } = new List<DrawingData>();

        private Vector3 last_mouse_position = Vector3.zero;
        private Action<string, object> Notify => _dressupManager.Notify;
        public string id { get; set; }

        /// <summary>
        /// 当前缓存
        /// </summary>
        private CacheData cache;

        private int current_index = 0;
        private List<CacheData> caches = new List<CacheData>();
        private DressupManager _dressupManager;


        public DressupDrawing(DressupManager dressupManager)
        {
            _dressupManager = dressupManager;
        }


        public void Forwad()
        {
            if (current_index >= caches.Count - 1)
            {
                return;
            }

            current_index++;
            CacheData cache = caches[current_index];
            cache.target.Clear();
            cache.target.DrawTexture(new Rect(0, 0, cache.next.width, cache.next.height), cache.next, null);
        }

        public void Backup()
        {
            if (current_index <= 0)
            {
                return;
            }

            current_index--;
            CacheData cache = caches[current_index];
            cache.target.Clear();
            cache.target.DrawTexture(new Rect(0, 0, cache.prev.width, cache.prev.height), cache.prev, null);
        }

        private void EnsureInitializedGraffiti()
        {
            if (this._dressupManager != null)
            {
                return;
            }

            _dressupManager.Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.NOT_INITIALIZED_DRAWING);
        }

        private void EnsureSelectionLayer()
        {
            if (current == null)
            {
                throw new Exception("not selection layer");
            }
        }


        /// <summary>
        /// 退出涂鸦
        /// </summary>
        // public void ExitGraffiti()
        // {
        //     if (this.drawing == null)
        //     {
        //         return;
        //     }
        //     
        //     this.drawing.Dispose();
        //     this.drawing = null;
        //     CameraCtrl.instance.Hide(ControllerState.Pen);
        //     CameraCtrl.instance.LockControllerType(ControllerState.None);
        // }

        /// <summary>
        /// 保存涂鸦数据
        /// </summary>
        /// <param name="name">文件名,如果文件为空则不保存数据</param>
        public void Save(string name)
        {
            this.EnsureInitializedGraffiti();
            Debug.Log("保存" + name);
            changed = Changed.Saved;
            if (name.IsNullOrEmpty())
            {
                Notify(EventNames.SAVED_GRAFFITI_DATA_COMPLETED, string.Empty);
                return;
            }

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((byte)element);
            writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(component.data));
            writer.Write((byte)layers.Count);
            for (int i = 0; i < layers.Count; i++)
            {
                DrawingData.Write(layers[i], writer);
            }

            string address = _dressupManager.address;
            string user = _dressupManager.user;
            int pid = _dressupManager.pid;
            API.UploadElementData(address, user, pid, id, name, stream.ToArray(), component.gameObject, component.data, API.PublishState.Drafts, args =>
            {
                if (args == null)
                {
                    Notify(EventNames.SAVED_GRAFFITI_DATA_COMPLETED, string.Empty);
                    return;
                }

                changed = Changed.Saved;
                Notify(EventNames.SAVED_GRAFFITI_DATA_COMPLETED, Newtonsoft.Json.JsonConvert.SerializeObject(args));
            });
            Debug.Log("通知保存完成" + name);
        }

        /// <summary>
        /// 发布涂鸦
        /// </summary>
        public void PublishGraffiti(string name)
        {
            this.EnsureInitializedGraffiti();
            RenderTexture.active = render;
            Texture2D publishTexture = new Texture2D(current.width, current.height, TextureFormat.ARGB32, false);
            publishTexture.ReadPixels(new Rect(0, 0, current.width, current.height), 0, 0);
            publishTexture.Apply();
            RenderTexture.active = null;
            string address = _dressupManager.address;
            string user = _dressupManager.user;
            int pid = _dressupManager.pid;
            API.UploadElementData(address, user, pid, id, name, publishTexture.EncodeToPNG(), component.gameObject, component.data, API.PublishState.Process, args =>
            {
                if (args == null)
                {
                    Notify(EventNames.PUBLISHING_GRAFFITI_COMPLETED, string.Empty);
                    return;
                }

                Notify(EventNames.PUBLISHING_GRAFFITI_COMPLETED, Newtonsoft.Json.JsonConvert.SerializeObject(args));
            });
        }

        /// <summary>
        /// 设置画笔
        /// </summary>
        /// <param name="brush">1:钢笔 2:刷子 3:橡皮檫 4:拖动</param>
        public void SetPaintbrushType(int brush)
        {
            this.EnsureInitializedGraffiti();
            DrawingSetting.instance.SetPaintBrush((PaintBrush)brush);
        }

        /// <summary>
        /// 设置画笔颜色
        /// </summary>
        /// <param name="hexadecimal">颜色值</param>
        public void SetPaintbrushColor(string hexadecimal)
        {
            this.EnsureInitializedGraffiti();
            DrawingSetting.instance.SetPaintbrushColor(hexadecimal.ToColor());
        }

        /// <summary>
        /// 设置画笔大小
        /// </summary>
        /// <param name="width"></param>
        public void SetPaintbrushWidth(float width)
        {
            this.EnsureInitializedGraffiti();
            DrawingSetting.instance.SetBrushWidth(width);
        }

        /// <summary>
        /// 在当前选中的图层中导入涂鸦图片
        /// </summary>
        public void ImportGraffitiTexture()
        {
            this.EnsureInitializedGraffiti();
            _dressupManager.LoadFileCompeltion += Runnable_OpenFileComplated;
            _dressupManager.OpenFileCallback();

            void Runnable_OpenFileComplated(byte[] args)
            {
                EnsureSelectionLayer();
                current.Import(args);
                Apply();
                Notify(EventNames.IMPORT_GRAFFITI_TEXTURE_COMPLETED, default);
            }
        }

        /// <summary>
        /// 新建图层
        /// </summary>
        /// <param name="name">图层名</param>
        public void NewLayer(string name)
        {
            this.EnsureInitializedGraffiti();
            if (layers.Count >= 5)
            {
                Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.LAYER_OUT_LIMIT_COUNT);
                return;
            }

            layers.Insert(0, new DrawingData(original.width, original.height, name));
            SelectionLayer(name);
            current.texture.Clear();
            Apply();
            Notify(EventNames.CREATE_LAYER_COMPLETED, current.name);
        }

        /// <summary>
        /// 选中图层
        /// </summary>
        /// <param name="name">要选中的图层名</param>
        public void SelectionLayer(string name)
        {
            this.EnsureInitializedGraffiti();
            DrawingData layer = layers.Find(x => x.name == name);
            if (layer == null)
            {
                Notify(EventNames.ERROR_MESSAGE_NOTICE, string.Format(ErrorInfo.NOT_FIND_TATH_LAYER, name));
                return;
            }

            current = layer;
            // CameraCtrl.instance.LockControllerType(ControllerState.Pen);
        }

        /// <summary>
        /// 删除选中图层
        /// </summary>
        public void DeleteLayer()
        {
            this.EnsureInitializedGraffiti();
            EnsureSelectionLayer();
            if (!layers.Contains(current))
            {
                return;
            }

            string name = current.name;
            layers.Remove(current);
            current = layers.LastOrDefault();
            Apply();
            Notify(EventNames.DELETE_LAYER_COMPLETED, name);
        }

        /// <summary>
        /// 设置选中图层透明度
        /// </summary>
        /// <param name="alpha">透明度 </param>
        public void SetLayerAlpha(float alpha)
        {
            this.EnsureInitializedGraffiti();
            EnsureSelectionLayer();
            current.SetAlpha(alpha);
            Apply();
        }

        /// <summary>
        /// 设置图层缩放大小
        /// </summary>
        /// <param name="size"></param>
        public void SetLayerSize(float size)
        {
            this.EnsureInitializedGraffiti();
            EnsureSelectionLayer();
            OnStartDrawing(default);
            current.Resize(size);
            OnDrawingCompleted();
            Apply();
        }

        /// <summary>
        /// 撤销
        /// </summary>
        /// <param name="isBackup">0:后退，1:前进</param>
        public void Undo(int isBackup)
        {
            this.EnsureInitializedGraffiti();
            if (isBackup == 0)
            {
                Backup();
            }
            else
            {
                Forwad();
            }

            Apply();
        }

        public void SetSort(int index)
        {
            this.EnsureInitializedGraffiti();
            if (current == null)
            {
                Notify(EventNames.ERROR_MESSAGE_NOTICE, ErrorInfo.NOT_FIND_TATH_LAYER);
                return;
            }

            if (index < 0 || index >= layers.Count)
            {
                Notify(EventNames.ERROR_MESSAGE_NOTICE, "ArgumentOutOfRangeException");
                return;
            }

            layers.Remove(current);
            layers.Insert(index, current);
            Apply();
            Notify(EventNames.SORT_LAYER_SUCCESSFLY, "");
        }


        public void OnUpdate()
        {
            // if (current == null || brush != ControllerState.Pen)
            // {
            //     return;
            // }

            void CheckMouseButtonDown()
            {
                if (!Input.GetMouseButtonDown(0))
                {
                    return;
                }

                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                {
                    OnStartDrawing(hit);
                }
            }

            void CheckMouseButtonUp()
            {
                if (!Input.GetMouseButtonUp(0))
                {
                    return;
                }

                OnDrawingCompleted();
            }

            void CheckMouseButtonDrag()
            {
                if (!Input.GetMouseButton(0))
                {
                    return;
                }

                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                {
                    OnBeginDarwing(hit);
                }
            }

            CheckMouseButtonDown();
            CheckMouseButtonUp();
            CheckMouseButtonDrag();
        }

        /// <summary>
        /// 绘画
        /// </summary>
        /// <param name="hit"></param>
        private void OnBeginDarwing(RaycastHit hit)
        {
            EnsureSelectionLayer();

            this.changed = Changed.WaitSave;
            if (DrawingSetting.instance.paintBrush == PaintBrush.Drag)
            {
                current.DragDrawing(hit.textureCoord);
                Apply();
                return;
            }

            if (!current.isImport)
            {
                switch (DrawingSetting.instance.paintBrush)
                {
                    case PaintBrush.Rubber:
                        current.Abrasion(hit.textureCoord);
                        break;
                    case PaintBrush.PaintBucket:
                        current.SetColor(hit.textureCoord, DrawingSetting.instance.material.color);
                        break;
                    case PaintBrush.Pen:
                    case PaintBrush.Brush:
                        current.Drawing(hit.textureCoord.x, hit.textureCoord.y);
                        break;
                }

                Apply();
            }
        }


        private void OnStartDrawing(RaycastHit hit)
        {
            if (DrawingSetting.instance.paintBrush == PaintBrush.Drag)
            {
                current.OnStartDrag(hit.textureCoord);
            }

            last_mouse_position = hit.textureCoord;

            Start(current.texture);
        }

        private void OnDrawingCompleted()
        {
            Ended(current.texture);
            current.OnDrawingCompleted();
        }

        public void Start(RenderTexture texture)
        {
            if (cache != null)
            {
                return;
            }

            if (current_index < caches.Count - 1)
            {
                for (int i = caches.Count - 1; i > current_index; i--)
                {
                    caches.Remove(caches[i]);
                }
            }

            cache = new CacheData();
            cache.target = texture;
            cache.prev = texture.ReadTexture2D();
        }

        public void Ended(RenderTexture texture)
        {
            if (cache == null)
            {
                return;
            }

            cache.next = texture.ReadTexture2D();
            caches.Add(cache);
            if (caches.Count > 10)
            {
                caches.Remove(caches.First());
            }

            cache = null;
            current_index = caches.Count - 1;
        }

        public void Apply()
        {
            render.Clear();
            render.DrawTexture(new Rect(0, 0, original.width, original.height), original, null);
            for (int i = 0; i < layers.Count; i++)
            {
                render.DrawTexture(new Rect(0, 0, layers[i].width, layers[i].height), layers[i].texture, null);
            }
        }

        public void Dispose()
        {
            // Client.Tools.RemoveCallback(OnUpdate);
            this.gameObject.DestroyMeshCollider();
            this.component.SetTexture2D(original);
            _dressupManager.EnableElement((int)Element.None);
        }
    }
}