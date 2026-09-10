using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DrawingPoint = System.Drawing.Point;
using DrawingSize = System.Drawing.Size;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Segmentation;

namespace MagneticLasso;

public sealed class MagneticLassoEngine : IDisposable
{
    public const double SimplifyEpsilon = 1.0;

    private Mat _imageBgr;
    private readonly IntelligentScissorsMB _scissors;

    public MagneticLassoEngine(Bitmap source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        using var normalized = new Bitmap(
            source.Width,
            source.Height,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        using (var g = Graphics.FromImage(normalized))
        {
            g.DrawImageUnscaled(source, 0, 0);
        }

        _imageBgr = BitmapConverter.ToMat(normalized);
        if (_imageBgr.Channels() == 4)
        {
            using var bgr = new Mat();
            Cv2.CvtColor(_imageBgr, bgr, ColorConversionCodes.BGRA2BGR);
            _imageBgr.Dispose();
            _imageBgr = bgr.Clone();
        }

        _scissors = new IntelligentScissorsMB();
        _scissors.SetEdgeFeatureCannyParameters(30, 100);
        _scissors.SetGradientMagnitudeMaxLimit(200);
        _scissors.ApplyImage(_imageBgr);
    }

    public DrawingSize ImageSize => new(_imageBgr.Width, _imageBgr.Height);

    public void BuildMap(DrawingPoint source)
    {
        _scissors.BuildMap(new OpenCvSharp.Point(source.X, source.Y));
    }

    public List<DrawingPoint> GetMagneticPath(DrawingPoint target)
    {
        using var contour = new Mat();
        _scissors.GetContour(
            new OpenCvSharp.Point(target.X, target.Y),
            contour,
            false);

        if (contour.Empty())
            return new List<DrawingPoint>();

        contour.GetArray(out Vec2i[] raw);
        return raw.Select(p => new DrawingPoint(p.Item0, p.Item1)).ToList();
    }

    public static List<DrawingPoint> SimplifyMagneticPath(IReadOnlyList<DrawingPoint> path)
    {
        if (path == null || path.Count == 0)
            return new List<DrawingPoint>();

        var clean = new List<DrawingPoint>(path.Count);

        foreach (var p in path)
        {
            if (clean.Count == 0 || clean[^1] != p)
                clean.Add(p);
        }

        if (clean.Count <= 2)
            return clean;

        using var curve = new Mat(clean.Count, 1, MatType.CV_32SC2);
        for (int i = 0; i < clean.Count; i++)
            curve.Set(i, 0, new Vec2i(clean[i].X, clean[i].Y));

        using var approx = new Mat();
        Cv2.ApproxPolyDP(
            curve,
            approx,
            SimplifyEpsilon,
            false);

        approx.GetArray(out Vec2i[] values);
        var simplified = values
            .Select(p => new DrawingPoint(p.Item0, p.Item1))
            .ToList();

        if (simplified.Count == 0)
            return new List<DrawingPoint> { clean[0], clean[^1] };

        if (simplified[0] != clean[0])
            simplified.Insert(0, clean[0]);

        if (simplified[^1] != clean[^1])
            simplified.Add(clean[^1]);

        var result = new List<DrawingPoint> { simplified[0] };
        const double minDistance = 2.0;
        const double minDistanceSq = minDistance * minDistance;

        for (int i = 1; i < simplified.Count - 1; i++)
        {
            double dx = simplified[i].X - result[^1].X;
            double dy = simplified[i].Y - result[^1].Y;

            if (dx * dx + dy * dy >= minDistanceSq)
                result.Add(simplified[i]);
        }

        if (result[^1] != simplified[^1])
            result.Add(simplified[^1]);

        return result;
    }

    public void Dispose()
    {
        _scissors.Dispose();
        _imageBgr.Dispose();
    }
}
