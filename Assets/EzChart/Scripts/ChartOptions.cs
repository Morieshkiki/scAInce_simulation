using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
using EzChartText = TMPro.TextMeshProUGUI;
using ChartTextFont = TMPro.TMP_FontAsset;
#else
using EzChartText = UnityEngine.UI.Text;
using ChartTextFont = UnityEngine.Font;
#endif

namespace ChartUtil
{
    public enum ChartType
    {
        PieChart, BarChart, LineChart, RoseChart, RadarChart, Gauge, SolidGauge
    }

    public enum AxisType
    {
        Category, Linear
    }

    public enum ColumnStacking
    {
        None, Normal, Percent
    }

    public class ChartOptions : MonoBehaviour
    {
        [System.Serializable]
        public struct ChartTextOptions
        {
            [Tooltip("Text color")]
            public Color color;
            [Tooltip("Text font size")]
            public int fontSize;
            [Tooltip("Text font. If this is null, Options - Plot Option - General Font will be used")]
            public ChartTextFont font;
            [Tooltip("Text template. Chart will instantiate the text GameObject with all its attached components (e.g. shadow, outline), which allows more advanced text settings. This will overwrite all basic text options (Color, Font Size and Font).")]
            public EzChartText customizedText;
            public ChartTextOptions(Color c, ChartTextFont f, int fs, EzChartText ct = null)
            {
                color = c;
                font = f;
                fontSize = fs;
                customizedText = ct;
            }
        }

        [System.Serializable]
        public struct BandOptions
        {
            [Tooltip("From")]
            [Range(0.0f, 1.0f)] public float from;
            [Tooltip("To")]
            [Range(0.0f, 1.0f)] public float to;
            [Tooltip("Band color")]
            public Color color;
        }

        [System.Serializable]
        public class BarChartOptions
        {
            [Tooltip("Set data color by categories instead of by series")]
            public bool colorByCategories = false;
            [Tooltip("Width of bars")]
            public float barWidth = 10.0f;
            [Tooltip("Separation distance between bars")]
            public float itemSeparation = 3.0f;
            [Tooltip("Enable/disable bar background")]
            public bool enableBarBackground = false;
            [Tooltip("Width of bar background")]
            public float barBackgroundWidth = 10.0f;
            [Tooltip("Color of bar background")]
            public Color barBackgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.2f);
            public float barGradientStartIntensity = 0.0f;
            public Color barGradientStartColor = Color.white;
            public float barGradientEndIntensity = 0.0f;
            public Color barGradientEndColor = Color.white;
        }

        [System.Serializable]
        public class LineChartOptions
        {
            [Tooltip("Point size for line chart item points")]
            public float pointSize = 10.0f;
            [Tooltip("Enable/disable lines")]
            public bool enableLine = true;
            [Tooltip("Line width for line chart lines")]
            public float lineWidth = 5.0f;
            [Tooltip("Enable/disable shade under the lines")]
            public bool enableShade = false;
            [Tooltip("Opacity of the shade")]
            public float shadeOpacity = 0.7f;
            [Tooltip("Enable/disable point outline")]
            public bool enablePointOutline = false;
            [Tooltip("Width of point outline")]
            public float pointOutlineWidth = 2.0f;
            [Tooltip("Color of point outline")]
            public Color pointOutlineColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            [Tooltip("Plot spline curve")]
            public bool splineCurve = false;
        }

        [System.Serializable]
        public class PieChartOptions
        {
            [Tooltip("Separation distance between items")]
            public float itemSeparation = 0.0f;
        }

        [System.Serializable]
        public class RoseChartOptions
        {
            [Tooltip("Set data color by categories instead of by series")]
            public bool colorByCategories = false;
            [Tooltip("Width of bars")]
            public float barWidth = 10.0f;
            [Tooltip("Separation distance between bars")]
            public float itemSeparation = 3.0f;
        }

