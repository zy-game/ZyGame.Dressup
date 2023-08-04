using System;
using ZyGame.Drawing;
using UnityEngine;

namespace ZyGame.Drawing
{
    public class DrawingSetting
    {
        public int layerLimit = 5;
        public float paintWidth = 4;
        public PaintBrush paintBrush = PaintBrush.Pen;
        public Texture2D paintTexture;
        public Material material;
        public Material abrasionMaterial;

        private static DrawingSetting _instance;

        public static DrawingSetting instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new DrawingSetting();
                    _instance.Initialized();
                }

                return _instance;
            }
        }

        public void Initialized()
        {
            paintTexture = Resources.Load<Texture2D>("paint1");
            material = Resources.Load<Material>("Brush");
            abrasionMaterial = Resources.Load<Material>("Abrasion");
            SetPaintbrushColor(Color.white);
            SetBrushWidth(4);
            SetPaintBrush(PaintBrush.Pen);
        }

        public void SetPaintbrushColor(Color color)
        {
            material.color = color;
        }

        public void SetPaintBrush(PaintBrush brush)
        {
            this.paintBrush = brush;
        }

        public void SetBrushWidth(float width)
        {
            this.paintWidth = width;
        }

        public void SetBrushTexture(Texture2D texture)
        {
            this.paintTexture = texture;
        }
    }
}