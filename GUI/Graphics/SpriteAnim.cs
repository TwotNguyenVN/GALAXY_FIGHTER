using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GUI.GraphicsCore
{
    /// <summary>
    /// Animation từ spritesheet (khung đều nhau).
    /// </summary>
    public class SpriteAnim
    {
        public Image Sheet { get; private set; }
        public int FrameW { get; private set; }
        public int FrameH { get; private set; }
        public int FrameCount { get; private set; }
        public float Fps { get; set; }
        public bool Loop { get; set; }

        private float _time;
        public int FrameIndex { get; private set; }
        public bool Finished { get; private set; }

        public SpriteAnim(Image sheet, int frameW, int frameH, int frameCount, float fps = 12f, bool loop = true)
        {
            Sheet = sheet;
            FrameW = frameW;
            FrameH = frameH;
            FrameCount = frameCount;
            Fps = fps;
            Loop = loop;
            _time = 0f;
            FrameIndex = 0;
            Finished = false;
        }

        public void Reset()
        {
            _time = 0f;
            FrameIndex = 0;
            Finished = false;
        }

        public void Update(float dt)
        {
            if (Finished) return;

            _time += dt;
            int next = (int)(_time * Fps);

            if (Loop)
            {
                FrameIndex = (next) % FrameCount;
            }
            else
            {
                FrameIndex = next;
                if (FrameIndex >= FrameCount - 1)
                {
                    FrameIndex = FrameCount - 1;
                    Finished = true;
                }
            }
        }

        public void Draw(System.Drawing.Graphics g, float x, float y, float scale, float alpha, float rotationDeg)
        {
            var dst = new RectangleF(x, y, FrameW * scale, FrameH * scale);
            DrawInternal(g, dst, alpha, rotationDeg, false);
        }

        public void DrawCentered(System.Drawing.Graphics g, float cx, float cy, float scale, float alpha, float rotationDeg)
        {
            var dst = new RectangleF(cx - FrameW * scale / 2f, cy - FrameH * scale / 2f, FrameW * scale, FrameH * scale);
            DrawInternal(g, dst, alpha, rotationDeg, true);
        }

        private void DrawInternal(System.Drawing.Graphics g, RectangleF dst, float alpha, float rotationDeg, bool centerOrigin)
        {
            if (alpha <= 0f) return;
            if (alpha > 1f) alpha = 1f;

            int cols = Sheet.Width / FrameW;
            int fi = FrameIndex < 0 ? 0 : (FrameIndex >= FrameCount ? FrameCount - 1 : FrameIndex);

            var src = new Rectangle(
                (fi % cols) * FrameW,
                (fi / cols) * FrameH,
                FrameW, FrameH
            );

            ImageAttributes attrs = null;
            if (alpha < 1f)
            {
                attrs = new ImageAttributes();
                var cm = new ColorMatrix();
                cm.Matrix33 = alpha;
                attrs.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            }

            var state = g.Save();
            if (rotationDeg != 0f || centerOrigin)
            {
                float cx = dst.X + dst.Width / 2f;
                float cy = dst.Y + dst.Height / 2f;

                g.TranslateTransform(cx, cy, MatrixOrder.Prepend);
                if (rotationDeg != 0f) g.RotateTransform(rotationDeg, MatrixOrder.Prepend);
                g.TranslateTransform(-cx, -cy, MatrixOrder.Prepend);
            }

            g.DrawImage(Sheet, Rectangle.Round(dst), src.X, src.Y, src.Width, src.Height, GraphicsUnit.Pixel, attrs);

            g.Restore(state);
            if (attrs != null) attrs.Dispose();
        }
    }
}
