using System.Collections.Generic;
using System.Drawing;

namespace MagneticLasso;

internal static class MagneticLassoSession
{
    public static List<Point> Polygon { get; set; } = new();
    public static bool CutRequested { get; set; }

    public static void Reset()
    {
        Polygon = new List<Point>();
        CutRequested = false;
    }
}
