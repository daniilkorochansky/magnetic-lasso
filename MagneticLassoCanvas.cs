using System.Drawing.Drawing2D;

namespace MagneticLasso;

public sealed class MagneticLassoCanvas : Control
{
    private readonly Bitmap _source;
    private readonly MagneticLassoEngine _engine;

    private readonly List<Point> _points = new();
    private readonly List<List<Point>> _segments = new();
    private List<Point> _magneticPreview = new();

    private bool _drawing;
    private bool _panning;
    private Point _panStart;

    private double _zoom = 1.0;
    private double _panX;
    private double _panY;

    private Point _mouseLogical;

    public bool IsClosed { get; private set; }
    public IReadOnlyList<Point> Polygon { get; private set; } = Array.Empty<Point>();

    public bool HasPolygon => IsClosed && Polygon.Count >= 3;

    public event EventHandler? PolygonChanged;

    public Bitmap SourceBitmap => _source;

    public MagneticLassoCanvas(Bitmap source)
    {
        _source = MagneticLassoImage.CloneBitmap(source);
        _engine = new MagneticLassoEngine(_source);

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);

        BackColor = Color.FromArgb(45, 45, 45);
        Cursor = Cursors.Cross;
        TabStop = true;

        MouseDown += Canvas_MouseDown;
        MouseMove += Canvas_MouseMove;
        MouseWheel += Canvas_MouseWheel;
        MouseUp += Canvas_MouseUp;
        KeyDown += Canvas_KeyDown;
        Resize += (_, _) => FitImage();

