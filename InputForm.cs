namespace WebView2MultiView;

public class InputForm : Form
{
    private readonly TextBox clusterIndexTextBox = new();
    private readonly Button okButton = new();
    private readonly Button cancelButton = new();

    public int ClusterIndex { get; private set; } = 0;

    public InputForm()
    {
        InitializeInputForm();
    }

    private void InitializeInputForm()
    {
        Text = "Enter Cluster Index";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(320, 160);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // for spacing

        Controls.Add(layout);

        var label = new Label
        {
            Text = "Cluster Index:",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(3, 6, 3, 6)
        };

        clusterIndexTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        clusterIndexTextBox.Margin = new Padding(3, 6, 3, 6);

        layout.Controls.Add(label, 0, 0);
        layout.Controls.Add(clusterIndexTextBox, 1, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        KeyPreview = true;
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                OnOkButtonClick();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };

        okButton.Text = "OK";
        okButton.Size = new Size(75, 30);
        okButton.Click += (s, e) => OnOkButtonClick();

        cancelButton.Text = "Cancel";
        cancelButton.Size = new Size(75, 30);
        cancelButton.Click += (s, e) => Close();

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.SetColumnSpan(buttonPanel, 2);
        layout.Controls.Add(buttonPanel, 0, 1);
    }

    private void OnOkButtonClick()
    {
        if (int.TryParse(clusterIndexTextBox.Text.Trim(), out int index))
        {
            ClusterIndex = index;
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show(
                "Please enter a valid integer.",
                "Invalid Input",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            clusterIndexTextBox.SelectAll();
            clusterIndexTextBox.Focus();
        }
    }
}
