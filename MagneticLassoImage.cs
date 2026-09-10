using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MagneticLasso;

internal static class MagneticLassoImage
{
    public static Bitmap CloneBitmap(Bitmap source)
    {
        var copy = new Bitmap(
            source.Width,
            source.Height,
            PixelFormat.Format32bppArgb);

        using var g = Graphics.FromImage(copy);
        g.DrawImageUnscaled(source, 0, 0);
        return copy;
    }

    public static Bitmap CreateTransparentIsland(
        Bitmap source,
        IReadOnlyList<Point> polygon)
    {
        if (polygon == null || polygon.Count < 3)
            throw new ArgumentException("The contour must contain at least 3 points.", nameof(polygon));

        int minX = source.Width - 1;
        int minY = source.Height - 1;
        int maxX = 0;
        int maxY = 0;

        foreach (var p in polygon)
        {
            minX = Math.Min(minX, p.X);
            minY = Math.Min(minY, p.Y);
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
        }

        minX = Math.Max(0, minX);
        minY = Math.Max(0, minY);
        maxX = Math.Min(source.Width - 1, maxX);
        maxY = Math.Min(source.Height - 1, maxY);

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        var result = new Bitmap(
            width,
            height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (var g = Graphics.FromImage(result))
        {
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.Clear(Color.Transparent);

            using var path = new System.Drawing.Drawing2D.GraphicsPath();

            var localPoints = polygon
                .Select(p => new Point(p.X - minX, p.Y - minY))
                .ToArray();

            path.AddPolygon(localPoints);

            // We draw the original image only inside the islet.
            g.SetClip(path);

            g.DrawImage(
                source,
                new Rectangle(0, 0, width, height),
                new Rectangle(minX, minY, width, height),
                GraphicsUnit.Pixel);
        }

        return result;
    }

    public static void ClearPolygonOnBitmap(
        Bitmap bitmap,
        IReadOnlyList<Point> polygon)
    {
        using var g = Graphics.FromImage(bitmap);
        g.CompositingMode = CompositingMode.SourceCopy;
        g.SmoothingMode = SmoothingMode.None;

        using var path = new GraphicsPath();
        path.AddPolygon(new List<Point>(polygon).ToArray());

        using var transparent = new SolidBrush(Color.FromArgb(0, 0, 0, 0));
        g.FillPath(transparent, path);
    }
}
