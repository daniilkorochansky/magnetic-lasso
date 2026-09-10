using PaintDotNet;
using PaintDotNet.Effects;
using System.Windows;

namespace MagneticLasso;

[EffectCategory(EffectCategory.Effect)]
[PluginSupportInfo(typeof(MagneticLassoPluginSupportInfo))]
public sealed class MagneticLassoEffect : Effect
{
    public MagneticLassoEffect()
        : base(
            "Magnetic Lasso",
            MagneticLasso.Properties.Resources.magnetic_lasso_plugin,
            "Selection",
            new EffectOptions
            {
                Flags = EffectFlags.Configurable
            })
    {
    }

    public override EffectConfigDialog CreateConfigDialog()
    {
        MagneticLassoSession.Reset();
        return new MagneticLassoConfigDialog();
    }

    public override void Render(
        EffectConfigToken? token,
        RenderArgs srcArgs,
        RenderArgs dstArgs,
        Rectangle[] rois,
        int startIndex,
        int length)
    {
        // Сначала полностью копируем исходное изображение.
        dstArgs.Surface.CopySurface(srcArgs.Surface);

        if (!MagneticLassoSession.CutRequested ||
            MagneticLassoSession.Polygon.Count < 3)
        {
            MagneticLassoSession.Reset();
            return;
        }

        var polygon = MagneticLassoSession.Polygon;

        // Работаем непосредственно с Surface через GDI+ alias.
        // alpha=true -> Format32bppArgb.
        using var bitmap = dstArgs.Surface.CreateAliasedBitmap(true);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode =
            System.Drawing.Drawing2D.SmoothingMode.None;

        graphics.CompositingMode =
            System.Drawing.Drawing2D.CompositingMode.SourceCopy;

        using var path = new System.Drawing.Drawing2D.GraphicsPath();

        path.AddPolygon(polygon.ToArray());

        // Полностью прозрачный цвет.
        using var transparentBrush =
            new SolidBrush(Color.FromArgb(0, 0, 0, 0));

        // Вырезаем область полигона.
        graphics.FillPath(
            transparentBrush,
            path);

        // Очень важно:
        // Paint.NET Surface использует BGRA32 и может обнаружить
        // некорректное состояние alpha после работы GDI+.
        dstArgs.Surface.DetectAndFixDishonestAlpha();

        MagneticLassoSession.Reset();
    }
}
