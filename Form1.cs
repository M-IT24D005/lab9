
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace lab9
{
    public partial class Form1 : Form
    {
        private PictureBox canvas;
        private Bitmap drawingBitmap;

        private Color selectedColor = Color.HotPink;
        private int brushSize = 5;
        private string currentTool = "Харандаа";

        private bool isDrawing = false;
        private Point startPoint;
        private Point lastPoint;
        private Bitmap previewBitmap;

        private Stack<Bitmap> undoStack = new Stack<Bitmap>();
        private Stack<Bitmap> redoStack = new Stack<Bitmap>();

        private Label lblSize;
        private Button btnColor;

        public Form1()
        {
            InitializeComponent();
            CreateInterface();
        }

        private void CreateInterface()
        {
            Text = "Cute Mini Paint Studio";
            Size = new Size(1100, 750);
            MinimumSize = new Size(850, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(255, 242, 248);

            // Дээд талын гарчиг
            Label title = new Label();
            title.Text = "♡ CUTE MINI PAINT STUDIO ♡";
            title.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            title.ForeColor = Color.DeepPink;
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Dock = DockStyle.Top;
            title.Height = 55;
            Controls.Add(title);

            // Зүүн талын хэрэгслийн самбар
            FlowLayoutPanel sidebar = new FlowLayoutPanel();
            sidebar.Dock = DockStyle.Left;
            sidebar.Width = 185;
            sidebar.Padding = new Padding(10);
            sidebar.FlowDirection = FlowDirection.TopDown;
            sidebar.WrapContents = false;
            sidebar.AutoScroll = true;
            sidebar.BackColor = Color.FromArgb(255, 224, 237);
            Controls.Add(sidebar);

            AddHeading(sidebar, "ЗУРАХ ХЭРЭГСЭЛ");

            ComboBox cmbTools = new ComboBox();
            cmbTools.Width = 155;
            cmbTools.DropDownStyle = ComboBoxStyle.DropDownList;

            cmbTools.Items.AddRange(new object[]
            {
                "Харандаа",
                "Шугам",
                "Дөрвөлжин",
                "Дугуй",
                "Зүрх",
                "Од",
                "Цэцэг",
                "Инээмсэглэл",
                "Баллуур"
            });

            cmbTools.SelectedIndex = 0;
            cmbTools.SelectedIndexChanged += (s, e) =>
            {
                currentTool = cmbTools.SelectedItem.ToString();
            };
            sidebar.Controls.Add(cmbTools);

            AddHeading(sidebar, "ӨНГӨ СОНГОХ");

            // Бэлэн өнгөнүүд
            FlowLayoutPanel colors = new FlowLayoutPanel();
            colors.Width = 160;
            colors.Height = 105;

            Color[] palette =
            {
                Color.Black, Color.Red, Color.Orange,
                Color.Gold, Color.LimeGreen, Color.DodgerBlue,
                Color.MediumPurple, Color.HotPink,
                Color.Brown, Color.White,
                Color.Turquoise, Color.LightPink
            };

            foreach (Color c in palette)
            {
                Button colorButton = new Button();
                colorButton.Size = new Size(31, 29);
                colorButton.BackColor = c;
                colorButton.FlatStyle = FlatStyle.Flat;
                colorButton.FlatAppearance.BorderColor = Color.Gray;

                colorButton.Click += (s, e) =>
                {
                    selectedColor = c;
                    btnColor.BackColor = c;
                    currentTool = "Харандаа";
                    cmbTools.SelectedItem = "Харандаа";
                };

                colors.Controls.Add(colorButton);
            }
            sidebar.Controls.Add(colors);

            btnColor = AddButton(sidebar, "🎨 Өөр өнгө сонгох");

            btnColor.Click += (s, e) =>
            {
                using (ColorDialog dialog = new ColorDialog())
                {
                    dialog.Color = selectedColor;
                    dialog.FullOpen = true;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        selectedColor = dialog.Color;
                        btnColor.BackColor = selectedColor;
                    }
                }
            };

            AddHeading(sidebar, "ЗУРААСНЫ ЗУЗААН");

            lblSize = new Label();
            lblSize.Text = "Зузаан: 5 px";
            lblSize.Width = 155;
            sidebar.Controls.Add(lblSize);

            TrackBar thickness = new TrackBar();
            thickness.Minimum = 1;
            thickness.Maximum = 25;
            thickness.Value = 5;
            thickness.Width = 155;
            thickness.TickFrequency = 5;

            thickness.ValueChanged += (s, e) =>
            {
                brushSize = thickness.Value;
                lblSize.Text = "Зузаан: " + brushSize + " px";
            };

            sidebar.Controls.Add(thickness);

            AddHeading(sidebar, "ЗАСВАРЛАХ");

            Button eraser = AddButton(sidebar, "🧽 Баллуур");
            eraser.Click += (s, e) =>
            {
                currentTool = "Баллуур";
                cmbTools.SelectedItem = "Баллуур";
            };

            Button undo = AddButton(sidebar, "↶ Буцаах");
            undo.Click += (s, e) => Undo();

            Button redo = AddButton(sidebar, "↷ Дахин хийх");
            redo.Click += (s, e) => Redo();

            Button clear = AddButton(sidebar, "🗑 Цэвэрлэх");
            clear.Click += (s, e) =>
            {
                SaveUndo();
                using (Graphics g = Graphics.FromImage(drawingBitmap))
                {
                    g.Clear(Color.White);
                }
                canvas.Image = drawingBitmap;
                canvas.Invalidate();
            };

            Button save = AddButton(sidebar, "💾 Хадгалах");
            save.Click += (s, e) => SaveImage();

            // Зураг зурах талбай
            Panel canvasPanel = new Panel();
            canvasPanel.Dock = DockStyle.Fill;
            canvasPanel.Padding = new Padding(15);
            canvasPanel.BackColor = Color.FromArgb(255, 242, 248);
            Controls.Add(canvasPanel);

            // Dock байрлалыг зөв тогтоох
            canvasPanel.BringToFront();
            sidebar.BringToFront();
            title.BringToFront();

            canvas = new PictureBox();
            canvas.Dock = DockStyle.Fill;
            canvas.BackColor = Color.White;
            canvas.BorderStyle = BorderStyle.FixedSingle;
            canvas.SizeMode = PictureBoxSizeMode.Normal;
            canvasPanel.Controls.Add(canvas);

            // Зургийн хэмжээ
            drawingBitmap = new Bitmap(1600, 1100);

            using (Graphics g = Graphics.FromImage(drawingBitmap))
            {
                g.Clear(Color.White);
            }

            canvas.Image = drawingBitmap;

            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
        }

        private void AddHeading(FlowLayoutPanel panel, string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            label.ForeColor = Color.MediumVioletRed;
            label.Width = 155;
            label.Height = 28;
            label.TextAlign = ContentAlignment.BottomLeft;
            panel.Controls.Add(label);
        }

        private Button AddButton(FlowLayoutPanel panel, string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 155;
            button.Height = 37;
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(90, 40, 70);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.LightPink;
            button.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            button.Margin = new Padding(3, 4, 3, 4);

            panel.Controls.Add(button);
            return button;
        }

        private void SaveUndo()
        {
            undoStack.Push((Bitmap)drawingBitmap.Clone());

            // Хэт их санах ой эзлэхээс сэргийлэх
            if (undoStack.Count > 20)
            {
                Bitmap[] items = undoStack.ToArray();
                undoStack.Clear();

                for (int i = Math.Min(19, items.Length - 1); i >= 0; i--)
                    undoStack.Push(items[i]);

                for (int i = 20; i < items.Length; i++)
                    items[i].Dispose();
            }

            foreach (Bitmap bmp in redoStack)
                bmp.Dispose();

            redoStack.Clear();
        }

        private void Undo()
        {
            if (undoStack.Count == 0) return;

            redoStack.Push((Bitmap)drawingBitmap.Clone());
            Bitmap old = drawingBitmap;
            drawingBitmap = undoStack.Pop();
            canvas.Image = drawingBitmap;
            old.Dispose();
            canvas.Invalidate();
        }

        private void Redo()
        {
            if (redoStack.Count == 0) return;

            undoStack.Push((Bitmap)drawingBitmap.Clone());
            Bitmap old = drawingBitmap;
            drawingBitmap = redoStack.Pop();
            canvas.Image = drawingBitmap;
            old.Dispose();
            canvas.Invalidate();
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (e.X < 0 || e.Y < 0 ||
                e.X >= drawingBitmap.Width ||
                e.Y >= drawingBitmap.Height) return;

            SaveUndo();

            isDrawing = true;
            startPoint = e.Location;
            lastPoint = e.Location;

            previewBitmap = (Bitmap)drawingBitmap.Clone();

            if (currentTool == "Харандаа" ||
                currentTool == "Баллуур")
            {
                DrawStroke(e.Location, e.Location);
                canvas.Invalidate();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;

            if (currentTool == "Харандаа" ||
                currentTool == "Баллуур")
            {
                DrawStroke(lastPoint, e.Location);
                lastPoint = e.Location;
            }
            else
            {
                using (Graphics g = Graphics.FromImage(drawingBitmap))
                {
                    g.DrawImageUnscaled(previewBitmap, 0, 0);
                }

                DrawShape(startPoint, e.Location);
            }

            canvas.Invalidate();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;

            if (currentTool == "Харандаа" ||
                currentTool == "Баллуур")
            {
                DrawStroke(lastPoint, e.Location);
            }
            else
            {
                using (Graphics g = Graphics.FromImage(drawingBitmap))
                {
                    g.DrawImageUnscaled(previewBitmap, 0, 0);
                }

                DrawShape(startPoint, e.Location);
            }

            isDrawing = false;

            if (previewBitmap != null)
            {
                previewBitmap.Dispose();
                previewBitmap = null;
            }

            canvas.Invalidate();
        }

        private void DrawStroke(Point a, Point b)
        {
            using (Graphics g = Graphics.FromImage(drawingBitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color color = currentTool == "Баллуур"
                    ? Color.White : selectedColor;

                int size = currentTool == "Баллуур"
                    ? brushSize * 3 : brushSize;

                using (Pen pen = new Pen(color, size))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLine(pen, a, b);
                }
            }
        }

        private void DrawShape(Point a, Point b)
        {
            using (Graphics g = Graphics.FromImage(drawingBitmap))
            using (Pen pen = new Pen(selectedColor, brushSize))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                int x = Math.Min(a.X, b.X);
                int y = Math.Min(a.Y, b.Y);
                int w = Math.Abs(a.X - b.X);
                int h = Math.Abs(a.Y - b.Y);

                switch (currentTool)
                {
                    case "Шугам":
                        g.DrawLine(pen, a, b);
                        break;

                    case "Дөрвөлжин":
                        g.DrawRectangle(pen, x, y, w, h);
                        break;

                    case "Дугуй":
                        g.DrawEllipse(pen, x, y, w, h);
                        break;

                    case "Зүрх":
                        DrawHeart(g, pen, x, y, w, h);
                        break;

                    case "Од":
                        DrawStar(g, pen, x, y, w, h);
                        break;

                    case "Цэцэг":
                        DrawFlower(g, pen, x, y, w, h);
                        break;

                    case "Инээмсэглэл":
                        DrawSmiley(g, pen, x, y, w, h);
                        break;
                }
            }
        }

        private void DrawHeart(Graphics g, Pen pen,
            int x, int y, int w, int h)
        {
            if (w < 5 || h < 5) return;

            PointF[] points = new PointF[101];

            for (int i = 0; i <= 100; i++)
            {
                double t = i * Math.PI * 2 / 100;

                double hx = 16 * Math.Pow(Math.Sin(t), 3);
                double hy = 13 * Math.Cos(t)
                    - 5 * Math.Cos(2 * t)
                    - 2 * Math.Cos(3 * t)
                    - Math.Cos(4 * t);

                points[i] = new PointF(
                    x + w / 2f + (float)(hx * w / 34),
                    y + h / 2f - (float)(hy * h / 34));
            }

            g.DrawLines(pen, points);
        }

        private void DrawStar(Graphics g, Pen pen,
            int x, int y, int w, int h)
        {
            if (w < 5 || h < 5) return;

            PointF[] points = new PointF[10];

            for (int i = 0; i < 10; i++)
            {
                double angle = -Math.PI / 2 + i * Math.PI / 5;
                double radius = i % 2 == 0 ? 1 : 0.45;

                points[i] = new PointF(
                    x + w / 2f + (float)(Math.Cos(angle) * w / 2 * radius),
                    y + h / 2f + (float)(Math.Sin(angle) * h / 2 * radius));
            }

            g.DrawPolygon(pen, points);
        }

        private void DrawFlower(Graphics g, Pen pen,
            int x, int y, int w, int h)
        {
            if (w < 10 || h < 10) return;

            float cx = x + w / 2f;
            float cy = y + h / 2f;

            for (int i = 0; i < 6; i++)
            {
                double angle = i * Math.PI / 3;

                float px = cx + (float)Math.Cos(angle) * w * 0.23f;
                float py = cy + (float)Math.Sin(angle) * h * 0.23f;

                g.DrawEllipse(pen,
                    px - w * 0.14f, py - h * 0.14f,
                    w * 0.28f, h * 0.28f);
            }

            g.DrawEllipse(pen,
                cx - w * 0.1f, cy - h * 0.1f,
                w * 0.2f, h * 0.2f);
        }

        private void DrawSmiley(Graphics g, Pen pen,
            int x, int y, int w, int h)
        {
            if (w < 10 || h < 10) return;

            g.DrawEllipse(pen, x, y, w, h);

            using (Brush brush = new SolidBrush(selectedColor))
            {
                float eyeW = Math.Max(2, w * 0.07f);
                float eyeH = Math.Max(2, h * 0.07f);

                g.FillEllipse(brush,
                    x + w * 0.28f, y + h * 0.32f,
                    eyeW, eyeH);

                g.FillEllipse(brush,
                    x + w * 0.65f, y + h * 0.32f,
                    eyeW, eyeH);
            }

            g.DrawArc(pen,
                x + w * 0.2f, y + h * 0.25f,
                w * 0.6f, h * 0.55f,
                20, 140);
        }

        private void SaveImage()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG Image|*.png";
                dialog.FileName = "MiniPaint_Zurag.png";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    drawingBitmap.Save(
                        dialog.FileName, ImageFormat.Png);

                    MessageBox.Show(
                        "Зураг амжилттай хадгалагдлаа!",
                        "Mini Paint",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (previewBitmap != null)
                previewBitmap.Dispose();

            foreach (Bitmap bmp in undoStack)
                bmp.Dispose();

            foreach (Bitmap bmp in redoStack)
                bmp.Dispose();

            if (canvas != null)
                canvas.Image = null;

            if (drawingBitmap != null)ssssss
                drawingBitmap.Dispose();

            base.OnFormClosed(e);
        }
    }
}
