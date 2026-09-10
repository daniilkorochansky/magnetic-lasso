using PaintDotNet.Effects;
using System.Drawing.Imaging;

namespace MagneticLasso;

public sealed class MagneticLassoConfigDialog : EffectConfigDialog
{
    private MagneticLassoCanvas? _canvas;
    private Button? _copyButton;

    public MagneticLassoConfigDialog()
    {
        Text = "Magnetic Lasso";
        ClientSize = new Size(1100, 760);
        MinimumSize = new Size(800, 600);

        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;

        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        Icon = MagneticLasso.Properties.Resources.magnetic_lasso_dialog;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        try
        {
            // EffectConfigDialog exposes the current Effect, and its
            // EnvironmentParameters expose the current Paint.NET Surface.
            // This lets the custom dialog work on the actual document.
            var sourceSurface = Effect.EnvironmentParameters.SourceSurface;
            using var aliased = sourceSurface.CreateAliasedBitmap(true);
            var source = MagneticLassoImage.CloneBitmap(aliased);

            BuildUi(source);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "The image could not be retrieved:\n\n" + ex.Message,
                "Magnetic Lasso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private void BuildUi(Bitmap source)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(6)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
      

        _canvas = new MagneticLassoCanvas(source)
        {
            Dock = DockStyle.Fill
        };

        _canvas.PolygonChanged += (_, _) => UpdateButtons();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 5, 0, 0)
        };

        _copyButton = new Button
        {
            Text = "Copy",
            AutoSize = true,
            Enabled = false
        };


        var resetButton = new Button
        {
            Text = "Reset",
            AutoSize = true
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        _copyButton.Click += (_, _) => CopyIsland();
        resetButton.Click += (_, _) => _canvas.ResetDraft();

        buttons.Controls.Add(_copyButton);
        buttons.Controls.Add(resetButton);
        buttons.Controls.Add(cancelButton);

        root.Controls.Add(_canvas, 0, 0);
        root.Controls.Add(buttons, 0, 1);

        Controls.Add(root);

        AcceptButton = null;
        CancelButton = cancelButton;

        Shown += (_, _) =>
        {
            _canvas.Focus();
            _canvas.FitImage();
            UpdateButtons();
        };
    }

    private void UpdateButtons()
    {
        bool enabled = _canvas?.HasPolygon == true;

        if (_copyButton != null)
            _copyButton.Enabled = enabled;

    }

    private void CopyIsland()
    {
        if (_canvas == null || !_canvas.HasPolygon)
            return;

        try
        {
            using var island = MagneticLassoImage.CreateTransparentIsland(
                _canvas.SourceBitmap,
                _canvas.Polygon);

            // Clipboard.SetImage clones owns the clipboard data independently
            // from our temporary bitmap.
            CopyBitmapToClipboard(island);

            MagneticLassoSession.Reset();

            DialogResult = DialogResult.Cancel;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "Failed to copy:\n\n" + ex.Message,
                "Magnetic Lasso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void CopyBitmapToClipboard(Bitmap bitmap)
    {
        using var pngStream = new MemoryStream();

        bitmap.Save(pngStream, ImageFormat.Png);

        // We are creating a separate thread whose position is 0
        var pngData = new MemoryStream(pngStream.ToArray());

        var data = new DataObject();

        data.SetData("PNG", false, pngData);
        data.SetData(DataFormats.Bitmap, false, new Bitmap(bitmap));

        Clipboard.SetDataObject(data, true);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_canvas != null)
            {
                _canvas.PolygonChanged -= (_, _) => UpdateButtons();
            }
        }

        base.Dispose(disposing);
    }
}