        [System.Serializable]
        public class RadarChartOptions
        {
            [Tooltip("Point size for radar chart item points")]
            public float pointSize = 10.0f;
            [Tooltip("Enable/disable lines")]
            public bool enableLine = true;
            [Tooltip("Line width for radar chart lines")]
            public float lineWidth = 5.0f;
            [Tooltip("Enable/disable shade")]
            public bool enableShade = false;
            [Tooltip("Transparency of the shade")]
            public float shadeOpacity = 0.7f;
            [Tooltip("Enable/disable point outline")]
            public bool enablePointOutline = false;
            [Tooltip("Width of point outline")]
            public float pointOutlineWidth = 2.0f;
            [Tooltip("Color of point outline")]
            public Color pointOutlineColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            [Tooltip("Use circular grid")]
            public bool circularGrid = false;
        }

        [System.Serializable]
        public class GaugeOptions
        {
            [Tooltip("Pointer length scale")]
            public float pointerLengthScale = 1.0f;
            [Tooltip("Pointer widht")]
            public float pointerWidth = 4.0f;
            [Tooltip("Color of pointer")]
            public Color pointerColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            [Tooltip("Bands")]
            public BandOptions[] bands;
            [Tooltip("Width of bands")]
            public float bandWidth = 10.0f;
        }