        FitImage();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _engine.Dispose();
            _source.Dispose();
        }

        base.Dispose(disposing);
    }

    private Point ScreenToLogical(Point p)
    {
        int x = (int)Math.Round((p.X - _panX) / _zoom);
        int y = (int)Math.Round((p.Y - _panY) / _zoom);

        x = Math.Clamp(x, 0, _source.Width - 1);
        y = Math.Clamp(y, 0, _source.Height - 1);

        return new Point(x, y);
    }

    private PointF LogicalToScreen(Point p)
    {
        return new PointF(
            (float)(p.X * _zoom + _panX),
            (float)(p.Y * _zoom + _panY));
    }

    public void FitImage()
    {
        if (_source.Width <= 0 || _source.Height <= 0 || ClientSize.Width <= 10)
            return;

        const int margin = 16;

        _zoom = Math.Min(
            (ClientSize.Width - margin * 2.0) / _source.Width,
            (ClientSize.Height - margin * 2.0) / _source.Height);

        _zoom = Math.Max(0.05, Math.Min(25.0, _zoom));

        _panX = (ClientSize.Width - _source.Width * _zoom) / 2.0;
        _panY = (ClientSize.Height - _source.Height * _zoom) / 2.0;

        Invalidate();
    }

    public void UndoLastSegment()
    {
        if (_segments.Count == 0)
            return;

        _segments.RemoveAt(_segments.Count - 1);
        RebuildPoints();

        IsClosed = false;
        Polygon = Array.Empty<Point>();
        _magneticPreview.Clear();

        if (_points.Count > 0)
        {
            _engine.BuildMap(_points[^1]);
            _drawing = true;
        }
        else
        {
            _drawing = false;
        }

        Invalidate();
        PolygonChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResetDraft()
    {
        _points.Clear();
        _segments.Clear();
        _magneticPreview.Clear();
        IsClosed = false;
        Polygon = Array.Empty<Point>();
        _drawing = false;
        Invalidate();
        PolygonChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RebuildPoints()
    {
        _points.Clear();

        foreach (var segment in _segments)
            _points.AddRange(segment);
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        Focus();

        if (e.Button == MouseButtons.Middle)
        {
            _panning = true;
            _panStart = e.Location;
            Capture = true;
            Cursor = Cursors.Hand;
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (IsClosed)
                return;

            _mouseLogical = ScreenToLogical(e.Location);
            _drawing = true;

            if (_points.Count == 0)
            {
                _points.Add(_mouseLogical);
                _segments.Add(new List<Point> { _mouseLogical });
                _engine.BuildMap(_mouseLogical);
                Invalidate();
                return;
            }

            if ((ModifierKeys & Keys.Shift) != Keys.None)
            {
                var path = _magneticPreview.Count > 0
                    ? _magneticPreview
                    : _engine.GetMagneticPath(_mouseLogical);

                var simplified = MagneticLassoEngine.SimplifyMagneticPath(path);

                if (simplified.Count > 1)
                {
                    var segment = simplified.Skip(1).ToList();
                    _segments.Add(segment);
                    _points.AddRange(segment);
                    _engine.BuildMap(_points[^1]);
                }
            }
            else
            {
                _segments.Add(new List<Point> { _mouseLogical });
                _points.Add(_mouseLogical);
                _engine.BuildMap(_mouseLogical);
            }

            _magneticPreview.Clear();
            Invalidate();
            return;
        }

        if (e.Button == MouseButtons.Right)
        {
            ClosePolygon((ModifierKeys & Keys.Shift) != Keys.None);
        }
    }

    private void ClosePolygon(bool magnetic)
    {
        if (_points.Count < 3)
            return;

        if (magnetic)
        {
            var closing = _engine.GetMagneticPath(_points[0]);
            var simplified = MagneticLassoEngine.SimplifyMagneticPath(closing);

            if (simplified.Count > 1)
            {
                var segment = simplified.Skip(1).ToList();
                _segments.Add(segment);
                _points.AddRange(segment);
            }
        }

        IsClosed = true;
        Polygon = new List<Point>(_points);
        _magneticPreview.Clear();
        _drawing = false;

        Invalidate();
        PolygonChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_panning)
        {
            _panX += e.X - _panStart.X;
            _panY += e.Y - _panStart.Y;
            _panStart = e.Location;
            Invalidate();
            return;
        }

        _mouseLogical = ScreenToLogical(e.Location);

        if (_drawing && !IsClosed && _points.Count > 0)
        {
            if ((ModifierKeys & Keys.Shift) != Keys.None)
            {
                try
                {
                    _magneticPreview = _engine.GetMagneticPath(_mouseLogical);
                }
                catch
                {
                    _magneticPreview.Clear();
                }
            }
            else
            {
                _magneticPreview.Clear();
            }

            Invalidate();
        }
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (_source.Width <= 0)
            return;

        var before = ScreenToLogical(e.Location);

        double factor = e.Delta > 0 ? 1.10 : 1.0 / 1.10;
        double newZoom = Math.Clamp(_zoom * factor, 0.20, 25.0);

        if (Math.Abs(newZoom - _zoom) < 0.000001)
            return;

        _zoom = newZoom;

        _panX = e.X - before.X * _zoom;
        _panY = e.Y - before.Y * _zoom;

        Invalidate();
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            _panning = false;
            Capture = false;
            Cursor = Cursors.Cross;
        }
    }

    private void Canvas_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Z && e.Control)
        {
            UndoLastSegment();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            ResetDraft();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            _magneticPreview.Clear();
            Invalidate();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Home)
        {
            FitImage();
            e.SuppressKeyPress = true;
        }
    }

    private void DrawCheckerboard(Graphics g)
    {
        const int tileSize = 16;

        Color color1 = Color.FromArgb(45, 45, 45);
        Color color2 = Color.FromArgb(60, 60, 60);

        using var brush1 = new SolidBrush(color1);
        using var brush2 = new SolidBrush(color2);

        for (int y = 0; y < ClientSize.Height; y += tileSize)
        {
            for (int x = 0; x < ClientSize.Width; x += tileSize)
            {
                bool even = ((x / tileSize) + (y / tileSize)) % 2 == 0;

                g.FillRectangle(
                    even ? brush1 : brush2,
                    x,
                    y,
                    tileSize,
                    tileSize);
            }
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        DrawCheckerboard(e.Graphics);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        var dest = new RectangleF(
            (float)_panX,
            (float)_panY,
            (float)(_source.Width * _zoom),
            (float)(_source.Height * _zoom));

        e.Graphics.DrawImage(_source, dest);

        using var red = new Pen(Color.Red, 2.0f);
        using var blue = new Pen(Color.DeepSkyBlue, 2.0f);
        using var yellow = new SolidBrush(Color.Yellow);
        using var cyan = new Pen(Color.Cyan, 2.0f);

        if (_points.Count > 1)
        {
            for (int i = 1; i < _points.Count; i++)
            {
                var a = LogicalToScreen(_points[i - 1]);
                var b = LogicalToScreen(_points[i]);
                e.Graphics.DrawLine(red, a, b);
            }
        }

        if (_drawing && _points.Count > 0 && !IsClosed)
        {
            var anchor = LogicalToScreen(_points[^1]);

            if (_magneticPreview.Count > 1 &&
                (ModifierKeys & Keys.Shift) != Keys.None)
            {
                var prev = LogicalToScreen(_magneticPreview[0]);

                for (int i = 1; i < _magneticPreview.Count; i++)
                {
                    var next = LogicalToScreen(_magneticPreview[i]);
                    e.Graphics.DrawLine(blue, prev, next);
                    prev = next;
                }
            }
            else
            {
                e.Graphics.DrawLine(red, anchor, LogicalToScreen(_mouseLogical));
            }
        }

        foreach (var p in _points)
        {
            var s = LogicalToScreen(p);
            e.Graphics.FillEllipse(
                yellow,
                s.X - 3.5f,
                s.Y - 3.5f,
                7,
                7);
        }

        if (_drawing && (ModifierKeys & Keys.Shift) != Keys.None)
        {
            var s = LogicalToScreen(_mouseLogical);
            e.Graphics.DrawEllipse(
                cyan,
                s.X - 4,
                s.Y - 4,
                8,
                8);
        }

        if (HasPolygon)
        {
            var screen = Polygon.Select(LogicalToScreen).ToArray();
            using var closed = new Pen(Color.LimeGreen, 2.0f);
            e.Graphics.DrawPolygon(closed, screen);
        }

        using var textBrush = new SolidBrush(Color.White);
        e.Graphics.DrawString(
            "LMB - Point | Shift - Magnet | Shift+LMB - Segment | RMB - Close | Ctrl+Z - Undo | MMB - Movement | MMW - Zoom",
            Font,
            textBrush,
            10,
            10);
    }
}
