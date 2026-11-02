using System;
using System.Drawing;
using System.Windows.Forms;

namespace lab9
{
    public partial class Form1 : Form
    {
        private Bitmap drawingBitmap;
        private Graphics graphics;
        private Point? lastPoint = null;
        private PictureBox pictureBox1;

        public Form1()
        {
            InitializeComponent();

            // 1. PictureBox-ийг үүсгэх
            pictureBox1 = new PictureBox();
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.BackColor = Color.White;

            // 2. MouseClick эвентийг ЭНД МЕХАНИКААР холбож өгнө
            pictureBox1.MouseClick += PictureBox1_MouseClick;

            this.Controls.Add(pictureBox1);

            // 3. Зураг зурах Bitmap үүсгэх
            drawingBitmap = new Bitmap(1024, 768);
            graphics = Graphics.FromImage(drawingBitmap);
            graphics.Clear(Color.White);
            pictureBox1.Image = drawingBitmap;
        }

        private void PictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            // Эхний цэгийг дарах үед
            if (lastPoint == null)
            {
                lastPoint = e.Location;

                // Цэг дээр улаан дугуй зурж харуулах
                using (Pen pen = new Pen(Color.Red, 4))
                {
                    graphics.DrawEllipse(pen, e.X - 2, e.Y - 2, 4, 4);
                }
            }
            else
            {
                // Өмнөх цэгээс одоогийн цэг хүртэл хар шугам татах
                using (Pen pen = new Pen(Color.Black, 2))
                {
                    graphics.DrawLine(pen, lastPoint.Value, e.Location);
                }

                // Дараагийн холболтонд зориулж сүүлийн цэгийг шинэчлэх
                lastPoint = e.Location;
            }

            // Дэлгэцийн зургийг шинэчлэх
            pictureBox1.Invalidate();
        }
    }
}