using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace Lab4Variant1
{
    

    // Класс MainWindow.xaml.cs
    public partial class MainWindow : Window
    {
        private bool _modeB = false; // Режим работы
        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            _modeB = !_modeB;
        }

        // Класс Layer (Слой)
        public class Layer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public IShape Shape { get; set; }
            public bool IsVisible { get; set; } = true;
           
            public Layer(int id, string name, IShape shape)
            {
                Id = id;
                Name = name;
                Shape = shape;
            }
        }

        // Интерфейс IShape (Геометрические фигуры)
        public interface IShape
        {
            IShape Clip(Rect clipRect);
            IShape DetermineVisiblePart(List<IShape> blockingShapes);
            void Rasterize(WriteableBitmap bitmap, bool modeB);
            List<IShape> GetVisibleFragments(); // Получение фрагментов для видимости
        
        }

        // Класс Triangle (Треугольник)
        public class Triangle : IShape
        {
            public Point[] Vertices { get; set; }
            public Color FillColor { get; set; }
            public Color BorderColor { get; set; }
            public List<IShape> VisibleFragments { get; private set; } = new List<IShape>();

            public Triangle(Point v1, Point v2, Point v3, Color fillColor, Color borderColor)
            {
                Vertices = new[] { v1, v2, v3 };
                FillColor = fillColor;
                BorderColor = borderColor;
            }

            public IShape Clip(Rect clipRect)
            {
                var clippedVertices = SutherlandHodgmanClip(Vertices, clipRect);
                return new Triangle(clippedVertices[0], clippedVertices[1], clippedVertices[2], FillColor, BorderColor);
            }

            public IShape DetermineVisiblePart(List<IShape> blockingShapes)
            {
                // Начинаем с полной фигуры
                var remainingVisible = new List<IShape> { this };

                // Проходим по каждой блокирующей фигуре
                foreach (var blockingShape in blockingShapes)
                {
                    var updatedVisible = new List<IShape>();
                    foreach (var fragment in remainingVisible)
                    {
                        if (blockingShape is Triangle blockingTriangle)
                        {
                            // Отсечь блокирующим треугольником
                            updatedVisible.AddRange(ClipTriangleWithBlocking(fragment as Triangle, blockingTriangle));
                        }
                    }
                    remainingVisible = updatedVisible;
                }

                VisibleFragments = remainingVisible;
                return this;
            }
            public List<IShape> GetVisibleFragments()
            {
                return VisibleFragments;
            }

            private List<IShape> ClipTriangleWithBlocking(Triangle target, Triangle blocking)
            {
                // Реализация логики отсечения треугольника другим треугольником
                // Вернуть список оставшихся фрагментов
                return new List<IShape> { target }; // Псевдокод
            }

            public void Rasterize(WriteableBitmap bitmap, bool modeB)
            {
                foreach (var fragment in VisibleFragments)
                {
                 (fragment as Triangle)?.DrawTriangle(bitmap, FillColor , modeB); 
                }
            }


            private void DrawTriangle(WriteableBitmap bitmap, Color color,bool modeB)
            {
                if (modeB)
                {
                    var (success, newCoordinates) = LiangBarsky.ClipLine(
                        Vertices[0].X, Vertices[0].Y, Vertices[1].X, Vertices[1].Y,
                        xmin: 0, ymin: 0, xmax: bitmap.Width - 1, ymax: bitmap.Height - 1
                        );
                    Console.WriteLine(new Point(newCoordinates.newX0, newCoordinates.newY0).ToString());
                    Console.WriteLine(new Point(newCoordinates.newX1, newCoordinates.newY1).ToString());

                    DrawLine(new Point(newCoordinates.newX0, newCoordinates.newY0), new Point(newCoordinates.newX1, newCoordinates.newY1), bitmap, color);


                    (success, newCoordinates) = LiangBarsky.ClipLine(
                        Vertices[1].X, Vertices[1].Y, Vertices[2].X, Vertices[2].Y,
                        xmin: 0, ymin: 0, xmax: bitmap.Width - 1, ymax: bitmap.Height - 1
                        );
                    DrawLine(new Point(newCoordinates.newX0, newCoordinates.newY0), new Point(newCoordinates.newX1, newCoordinates.newY1), bitmap, color);


                    (success, newCoordinates) = LiangBarsky.ClipLine(
                        Vertices[2].X, Vertices[2].Y, Vertices[0].X, Vertices[0].Y,
                        xmin: 0, ymin: 0, xmax: bitmap.Width - 1, ymax: bitmap.Height - 1
                        );
                    DrawLine(new Point(newCoordinates.newX0, newCoordinates.newY0), new Point(newCoordinates.newX1, newCoordinates.newY1), bitmap, color);
                    FillTriangle(bitmap, color);
                }
                else
                {
                    DrawLine(Vertices[0], Vertices[1],bitmap, Colors.Red);
                    DrawLine(Vertices[1], Vertices[2],bitmap, Colors.Red);
                    DrawLine(Vertices[2], Vertices[0],bitmap, Colors.Red);
                }
               
            }


            private void DrawLine(Point start, Point end, WriteableBitmap bitmap, Color color)
            {
                // Округляем координаты до ближайших целых чисел
                int x0 = (int)Math.Round(start.X);
                int y0 = (int)Math.Round(start.Y);
                int x1 = (int)Math.Round(end.X);
                int y1 = (int)Math.Round(end.Y);

                int dx = Math.Abs(x1 - x0);
                int dy = Math.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1;
                int sy = y0 < y1 ? 1 : -1;
                int err = dx - dy;

                int maxSteps = Math.Max(dx, dy) + 1; // Ограничение на количество шагов

                while (maxSteps-- > 0) // Защита от бесконечного цикла
                {
                    SetPixel(bitmap, x0, y0, color);

                    if (x0 == x1 && y0 == y1)
                        break;

                    int e2 = 2 * err;
                    if (e2 > -dy)
                    {
                        err -= dy;
                        x0 += sx;
                    }
                    if (e2 < dx)
                    {
                        err += dx;
                        y0 += sy;
                    }
                }
            }

            private void SetPixel(WriteableBitmap bitmap, int x, int y, Color color)
            {
                if (x >= 0 && y >= 0 && x < bitmap.PixelWidth && y < bitmap.PixelHeight)
                {
                    byte r = color.R;
                    byte g = color.G;
                    byte b = color.B;
                    byte a = color.A;

                    bitmap.WritePixels(
                        new Int32Rect(x, y, 1, 1),
                        new[] { (int)(a << 24 | r << 16 | g << 8 | b) },
                        4,
                        0
                    );
                }
                    
            }
            private void CheckIntersection(Point p1, Point p2, int y, List<int> intersections)
            {
                // Если линия пересекает строку y, то находим точку пересечения
                if ((p1.Y <= y && p2.Y > y) || (p1.Y > y && p2.Y <= y))
                {
                    // Находим X-координату пересечения
                    int x = (int)(p1.X + (y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y));
                    intersections.Add(x);
                }
            }
            private void FillTriangle(WriteableBitmap bitmap, Color color)
            {
                // Вычисляем минимальные и максимальные значения Y для треугольника
                int minY = (int)Vertices.Min(v => v.Y);
                int maxY = (int)Vertices.Max(v => v.Y);

                // Ограничиваем минимум и максимум по Y в пределах изображения
                minY = Math.Max(minY, 0);
                maxY = Math.Min(maxY, bitmap.PixelHeight - 1);

                // Проходим по каждой горизонтальной линии (scanline)
                for (int y = minY; y <= maxY; y++)
                {
                    // Находим все пересечения этой строки с треугольником
                    List<int> intersections = new List<int>();

                    // Проверяем пересечения с каждой стороной треугольника
                    CheckIntersection(Vertices[0], Vertices[1], y, intersections);
                    CheckIntersection(Vertices[1], Vertices[2], y, intersections);
                    CheckIntersection(Vertices[2], Vertices[0], y, intersections);

                    // Если пересечений больше одного, то сортируем их
                    intersections.Sort();

                    // Закрашиваем все пиксели между найденными пересечениями
                    for (int i = 0; i < intersections.Count; i += 2)
                    {
                        int xStart = intersections[i];
                        int xEnd = intersections[i + 1];

                        for (int x = xStart; x <= xEnd; x++)
                        {
                            // Проверяем, что пиксель в пределах изображения
                            if (x >= 0 && x < bitmap.PixelWidth)
                            {
                                SetPixel(bitmap, x, y, color);
                            }
                        }
                    }
                }
            }
           

            private bool PointInTriangle(Point pt)
            {
                double areaOrig = Area(Vertices[0], Vertices[1], Vertices[2]);
                double area1 = Area(pt, Vertices[1], Vertices[2]);
                double area2 = Area(Vertices[0], pt, Vertices[2]);
                double area3 = Area(Vertices[0], Vertices[1], pt);

                return (areaOrig == area1 + area2 + area3);
            }

            private double Area(Point p1, Point p2, Point p3)
            {
                return Math.Abs((p1.X * (p2.Y - p3.Y) + p2.X * (p3.Y - p1.Y) + p3.X * (p1.Y - p2.Y)) / 2.0);
            }

            private Point[] SutherlandHodgmanClip(Point[] vertices, Rect clipRect)
            {
                return vertices; // Псевдокод
            }
        }
        // Класс для реализации алгоритма Лианга-Барски
        public class LiangBarsky
        {
            // Функция для отсечения одного отрезка
            public static (bool success, (double newX0, double newY0, double newX1, double newY1)) ClipLine(
                double x0, double y0, double x1, double y1,
                double xmin, double ymin, double xmax, double ymax)
            {
                double t0 = 0.0, t1 = 1.0;
                double dx = x1 - x0, dy = y1 - y0;

                double[] p = { -dx, dx, -dy, dy };
                double[] q = { x0 - xmin, xmax - x0, y0 - ymin, ymax - y0 };

                for (int i = 0; i < 4; i++)
                {
                    if (p[i] == 0)
                    {
                        if (q[i] < 0)
                        {
                            return (false, (x0, y0, x1, y1)); // Линия полностью вне области
                        }
                    }
                    else
                    {
                        double r = q[i] / p[i];
                        if (p[i] < 0)
                        {
                            t0 = Math.Max(t0, r); // Входной интервал
                        }
                        else
                        {
                            t1 = Math.Min(t1, r); // Выходной интервал
                        }
                    }
                }

                if (t0 > t1)
                {
                    return (false, (x0, y0, x1, y1)); // Линия полностью вне области
                }

                // Рассчитываем новые координаты
                double newX0 = x0 + t0 * dx;
                double newY0 = y0 + t0 * dy;
                double newX1 = x0 + t1 * dx;
                double newY1 = y0 + t1 * dy;

                return (true, (newX0, newY0, newX1, newY1));
            }
        }


        // Класс LayerContainer (Контейнер слоёв)
        public class LayerContainer
        {
            public void UpdateLayersWithVisibility(WriteableBitmap bitmap, bool modeB)
            {
                Rect clipRect = new Rect(30, 30, bitmap.PixelWidth - 400, bitmap.PixelHeight - 400);

                foreach (var layer in Layers)
                {
                    if (layer.IsVisible)
                    {
                        var blockingShapes = GetBlockingShapes(layer.Id);
                        layer.Shape.Clip(clipRect); // Отсечение по кадру
                        layer.Shape.DetermineVisiblePart(blockingShapes); // Определение видимых частей
                    }
                }
            }

            public void RasterizeAllLayers(WriteableBitmap bitmap, bool modeB)
            {
                foreach (var layer in Layers)
                {
                    if (layer.IsVisible)
                    {
                        layer.Shape.Rasterize(bitmap, modeB);
                    }
                }
            }

            public List<Layer> Layers { get; private set; } = new List<Layer>();
            
            public void AddLayer(Layer layer)
            {
                Layers.Add(layer);
            }
            //public void Shift(Layer layer)
            //{
            //    foreach (var lyer in Layers)
            //    {
            //        if (lyer.Shape is Triangle triangle)
            //        {
            //            lyer.Shape.ShiftX();
            //            lyer.Shape.ShiftY();
            //        }
            //    }
            //}
            public async void UpdateLayersWithClipping(WriteableBitmap bitmap, bool _modeB)
            {
                Rect clipRect = new Rect(0, 0, bitmap.PixelWidth - 1, bitmap.PixelHeight - 1);

                foreach (var layer in Layers)
                {
                    if (layer.IsVisible && layer.Shape is Triangle triangle)
                    {
                        layer.Shape.Rasterize(bitmap, _modeB);
                        await Task.Delay(1000);
                    }
                }
            }

            
            public List<IShape> GetBlockingShapes(int layerId)
            {
                return Layers.Where(l => l.Id < layerId && l.IsVisible).Select(l => l.Shape).ToList();
            }
        }
        private LayerContainer _layerContainer;
        private double _scale = 1.0;
        private Point _shift = new Point(0, 0);

        public MainWindow()
        {
            InitializeComponent();
            _layerContainer = new LayerContainer();
        }
        // Обработчик изменения масштаба
        private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Обновляем масштаб на основе значения слайдера
            _scale = ScaleSlider.Value;

            // Обновляем текстовое поле с текущим масштабом
            ScaleValueText.Text = $"Масштаб: {(int)(_scale * 100)}%";

            // Применяем новый масштаб
            ApplyTransform(DrawingCanvas);
        }
       
        private void SetPixel(WriteableBitmap bitmap, int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < bitmap.PixelWidth && y < bitmap.PixelHeight)
            {
                byte r = color.R;
                byte g = color.G;
                byte b = color.B;
                byte a = color.A;

                bitmap.WritePixels(
                    new Int32Rect(x, y, 1, 1),
                    new[] { (int)(a << 24 | r << 16 | g << 8 | b) },
                    4,
                    0
                );
            }

        }
        private void AddLayerButton_Click(object sender, RoutedEventArgs e)
        {
            var triangle = new Triangle(
                new Point(90, 50),
                new Point(100, 50),
                new Point(100, 150),
                Colors.Blue, Colors.Black);
            
            var triangle2 = new Triangle(
                new Point(100, 50),
                new Point(150, 50),
                new Point(200, 150),
                Colors.Red, Colors.Black
                );

            var triangle3 = new Triangle(
              new Point(-300, 50),
              new Point(100, 250),
              new Point(300, 150),
              Colors.Yellow, Colors.Black);

            var triangle4 = new Triangle(
             new Point(-40, 20),
             new Point(-40, 400),
             new Point(800, 300),
             Colors.Violet, Colors.Black);

            var triangle5 = new Triangle(
            new Point(200, 200),
            new Point(100, 500),
            new Point(800, 800),
            Colors.Pink, Colors.Black);

            _layerContainer.AddLayer(new Layer(_layerContainer.Layers.Count, $"Layer {_layerContainer.Layers.Count}", triangle));
            _layerContainer.AddLayer(new Layer(_layerContainer.Layers.Count, $"Layer {_layerContainer.Layers.Count}", triangle2));
            _layerContainer.AddLayer(new Layer(_layerContainer.Layers.Count, $"Layer {_layerContainer.Layers.Count}", triangle3));
            _layerContainer.AddLayer(new Layer(_layerContainer.Layers.Count, $"Layer {_layerContainer.Layers.Count}", triangle4));
            _layerContainer.AddLayer(new Layer(_layerContainer.Layers.Count, $"Layer {_layerContainer.Layers.Count}", triangle5));


        }
        private void DrawLine(WriteableBitmap bitmap, int x1, int y1, int x2, int y2, Color color)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetPixel(bitmap, x1, y1, color);

                if (x1 == x2 && y1 == y2) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }
        private void DrawRectangle(WriteableBitmap bitmap, int x1, int y1, int x2, int y2, Color color)
        {
            // Рисуем верхнюю горизонтальную линию
            DrawLine(bitmap, x1, y1, x2, y1, color);

            // Рисуем нижнюю горизонтальную линию
            DrawLine(bitmap, x1, y2, x2, y2, color);

            // Рисуем левую вертикальную линию
            DrawLine(bitmap, x1, y1, x1, y2, color);

            // Рисуем правую вертикальную линию
            DrawLine(bitmap, x2, y1, x2, y2, color);
        }
        private void DrawGrid(WriteableBitmap bitmap, int gridSize = 10, Color gridColor = default)
        {
            // Если цвет не задан, используем стандартный
            if (gridColor == default)
            {
                gridColor = Colors.LightGray;
            }

            // Рисуем вертикальные линии
            for (int x = 0; x < bitmap.PixelWidth; x += gridSize)
            {
                DrawLine(bitmap, x, 0, x, bitmap.PixelHeight - 1, gridColor);
            }

            // Рисуем горизонтальные линии
            for (int y = 0; y < bitmap.PixelHeight; y += gridSize)
            {
                DrawLine(bitmap, 0, y, bitmap.PixelWidth - 1, y, gridColor);
            }
        }

        private void DrawAxesAndFrame(WriteableBitmap bitmap)
        {
            // Рисуем рамку (границу) изображения
            DrawRectangle(bitmap, 0, 0, bitmap.PixelWidth - 1, bitmap.PixelHeight - 1, Colors.Black);
        }
       

        private void ScaleUpButton_Click(object sender, RoutedEventArgs e)
        {
            _scale += 0.1;
            ApplyTransform(DrawingCanvas);
        }

        private void ScaleDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (_scale > 0.1)
            {
                _scale -= 0.1;
                ApplyTransform(DrawingCanvas);
            }
        }

        private void ShiftLeftButton_Click(object sender, RoutedEventArgs e)
        {
            _shift.X -= 10;
            ApplyTransform(DrawingCanvas);
        }

        private void ShiftRightButton_Click(object sender, RoutedEventArgs e)
        {
            _shift.X += 10;
            ApplyTransform(DrawingCanvas);
        }

        private void ShiftUpButton_Click(object sender, RoutedEventArgs e)
        {
            _shift.Y -= 10;
            ApplyTransform(DrawingCanvas);
        }

        private void ShiftDownButton_Click(object sender, RoutedEventArgs e)
        {
            _shift.Y += 10;
            ApplyTransform(DrawingCanvas);
        }

        private void ApplyTransform(Canvas canvas)
        {
            canvas.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
            {
                new ScaleTransform(_scale, _scale),
                
            }
            };
        }
        private void ClearBitmap(WriteableBitmap bitmap, Color clearColor)
        {
            bitmap.Lock();
            unsafe
            {
                int pixel = clearColor.A << 24 | clearColor.R << 16 | clearColor.G << 8 | clearColor.B;
                int* pBackBuffer = (int*)bitmap.BackBuffer;

                for (int i = 0; i < bitmap.PixelWidth * bitmap.PixelHeight; i++)
                    pBackBuffer[i] = pixel;
            }
            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();
        }
        private  void RenderButton_Click(object sender, RoutedEventArgs e)
        {
            var _virtualBitmap = new WriteableBitmap((int)DrawingCanvas.Width , (int)DrawingCanvas.Height , 2, 2, PixelFormats.Pbgra32, null);
            ClearBitmap(_virtualBitmap, Colors.White);

            // Обновляем слои с отсечением
            _layerContainer.UpdateLayersWithVisibility(_virtualBitmap, _modeB);
            _layerContainer.RasterizeAllLayers(_virtualBitmap, _modeB);
            DrawAxesAndFrame(_virtualBitmap);
            DrawGrid(_virtualBitmap, gridSize: 2, gridColor: Colors.LightGray);
            DrawingCanvas.Background = new ImageBrush(_virtualBitmap);
        }
        private async void RenderSequentiallyButton_Click(object sender, RoutedEventArgs e)
        {
            var bitmap = new WriteableBitmap((int)DrawingCanvas.Width , (int)DrawingCanvas.Height , 2, 2, PixelFormats.Pbgra32, null);
            DrawAxesAndFrame(bitmap);
            // Обновляем слои с отсечением
            _layerContainer.UpdateLayersWithClipping(bitmap, _modeB);
            foreach (var layer in _layerContainer.Layers)
            {
                if (layer.IsVisible && layer.Shape is Triangle triangle)
                {
                    var visiblePart = layer.Shape.DetermineVisiblePart(_layerContainer.GetBlockingShapes(layer.Id));
                    visiblePart.Rasterize(bitmap,_modeB);
                    DrawingCanvas.Background = new ImageBrush(bitmap);
                    await Task.Delay(1000);
                }
            }
            DrawGrid(bitmap, gridSize: 2, gridColor: Colors.LightGray);

        }

        private void ShowGridButton_Click(object sender, RoutedEventArgs e)
        {
           

        }
    }


}