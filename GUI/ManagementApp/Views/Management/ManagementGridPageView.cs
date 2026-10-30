namespace TrangSach.Management.Views;

internal sealed class ManagementPageActionEventArgs(string action) : EventArgs
{
    public string Action { get; } = action;
}

/// <summary>Traditional management page layout: title, command bar, searchable grid and optional master-detail area.</summary>
internal class ManagementGridPageView : UserControl
{
    private readonly TableLayoutPanel _toolbar;
    private readonly TableLayoutPanel _pageLayout;
    private readonly FlowLayoutPanel _actions;
    private readonly FlowLayoutPanel _searchTools;
    private readonly FlowLayoutPanel _searchFilters;
    private readonly TextBox _search = new();
    private readonly Label _rowCount = new();
    private readonly SplitContainer _gridSplit;
    private readonly Label _detailTitle = new();
    private object? _unfilteredDataSource;
    private bool _applyingFilter;

    public string PageTitle { get; }
    public DataGridView Grid { get; }
    public event EventHandler<ManagementPageActionEventArgs>? ActionRequested;

    protected ManagementGridPageView(string pageTitle)
    {
        PageTitle = pageTitle;
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(244, 247, 250);
        Padding = new Padding(18, 14, 18, 16);

        _pageLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        _pageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        _pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        _pageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = Padding.Empty };
        titlePanel.Controls.Add(new Label
        {
            Text = PageTitle,
            AutoSize = true,
            Location = new Point(2, 2),
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            ForeColor = Color.FromArgb(29, 51, 75)
        });
        titlePanel.Controls.Add(new Label
        {
            Text = "DANH SÁCH VÀ TÁC VỤ",
            AutoSize = true,
            Location = new Point(4, 36),
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            ForeColor = Color.FromArgb(112, 128, 143)
        });

