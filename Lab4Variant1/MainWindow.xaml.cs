using System;
using System.Collections.Generic;
using System.Linq;
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
            // Добавление метода для получения ограничивающего прямоугольника
        }

        // Класс Triangle (Треугольник)
        public class Triangle : IShape
        {
            public Point[] Vertices { get; set; }
            public Color FillColor { get; set; }
            public Color BorderColor { get; set; }

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
                IShape visiblePart = this;
               
                return visiblePart;
            }

            public void Rasterize(WriteableBitmap bitmap, bool modeB)
            {
                // Отрисовываем контуры и заполняем треугольник
                DrawTriangle(bitmap, modeB ? Colors.Red : FillColor);
            }

            private void DrawTriangle(WriteableBitmap bitmap, Color color)
            {
                // Отрисовываем стороны треугольника
                DrawLine(Vertices[0], Vertices[1], bitmap, color);
                Console.WriteLine(Vertices[0].ToString());
                DrawLine(Vertices[1], Vertices[2], bitmap, color);
                DrawLine(Vertices[2], Vertices[0], bitmap, color);
                // Заполнение треугольника
                FillTriangle(bitmap, color);
            }
            

            private void DrawLine(Point start, Point end, WriteableBitmap bitmap, Color color)
            {
                int dx = Math.Abs((int)(end.X - start.X));
                int dy = Math.Abs((int)(end.Y - start.Y));
                int sx = start.X < end.X ? 1 : -1;
                int sy = start.Y < end.Y ? 1 : -1;
                int err = dx - dy;

                while (true)
                {
                    SetPixel(bitmap, (int)start.X, (int)start.Y, color);

                    if (start.X == end.X && start.Y == end.Y)
                        break;

                    int e2 = 2 * err;
                    if (e2 > -dy)
                    {
                        err -= dy;
                        start.X += sx;
                    }
                    if (e2 < dx)
                    {
                        err += dx;
                        start.Y += sy;
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
            public static bool ClipLine(double x0, double y0, double x1, double y1, double xmin, double ymin, double xmax, double ymax, out double newX0, out double newY0, out double newX1, out double newY1)
            {
                // Определение коэффициентов для каждой из сторон окна
                double p0, p1, q0, q1;
                newX0 = x0;
                newY0 = y0;
                newX1 = x1;
                newY1 = y1;

                // Логика отсечения для горизонтальных и вертикальных сторон окна
                p0 = -(x1 - x0);  // dx
                p1 = x1 - x0;     // dx
                q0 = x0 - xmin;
                q1 = xmax - x0;

                if (ClipTest(p0, q0, ref newX0, ref newX1) && ClipTest(p1, q1, ref newX0, ref newX1))
                {
                    return true; // Успешное отсечение
                }

                // Если линия не пересекает окно
                return false;
            }

            public static bool ClipTest(double p, double q, ref double x0, ref double x1)
            {
                if (p == 0)
                {
                    return (q >= 0); // если отрезок параллелен стороне и выходит за пределы окна
                }
                double r = q / p;
                if (p < 0)
                {
                    if (r > x1)
                    {
                        return false;
                    }
                    if (r > x0)
                    {
                        x0 = r;
                    }
                }
                else
                {
                    if (r < x0)
                    {
                        return false;
                    }
                    if (r < x1)
                    {
                        x1 = r;
                    }
                }
                return true;
            }
        }

        // Класс для кадрирования треугольников с использованием Лианга-Барски
        public class TriangleClipper
        {
            public static void ClipTriangle(ref Point v1, ref Point v2, ref Point v3, Rect clipRect)
    {
        // Определяем границы
        double xmin = clipRect.Left, ymin = clipRect.Top, xmax = clipRect.Right, ymax = clipRect.Bottom;

        // Отсечение каждой стороны треугольника
        if (!LiangBarsky.ClipLine(v1.X, v1.Y, v2.X, v2.Y, xmin, ymin, xmax, ymax, out double newX0, out double newY0, out double newX1, out double newY1))
        {
            v1 = new Point(double.NaN, double.NaN); // Помечаем как "невидимую" сторону
        }
        else
        {
            v1 = new Point(newX0, newY0);
            v2 = new Point(newX1, newY1);
        }

        if (!LiangBarsky.ClipLine(v2.X, v2.Y, v3.X, v3.Y, xmin, ymin, xmax, ymax, out newX0, out newY0, out newX1, out newY1))
        {
            v2 = new Point(double.NaN, double.NaN);
        }
        else
        {
            v2 = new Point(newX0, newY0);
            v3 = new Point(newX1, newY1);
        }

        if (!LiangBarsky.ClipLine(v3.X, v3.Y, v1.X, v1.Y, xmin, ymin, xmax, ymax, out newX0, out newY0, out newX1, out newY1))
        {
            v3 = new Point(double.NaN, double.NaN);
        }
        else
        {
            v3 = new Point(newX0, newY0);
            v1 = new Point(newX1, newY1);
        }
    }
        }

        // Класс LayerContainer (Контейнер слоёв)
        public class LayerContainer
        {
            public List<Layer> Layers { get; private set; } = new List<Layer>();

            public void AddLayer(Layer layer)
            {
                Layers.Add(layer);
            }
            public void UpdateLayersWithClipping(WriteableBitmap bitmap)
            {
                Rect clipRect = new Rect(0, 0, bitmap.PixelWidth - 1, bitmap.PixelHeight - 1);

                foreach (var layer in Layers)
                {
                    if (layer.IsVisible && layer.Shape is Triangle triangle)
                    {
                        // Получаем вершины треугольника
                        Point v1 = triangle.Vertices[0];
                        Point v2 = triangle.Vertices[1];
                        Point v3 = triangle.Vertices[2];

                        // Применяем алгоритм Лианга-Барски
                        TriangleClipper.ClipTriangle(ref v1, ref v2, ref v3, clipRect);

                        // Обновляем вершины треугольника
                        triangle.Vertices = new[] { v1, v2, v3 };

                        // Лог для проверки
                        Console.WriteLine($"Обновлённый треугольник в слое {layer.Name}:");
                        Console.WriteLine($"V1: ({v1.X}, {v1.Y}), V2: ({v2.X}, {v2.Y}), V3: ({v3.X}, {v3.Y})");
                    }
                }
                
            }
           
            public void RasterizeAllLayers(WriteableBitmap bitmap, bool modeB)
            {
                foreach (var layer in Layers)
                {
                    layer.Shape.Rasterize(bitmap,modeB);
                }
            }
            public List<IShape> GetBlockingShapes(int layerId)
            {
                return Layers.Where(l => l.Id < layerId && l.IsVisible).Select(l => l.Shape).ToList();
            }
        }
        private LayerContainer _layerContainer;
        private bool _modeB = false; // Режим отображения
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
                Colors.Red, Colors.Black);

            var triangle3 = new Triangle(
              new Point(-50, 50),
              new Point(200, 50),
              new Point(200, 150),
              Colors.Yellow, Colors.Black);
            _layerContainer.AddLayer(new Layer(_layerContainer.Layers.Count, $"Layer {_layerContainer.Layers.Count}", triangle3));
            _layerContainer.AddLayer(new Layer(_layerContainer.Layers.Count, $"Layer {_layerContainer.Layers.Count}", triangle));
            _layerContainer.AddLayer(new Layer(_layerContainer.Layers.Count, $"Layer {_layerContainer.Layers.Count}", triangle2));
            

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
       

        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            _modeB = !_modeB;
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
                new TranslateTransform(_shift.X, _shift.Y)
            }
            };
        }
        private  void RenderButton_Click(object sender, RoutedEventArgs e)
        {
            var bitmap = new WriteableBitmap((int)DrawingCanvas.Width, (int)DrawingCanvas.Height, 2, 2, PixelFormats.Pbgra32, null);
            DrawAxesAndFrame(bitmap);
            // Обновляем слои с отсечением
          
            //_layerContainer.UpdateLayersWithClipping(bitmap);
             Console.WriteLine("end");
            _layerContainer.RasterizeAllLayers(bitmap, _modeB);
             Console.WriteLine("end");
            
           
            DrawGrid(bitmap, gridSize: 2, gridColor: Colors.LightGray);

            DrawingCanvas.Background = new ImageBrush(bitmap);
        }
        private async void RenderSequentiallyButton_Click(object sender, RoutedEventArgs e)
        {
            var bitmap = new WriteableBitmap((int)DrawingCanvas.Width, (int)DrawingCanvas.Height, 2, 2, PixelFormats.Pbgra32, null);
            DrawAxesAndFrame(bitmap);
            // Обновляем слои с отсечением
            _layerContainer.UpdateLayersWithClipping(bitmap);
            foreach (var layer in _layerContainer.Layers)
            {
                if (layer.IsVisible && layer.Shape is Triangle triangle)
                {
                    var visiblePart = layer.Shape.DetermineVisiblePart(_layerContainer.GetBlockingShapes(layer.Id));
                    visiblePart.Rasterize(bitmap, _modeB);
                    DrawingCanvas.Background = new ImageBrush(bitmap);
                    await Task.Delay(1000);
                }
            }
            DrawGrid(bitmap, gridSize: 2, gridColor: Colors.LightGray);

        }
    }


}