        [System.Serializable]
        public class SolidGaugeOptions
        {
            [Tooltip("Width of bars")]
            public float barWidth = 10.0f;
            [Tooltip("Enable/disable bar background")]
            public bool enableBarBackground = false;
            [Tooltip("Width of bar background")]
            public float barBackgroundWidth = 10.0f;
            [Tooltip("Color of bar background")]
            public Color barBackgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.2f);
        }

        [System.Serializable]
        public class PlotOptions
        {
            [Tooltip("C# culture info for the chart. Leave it empty for invariant culture.")]
            public string cultureInfoName = "";
            [Tooltip("Colors for chart series data, if number of series is larger then data color length, it will loop over the first color element")]
            public Color[] dataColor = new Color[11]
            {
                new Color32 (125, 180, 240, 255),
                new Color32 (255, 125, 80, 255),
                new Color32 (144, 237, 125, 255),
                new Color32 (247, 163, 92, 255),
                new Color32 (128, 133, 233, 255),
                new Color32 (241, 92, 128, 255),
                new Color32 (228, 211, 84, 255),
                new Color32 (43, 144, 143, 255),
                new Color32 (244, 91, 91, 255),
                new Color32 (190, 110, 240, 255),
                new Color32 (170, 240, 240, 255)
            };

            [Tooltip("Font used for the all text elements in the chart")]
            public ChartTextFont generalFont = null;
            [Tooltip("Invert XY axes (if applicable)")]
            public bool inverted = false;
            [Tooltip("Track mouse position to highlight chart items and display tooltip")]
            public bool mouseTracking = true;
            [Tooltip("Bring the grid to the front")]
            public bool frontGrid = false;
            [Tooltip("Column stacking modes")]
            public ColumnStacking columnStacking = ColumnStacking.None;
            [Tooltip("Item background color when mouse is hovering the item")]
            public Color itemHighlightColor = new Color32(173, 219, 238, 100);
            [Tooltip("Enable/Disable chart background")]
            public bool enableBackground = true;
            [Tooltip("Chart background color")]
            public Color backgroundColor = Color.clear;

            [Header("Chart specific options")]
            public BarChartOptions barChartOption = new BarChartOptions();
            public LineChartOptions lineChartOption = new LineChartOptions();
            public PieChartOptions pieChartOption = new PieChartOptions();
            public RoseChartOptions roseChartOption = new RoseChartOptions();
            public RadarChartOptions radarChartOption = new RadarChartOptions();
            public GaugeOptions gaugeOption = new GaugeOptions();
            public SolidGaugeOptions solidGaugeOption = new SolidGaugeOptions();
        }
        
        [System.Serializable]
        public class Title
        {
            [Tooltip("Show/hide chart main title")]
            public bool enableMainTitle = true;
            [Tooltip("Main title content")]
            public string mainTitle = "Main Title";
            [Tooltip("Main title text options")]
            public ChartTextOptions mainTitleOption = new ChartTextOptions(new Color(0.2f, 0.2f, 0.2f, 1.0f), null, 18);
            [Tooltip("Show/hide chart sub title")]
            public bool enableSubTitle = false;
            [Tooltip("Sub title content")]
            public string subTitle = "Sub Title";
            [Tooltip("Sub title text options")]
            public ChartTextOptions subTitleOption = new ChartTextOptions(new Color(0.2f, 0.2f, 0.2f, 1.0f), null, 12);
        }

        [System.Serializable]
        public class XAxis
        {
            [Tooltip("Reverse x axis")]
            public bool reversed = false;

            [Header("Grid")]
            [Tooltip("Show/Hide axis line")]
            public bool enableAxisLine = true;
            [Tooltip("Color of axis line")]
            public Color axisLineColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            [Tooltip("Width of axis line")]
            public float axisLineWidth = 2;
            [Tooltip("Show/Hide grid lines")]
            public bool enableGridLine = true;
            [Tooltip("Color of grid lines")]
            public Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            [Tooltip("Width of grid lines")]
            public float gridLineWidth = 1;
            [Tooltip("Show/Hide ticks")]
            public bool enableTick = true;
            [Tooltip("Color of ticks")]
            public Color tickColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            [Tooltip("Width/Length of ticks")]
            public Vector2 tickSize = new Vector2(2.0f, 4.0f);

            [Header("Title and labels")]
            [Tooltip("Show/hide x axis title")]
            public bool enableTitle = false;
            [Tooltip("X axis title content")]
            public string title = "xAxis";
            [Tooltip("Title text options")]
            public ChartTextOptions titleOption = new ChartTextOptions(new Color(0.2f, 0.2f, 0.2f, 1.0f), null, 14);
            [Tooltip("Show/hide x axis labels")]
            public bool enableLabel = true;
            [Tooltip("Label format string for linear axis, keywords will be replaced while other characters remain the same, useful for adding unit. " +
                "'{value}' will be replaced with label value. ")]
            public string labelFormat = "{value}";
            [Tooltip("Label numeric format string, it is a C# standard numeric format string. Leave it empty for auto numeric format")]
            public string labelNumericFormat = "";
            [Tooltip("Label text options")]
            public ChartTextOptions labelOption = new ChartTextOptions(new Color(0.2f, 0.2f, 0.2f, 1.0f), null, 12);
            [Tooltip("Automatically rotate labels")]
            public bool autoRotateLabel = true;

            [Header("Values")]
            [Tooltip("The maximum value of the axis when 'auto axis values' is disabled. Not applicable to categorized axis.")]
            public AxisType type = AxisType.Category;
            [Tooltip("Automatically caculate min/max axis values for linear axis. If disabled, axis values will be determined by 'min' and 'max'.")]
            public bool autoAxisValues = true;
            [Tooltip("The interval of point marks(ticks, grid lines, labels) in axis units, interval will be automatically set when this value < 1")]
            public int interval = -1;
            [Tooltip("This option sets the approximate axis division. Not applicable to categorized axis.")]
            public int axisDivision = 4;
            [Tooltip("The maximum value of the axis when 'auto axis values' is disabled.")]
            public float min = 0.0f;
            [Tooltip("The maximum value of the axis when 'auto axis values' is disabled.")]
            public float max = 100.0f;
            [Tooltip("Min padding along axis")]
            public float minPadding = 0.0f;
            [Tooltip("Max padding along axis")]
            public float maxPadding = 10.0f;
        }

        [System.Serializable]
        public class YAxis
        {
            [Tooltip("Mirror the axis")]
            public bool mirrored = false;

            [Header("Grid")]
            [Tooltip("Show/Hide axis line")]
            public bool enableAxisLine = true;
            [Tooltip("Color of axis line")]
            public Color axisLineColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            [Tooltip("Width of axis line")]
            public float axisLineWidth = 2;
            [Tooltip("Show/Hide grid lines")]
            public bool enableGridLine = false;
            [Tooltip("Color of grid lines")]
            public Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            [Tooltip("Width of grid lines")]
            public float gridLineWidth = 1;
            [Tooltip("Show/Hide ticks")]
            public bool enableTick = false;
            [Tooltip("Color of ticks")]
            public Color tickColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            [Tooltip("Width/Length of ticks")]
            public Vector2 tickSize = new Vector2(2.0f, 4.0f);

            [Header("Title and labels")]
            [Tooltip("Show/hide y axis title")]
            public bool enableTitle = false;
            [Tooltip("Y axis title content")]
            public string title = "yAxis";
            [Tooltip("Title text options")]
            public ChartTextOptions titleOption = new ChartTextOptions(new Color(0.2f, 0.2f, 0.2f, 1.0f), null, 14);
            [Tooltip("Show/hide y axis labels")]
            public bool enableLabel = true;
            [Tooltip("Display absolute label values")]
            public bool absoluteValue = false;
            [Tooltip("Label format string, keywords will be replaced while other characters remain the same, useful for adding unit. " +
                "'{value}' will be replaced with label value. ")]
            public string labelFormat = "{value}";
            [Tooltip("Label numeric format string, it is a C# standard numeric format string. Leave it empty for auto numeric format")]
            public string labelNumericFormat = "";
            [Tooltip("Label text options")]
            public ChartTextOptions labelOption = new ChartTextOptions(new Color(0.2f, 0.2f, 0.2f, 1.0f), null, 12);

            [Header("Values")]
            [Tooltip("Automatically caculate min/max axis values. If disabled, axis values will be determined by 'min' and 'max'")]
            public bool autoAxisValues = true;
            [Tooltip("Axis values always start from zero when 'auto axis values' is enabled")]
            public bool startFromZero = true;
            [Tooltip("This option sets the approximate axis division")]
            public int axisDivision = 4;
            [Tooltip("The maximum value of the axis when 'auto axis values' is disabled")]
            public float min = 0.0f;
            [Tooltip("The maximum value of the axis when 'auto axis values' is disabled")]
            public float max = 100.0f;
            [Tooltip("Min padding along the axis")]
            public float minPadding = 0.0f;
            [Tooltip("Max padding along the axis")]
            public float maxPadding = 10.0f;
        }

        [System.Serializable]
        public class Pane
        {
            [Tooltip("Show/Hide outer border")]
            public bool enableOuterBorder = true;
            [Tooltip("Color of outer border")]
            public Color outerBorderColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            [Tooltip("Width of outer border")]
            public float outerBorderWidth = 2;
            [Tooltip("Show/Hide inner border")]
            public bool enableInnerBorder = true;
            [Tooltip("Color of inner border")]
            public Color innerBorderColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            [Tooltip("Width of inner border")]
            public float innerBorderWidth = 2;
            [Tooltip("Inner size of the pane")]
            [Range(0.0f, 1.0f)] public float innerSize = 0.0f;
            [Tooltip("Outer size of the pane")]
            [Range(0.0f, 1.0f)] public float outerSize = 1.0f;
            [Tooltip("Start angle")]
            public float startAngle = 0.0f;
            [Tooltip("End angle")]
            public float endAngle = 360.0f;
            [Tooltip("Semicircle mode. Start angle and end angle will be restricted between -90 degrees and 90 degrees")]
            public bool semicircle = false;
        }

        [System.Serializable]
        public class Tooltip
        {
            [Tooltip("Enable/disable tooltip when mouse is hovering chart items")]
            public bool enable = true;
            [Tooltip("Share tooltip for all series in current category or display tooltip for individual series")]
            public bool share = true;
            [Tooltip("Display absolute data values")]
            public bool absoluteValue = false;
            [Tooltip("Tooltip header format string, keywords will be replaced while other characters remain the same. " +
                "'{category}' will be replaced with current category.")]
            public string headerFormat = "{category}";
            [Tooltip("Tooltip point format string, keywords will be replaced while other characters remain the same. " +
                "'{series.name}' will be replaced with series name. " +
                "'{data.value}' will be replaced with data value. " +
                "'{data.percentage}' will be replaced with data percentage in current category. ")]
            public string pointFormat = "{series.name}: {data.value}";
            [Tooltip("Tooltip point numeric format string, it is a C# standard numeric format string. Leave it empty for auto numeric format")]
            public string pointNumericFormat = "";
            [Tooltip("Tooltip text options")]
            public ChartTextOptions textOption = new ChartTextOptions(new Color(0.9f, 0.9f, 0.9f, 1.0f), null, 14);
            [Tooltip("Color of tooltip background")]
            public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        }

        [System.Serializable]
        public class Legend
        {
            [Tooltip("Show/hide chart legends")]
            public bool enable = true;
            [Tooltip("Legend alignment position")]
            public TextAnchor alignment = TextAnchor.LowerCenter;
            [Tooltip("Horizontal or vertical layout")]
            public RectTransform.Axis itemLayout = RectTransform.Axis.Horizontal;
            [Tooltip("Number of rows for horizontal layout")]
            public int horizontalRows = 1;
            [Tooltip("Label format string, keywords will be replaced while other characters remain the same. " +
                "'{series.name}' will be replaced with series name. " +
                "'{data.value}' will be replaced with data value (if applicable). " +
                "'{data.percentage}' will be replaced with data percentage in current category (if applicable). ")]
            public string format = "{series.name}";
            [Tooltip("Label numeric format string, it is a C# standard numeric format string. Leave it empty for auto numeric format")]
            public string numericFormat = "";
            [Tooltip("Legend text options")]
            public ChartTextOptions textOption = new ChartTextOptions(new Color(0.2f, 0.2f, 0.2f, 1.0f), null, 14);
            [Tooltip("Enable/Disable legend icon")]
            public bool enableIcon = true;
            [Tooltip("Legend icon image")]
            public Sprite[] iconImage = null;
            [Tooltip("Color of legend background")]
            public Color backgroundColor = Color.clear;
            [Tooltip("Color when legend is highlighted")]
            public Color highlightColor = new Color(0.8f, 0.8f, 0.8f, 0.7f);
            [Tooltip("Color when legend is turned off")]
            public Color dimmedColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }

        [System.Serializable]
        public class Label
        {
            [Tooltip("Enable/disable label of chart data")]
            public bool enable = false;
            [Tooltip("Display absolute data values")]
            public bool absoluteValue = false;
            [Tooltip("Label format string, keywords will be replaced while other characters remain the same. " +
                "'{series.name}' will be replaced with series name. " +
                "'{data.value}' will be replaced with data value. " +
                "'{data.percentage}' will be replaced with data percentage in current category. ")]
            public string format = "{data.value}";
            [Tooltip("Label numeric format string, it is a C# standard numeric format string. Leave it empty for auto numeric format")]
            public string numericFormat = "";
            [Tooltip("Label text options")]
            public ChartTextOptions textOption = new ChartTextOptions(new Color(0.2f, 0.2f, 0.2f, 1.0f), null, 14);
            [Tooltip("Label anchored position in the chart item, 0.0/0.5/1.0 indicates beginning/middle/end of the item")]
            public float anchoredPosition = 1.0f;
            [Tooltip("Label offset distance from the chart item, positive/negative value will move label away/toward the chart center")]
            public float offset = 12.0f;
            [Tooltip("Label rotation")]
            public float rotation = 0.0f;
            [Tooltip("Adjust pie chart size to fit with labels (only applicable for pie chart)")]
            public bool bestFit = true;
        }

        [Tooltip("General chart plot options")]
        public PlotOptions plotOptions = new PlotOptions();
        [Tooltip("Chart title options")]
        public Title title = new Title();
        [Tooltip("X-axis options")]
        public XAxis xAxis = new XAxis();
        [Tooltip("Y-axis options")]
        public YAxis yAxis = new YAxis();
        [Tooltip("Plot options for circle pane")]
        public Pane pane = new Pane();
        [Tooltip("Tootip options")]
        public Tooltip tooltip = new Tooltip();
        [Tooltip("Legend options")]
        public Legend legend = new Legend();
        [Tooltip("Label options")]
        public Label label = new Label();

        private void Reset()
        {
#if CHART_TMPRO
            plotOptions.generalFont = Resources.Load("Fonts & Materials/LiberationSans SDF", typeof(TMP_FontAsset)) as TMP_FontAsset;
#else
            plotOptions.generalFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            legend.iconImage = new Sprite[1];
            legend.iconImage[0] = Resources.Load<Sprite>("Images/Chart_Circle_128x128");
        }
    }
}
