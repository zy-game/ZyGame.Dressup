namespace ZyGame.Drawing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    public sealed class DrawingData
    {
        private string _name;
        private bool isChange;
        private float _alpha;
        private Vector2 _position;
        private Texture2D _tempCache;
        private RenderTexture _texture;
        private Color[] colors;

        public bool isImport { get; private set; }

        public string name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (this._texture != null)
                {
                    this._texture.name = value;
                }

                if (this._tempCache != null)
                {
                    this._tempCache.name = value + "_cache";
                }
            }
        }

        public RenderTexture texture
        {
            get { return this._texture; }
        }

        public int width { get; private set; }

        public int height { get; private set; }

        public DrawingData(string name)
        {
            this.name = name;
        }

        public DrawingData(int width, int height, string name) : this(name)
        {
            this.width = width;
            this.height = height;
            this.isChange = false;
            this._tempCache = new Texture2D(width, height, TextureFormat.RGBA32, false);
            this._tempCache.name = name + "_cache";
            this._texture = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
            this._texture.name = name + "_render";
        }

        public void Import(byte[] bytes)
        {
            Texture2D texture = new Texture2D(this.width, this.height, TextureFormat.RGBA32, false);
            texture.name = " import" + bytes.GetMd5();
            texture.LoadImage(bytes);
            this.isImport = true;
            this.texture.DrawTexture(new Rect(0, 0, this.width, this.height), texture, null);
        }

        public void SetAlpha(float alpha)
        {
            if (colors is null || colors.Length is 0)
            {
                return;
            }
            this._alpha = alpha;
            RenderTexture.active = this._texture;
            this._tempCache.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            Color temp = Color.white;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color color = this.colors[j * width + i];
                    temp = this._tempCache.GetPixel(i, j);
                    if (color.r > 0 || color.g > 0 || color.b > 0)
                    {
                        temp = new Color(color.r, color.g, color.b, alpha);
                    }
                    this._tempCache.SetPixel(i, j, temp);
                }
            }

            this._tempCache.Apply();
            this._texture.Clear();
            this._texture.DrawTexture(new Rect(0, 0, width, height), this._tempCache, null);
        }

        public void SetColor(Vector2 vector, Color color)
        {
            var _x = (int)(vector.x * this.width);
            var _y = (int)(height - vector.y * height);
            Vector2 colorPos = new Vector2(_x, _y);
            //WriteColor(colorPos);
            //设置颜色有几种方式，如果点在透明的地方那就是填充透明，如果点在有颜色的地方，那就填充rgb一致的并且连起来的颜色
            RenderTexture.active = this._texture;
            this._tempCache.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            Color temp = Color.white;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    temp = this._tempCache.GetPixel(i, j);
                    temp.r = color.r;
                    temp.g = color.g;
                    temp.b = color.b;
                    this._tempCache.SetPixel(i, j, temp);
                }
            }

            this._tempCache.Apply();
            this._texture.Clear();
            this._texture.DrawTexture(new Rect(0, 0, width, height), this._tempCache, null);
        }
        private void WriteColor(Vector2 colorPos)
        {
            RenderTexture.active = this._texture;
            this._tempCache.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            //实际应用任何先前的 SetPixel 和 SetPixels 更改
            _tempCache.Apply();
        }

        public Color getColor(int x, int y)
        {
            //返回坐标 (x,y) 处的像素颜色
            return _tempCache.GetPixel(x, y);
        }
        public void setColor(int x, int y, Color c)
        {
            //设置坐标 (x,y) 处的像素颜色
            _tempCache.SetPixel(x, y, c);
        }

        public void Drawing(float x, float y)
        {
            var _x = (int)(x * this.width);
            var _y = (int)(height - y * height);
            Color temp = DrawingSetting.instance.material.color;
            DrawingSetting.instance.material.color = new Color(temp.r, temp.g, temp.b, _alpha);
            var _width = (int)DrawingSetting.instance.paintWidth;
            texture.DrawTexture(new Rect(_x - _width / 2, _y - _width / 2, _width, _width), DrawingSetting.instance.paintTexture, DrawingSetting.instance.material);
            this.isChange = true;
            _tempCache = texture.ReadTexture2D();
            colors = _tempCache.GetPixels();
            DrawingSetting.instance.material.color = temp;
        }

        public void Abrasion(Vector2 point)
        {
            var _x = (int)(point.x * this.width);
            var _y = (int)(height - point.y * height);
            var _width = (int)DrawingSetting.instance.paintWidth;
            texture.DrawTexture(new Rect(_x - _width / 2, _y - _width / 2, _width, _width), DrawingSetting.instance.paintTexture, DrawingSetting.instance.abrasionMaterial);
            this.isChange = true;
            colors = _tempCache.GetPixels();
        }

        public void OnDrawingCompleted()
        {

        }

        public void OnStartDrag(Vector2 point)
        {
            _position = point;
            _position.x *= this.width;
            _position.y *= this.height;
        }

        public void DragDrawing(Vector2 offset)
        {
            Vector2 pixelUV = new Vector2(offset.x * this.width, offset.y * this.height);
            offset = pixelUV - _position;
            _position = pixelUV;

            int length_x = (int)Math.Abs(offset.x);
            if (length_x != 0)
            {
                RenderTexture.active = this._texture;
                if (offset.x < 0) //→
                {
                    this._tempCache.ReadPixels(new Rect(0, 0, length_x, height), width - length_x, 0);
                    this._tempCache.ReadPixels(new Rect(length_x, 0, width - length_x, height), 0, 0);
                }
                else if (offset.x > 0) //←
                {
                    this._tempCache.ReadPixels(new Rect(width - length_x, 0, length_x, height), 0, 0);
                    this._tempCache.ReadPixels(new Rect(0, 0, width - length_x, height), length_x, 0);
                }
            }

            int length_y = (int)Math.Abs(offset.y);
            if (length_y != 0)
            {
                RenderTexture.active = this._texture;
                if (offset.y > 0) //↑
                {
                    this._tempCache.ReadPixels(new Rect(0, 0, width, length_y), 0, 0);
                    this._tempCache.ReadPixels(new Rect(0, length_y, width, height - length_y), 0, length_y);
                }
                else if (offset.y < 0) //↓
                {
                    this._tempCache.ReadPixels(new Rect(0, height - length_y, width, length_y), 0, height - length_y);
                    this._tempCache.ReadPixels(new Rect(0, 0, width, height - length_y), 0, 0);
                }
            }

            if (length_x != 0 || length_y != 0)
            {
                this._tempCache.Apply();
                this._texture.Clear();
                this._texture.DrawTexture(new Rect(0, 0, width, height), this._tempCache, null);
            }
        }
        Material material = new Material(Shader.Find("Unlit/Transparent"));
        internal void Resize(float size)
        {
            int newWidth = (int)(width * size);
            int newHeight = (int)(height * size);
            if (this.isChange == true)
            {
                material.name = "drawing texture";
                this._tempCache = this.texture.ReadTexture2D();
                this.isChange = false;
            }
            this.texture.Clear();
            material.mainTexture = this._tempCache;
            this.texture.DrawTexture(new Rect((width - newWidth) / 2, (height - newHeight) / 2, newWidth, newHeight), this._tempCache, material);
        }

        public static DrawingData GenerateToBinary(BinaryReader reader)
        {
            DrawingData layer = new(string.Empty);
            layer.name = reader.ReadString();
            layer.width = reader.ReadInt32();
            layer.height = reader.ReadInt32();
            layer.isImport = reader.ReadByte() == 0;
            int length = reader.ReadInt32();
            Color[] _colors = new Color[length];
            for (int i = 0; i < length; i++)
            {
                _colors[i] = reader.ReadColor();
            }

            layer._tempCache = new Texture2D(layer.width, layer.height, TextureFormat.RGBA32, false);
            layer._tempCache.name = layer.name + "_cache";
            layer._texture = new RenderTexture(layer.width, layer.height, 0, RenderTextureFormat.Default);
            layer._texture.name = layer.name + "_render";
            layer._tempCache.SetPixels(_colors);
            layer._tempCache.Apply();
            layer._texture.DrawTexture(new Rect(0, 0, layer.width, layer.height), layer._tempCache, null);

            return layer;
        }

        public static void Write(DrawingData data, BinaryWriter writer)
        {
            int length = data.width * data.height;
            writer.Write(data.name);
            writer.Write(data.width);
            writer.Write(data.height);
            writer.Write((byte)(data.isImport ? 0 : 1));
            writer.Write(length);
            data._tempCache = data.texture.ReadTexture2D();
            Color[] _colors = data._tempCache.GetPixels();
            for (int j = 0; j < length; j++)
            {
                writer.Write(_colors[j]);
            }
        }
    }
}