        _toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 760));
        _toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        _toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        _actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Padding = new Padding(8, 7, 4, 5), Margin = Padding.Empty };
        _searchTools = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Padding = new Padding(4, 5, 8, 2), Margin = Padding.Empty };
        _searchFilters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Padding = new Padding(8, 1, 8, 4), Margin = Padding.Empty, BackColor = Color.FromArgb(248, 250, 252) };

        Grid = CreateGrid();
        _gridSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(224, 231, 238),
            SplitterWidth = 6,
            Panel2Collapsed = true
        };
        _gridSplit.Panel1.BackColor = Color.White;
        _gridSplit.Panel1.Controls.Add(Grid);

        _detailTitle.Dock = DockStyle.Top;
        _detailTitle.Height = 34;
        _detailTitle.Padding = new Padding(7, 9, 4, 2);
        _detailTitle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _detailTitle.ForeColor = Color.FromArgb(55, 73, 91);

        var searchLabel = new Label
        {
            Text = "Tìm kiếm",
            AutoSize = true,
            Margin = new Padding(8, 9, 3, 0),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(72, 88, 104)
        };
        _search.Width = 300;
        _search.Height = 31;
        _search.Margin = new Padding(2, 2, 8, 0);
        _search.PlaceholderText = "Nhập từ khóa cần tìm...";
        _search.Font = new Font("Segoe UI", 9);
        new ToolTip().SetToolTip(_search, "Tìm thường: nhập từ khóa. Tìm nâng cao: Giá bán >= 100000 AND Tồn kho < 5. Có thể dùng =, !=, >, <, >=, <=, AND, OR, NOT và ngoặc.");
        _search.TextChanged += (_, _) => ApplyQuickFilter();
        _rowCount.AutoSize = true;
        _rowCount.Margin = new Padding(2, 9, 5, 0);
        _rowCount.Font = new Font("Segoe UI", 8.5f);
        _rowCount.ForeColor = Color.FromArgb(112, 128, 143);
        Grid.DataSourceChanged += (_, _) =>
        {
            if (_applyingFilter) return;
            _unfilteredDataSource = Grid.DataSource;
            var rows = (Grid.DataSource as System.Collections.IEnumerable)?.Cast<object>().ToArray() ?? [];
            OnDataSourceRefreshed(rows);
        };
        Grid.DataBindingComplete += (_, _) => ApplyQuickFilter();
        _searchTools.Controls.AddRange([searchLabel, _search, _rowCount]);
        _toolbar.Controls.Add(_actions, 0, 0);
        _toolbar.SetRowSpan(_actions, 2);
        _toolbar.Controls.Add(_searchTools, 1, 0);
        _toolbar.Controls.Add(_searchFilters, 1, 1);

        _pageLayout.Controls.Add(titlePanel, 0, 0);
        _pageLayout.Controls.Add(_toolbar, 0, 1);
        _pageLayout.Controls.Add(_gridSplit, 0, 2);
        Controls.Add(_pageLayout);
        _gridSplit.SizeChanged += (_, _) => ResizeDetailArea();
    }

    protected void AddAction(string caption, string action)
    {
        var isPrimary = action is "add" or "create";
        var isDestructive = action is "delete" or "cancel" or "hide";
        var button = new Button
        {
            Text = caption,
            AutoSize = true,
            Height = 32,
            Margin = new Padding(3, 1, 3, 1),
            FlatStyle = FlatStyle.Flat,
            BackColor = isPrimary ? Color.FromArgb(31, 111, 88) : isDestructive ? Color.FromArgb(255, 238, 237) : Color.FromArgb(241, 245, 249),
            ForeColor = isPrimary ? Color.White : isDestructive ? Color.FromArgb(166, 57, 57) : Color.FromArgb(45, 63, 82),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => ActionRequested?.Invoke(this, new ManagementPageActionEventArgs(action));
        _actions.Controls.Add(button);
    }

    protected void AddToolbarControl(Control control)
    {
        control.Margin = new Padding(4, 2, 4, 0);
        _actions.Controls.Add(control);
    }

    protected void AddSearchFilterControl(Control control)
    {
        control.Margin = new Padding(3, 1, 8, 1);
        _searchFilters.Controls.Add(control);
        _toolbar.RowStyles[1] = new RowStyle(SizeType.Absolute, 36);
        _pageLayout.RowStyles[1] = new RowStyle(SizeType.Absolute, 88);
    }

    protected void RefreshFilter() => ApplyQuickFilter();

    protected void SetSearchPlaceholder(string text) => _search.PlaceholderText = text;

    protected virtual bool HasAdditionalFilters => false;
    protected virtual bool MatchesAdditionalFilters(object item) => true;
    protected virtual IEnumerable<string?> GetAdditionalSearchValues(object item) => [];
    protected virtual void OnDataSourceRefreshed(IReadOnlyList<object> rows) { }

    protected void SetDetailsView(Control details, string initialMessage)
    {
        _detailTitle.Text = initialMessage;
        details.Dock = DockStyle.Fill;
        details.Margin = Padding.Empty;
        var detailPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 8), BackColor = Color.White };
        detailPanel.Controls.Add(details);
        detailPanel.Controls.Add(_detailTitle);
        _gridSplit.Panel2.Controls.Add(detailPanel);
        _gridSplit.Panel2Collapsed = false;
        ResizeDetailArea();
    }

    protected void SetDetailsTitle(string text) => _detailTitle.Text = text;

    private void ResizeDetailArea()
    {
        if (_gridSplit.Panel2Collapsed) return;
        var available = _gridSplit.ClientSize.Height - _gridSplit.SplitterWidth;
        const int primaryMinimum = 180;
        const int detailMinimum = 120;
        if (available <= primaryMinimum + detailMinimum) return;
        _gridSplit.Panel1MinSize = primaryMinimum;
        _gridSplit.Panel2MinSize = detailMinimum;
        var detailHeight = Math.Min(230, Math.Max(detailMinimum, available / 3));
        _gridSplit.SplitterDistance = Math.Clamp(available - detailHeight, primaryMinimum, available - detailMinimum);
    }

    private void ApplyQuickFilter()
    {
        if (_applyingFilter) return;
        var query = _search.Text.Trim();
        var allRows = (_unfilteredDataSource as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? [];
        Func<object, bool>? expression = null;
        string? syntaxError = null;
        var isExpression = ManagementSearchExpression.IsExpression(query);
        if (isExpression) ManagementSearchExpression.TryCreate(query, allRows.FirstOrDefault(), out expression, out syntaxError);
        var filteredRows = allRows.Where(item =>
            (query.Length == 0 || (isExpression ? syntaxError is null && expression?.Invoke(item) == true : ContainsSearchTerm(item)))
            && MatchesAdditionalFilters(item)).ToList();
        object? requestedSource = _unfilteredDataSource;
        var hasFilters = query.Length > 0 || HasAdditionalFilters;
        if (hasFilters && _unfilteredDataSource is not null)
        {
            var itemType = allRows.FirstOrDefault()?.GetType()
                ?? _unfilteredDataSource.GetType().GetGenericArguments().FirstOrDefault()
                ?? typeof(object);
            var typedRows = Array.CreateInstance(itemType, filteredRows.Count);
            for (var index = 0; index < filteredRows.Count; index++) typedRows.SetValue(filteredRows[index], index);
            requestedSource = typedRows;
        }
        if (syntaxError is not null) _rowCount.Text = syntaxError;
        else _rowCount.Text = !hasFilters || _unfilteredDataSource is null
            ? $"{allRows.Count:N0} dòng"
            : $"{filteredRows.Count:N0} / {allRows.Count:N0} dòng";
        if (ReferenceEquals(Grid.DataSource, requestedSource)) return;

        try
        {
            _applyingFilter = true;
            Grid.DataSource = requestedSource;
        }
        finally { _applyingFilter = false; }

        bool ContainsSearchTerm(object item)
        {
            var values = Grid.Columns.Cast<DataGridViewColumn>()
                .Where(column => column.Visible && !string.IsNullOrWhiteSpace(column.DataPropertyName))
                .Select(column => item.GetType().GetProperty(column.DataPropertyName)?.GetValue(item))
                .Select(value => Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture))
                .Concat(GetAdditionalSearchValues(item))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
            var terms = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return terms.All(term => values.Any(value => value!.Contains(term, StringComparison.CurrentCultureIgnoreCase)));
        }
    }

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Color.White,
        GridColor = Color.FromArgb(229, 235, 241),
        RowHeadersVisible = false,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersHeight = 40,
        EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(237, 242, 247),
            ForeColor = Color.FromArgb(39, 58, 78),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 6, 0),
            WrapMode = DataGridViewTriState.False
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = new Font("Segoe UI", 9),
            Padding = new Padding(6, 1, 6, 1),
            SelectionBackColor = Color.FromArgb(222, 238, 248),
            SelectionForeColor = Color.FromArgb(25, 45, 68)
        },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(249, 251, 253)
        },
        RowTemplate = { Height = 38 }
    };

    protected static DataGridView CreateDetailGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Color.White,
        GridColor = Color.FromArgb(229, 235, 241),
        RowHeadersVisible = false,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersHeight = 34,
        EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(237, 242, 247),
            ForeColor = Color.FromArgb(39, 58, 78),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        },
        DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 8.5f), Padding = new Padding(5, 1, 5, 1) },
        RowTemplate = { Height = 32 }
    };
}

