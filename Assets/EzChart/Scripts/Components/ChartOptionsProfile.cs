using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    [CreateAssetMenu(fileName = "ChartOptionsProfile", menuName = "EzChart/ChartOptionsProfile", order = 1)]
    public class ChartOptionsProfile : ScriptableObject
    {
        [System.Serializable]
        public struct ChartTextOptions
        {
            public bool color;
            public bool fontSize;
            public bool font;
            public bool customizedText;

            public void LoadPreset(ChartOptions.ChartTextOptions preset, ref ChartOptions.ChartTextOptions value)
            {
                if (color) value.color = preset.color;
                if (fontSize) value.fontSize = preset.fontSize;
                if (font) value.font = preset.font;
                if (customizedText) value.customizedText = preset.customizedText;
            }
        }

        [System.Serializable]
        public struct BarChartOptions
        {
            public bool colorByCategories;
            public bool barWidth;
            public bool itemSeparation;
            public bool enableBarBackground;
            public bool barBackgroundWidth;
            public bool barBackgroundColor;

            public void LoadPreset(ChartOptions.BarChartOptions preset, ref ChartOptions.BarChartOptions value)
            {
                if (colorByCategories) value.colorByCategories = preset.colorByCategories;
                if (barWidth) value.barWidth = preset.barWidth;
                if (itemSeparation) value.itemSeparation = preset.itemSeparation;
                if (enableBarBackground) value.enableBarBackground = preset.enableBarBackground;
                if (barBackgroundWidth) value.barBackgroundWidth = preset.barBackgroundWidth;
                if (barBackgroundColor) value.barBackgroundColor = preset.barBackgroundColor;
            }
        }

        [System.Serializable]
        public struct LineChartOptions
        {
            public bool pointSize;
            public bool enableLine;
            public bool lineWidth;
            public bool enableShade;
            public bool shadeOpacity;
            public bool enablePointOutline;
            public bool pointOutlineWidth;
            public bool pointOutlineColor;
            public bool splineCurve;

            public void LoadPreset(ChartOptions.LineChartOptions preset, ref ChartOptions.LineChartOptions value)
            {
                if (pointSize) value.pointSize = preset.pointSize;
                if (enableLine) value.enableLine = preset.enableLine;
                if (lineWidth) value.lineWidth = preset.lineWidth;
                if (enableLine) value.enableLine = preset.enableLine;
                if (shadeOpacity) value.shadeOpacity = preset.shadeOpacity;
                if (enablePointOutline) value.enablePointOutline = preset.enablePointOutline;
                if (pointOutlineWidth) value.pointOutlineWidth = preset.pointOutlineWidth;
                if (pointOutlineColor) value.pointOutlineColor = preset.pointOutlineColor;
                if (splineCurve) value.splineCurve = preset.splineCurve;
            }
        }

        [System.Serializable]
        public struct PieChartOptions
        {
            public bool itemSeparation;

            public void LoadPreset(ChartOptions.PieChartOptions preset, ref ChartOptions.PieChartOptions value)
            {
                if (itemSeparation) value.itemSeparation = preset.itemSeparation;
            }
        }

        [System.Serializable]
        public struct RoseChartOptions
        {
            public bool colorByCategories;
            public bool barWidth;
            public bool itemSeparation;

            public void LoadPreset(ChartOptions.RoseChartOptions preset, ref ChartOptions.RoseChartOptions value)
            {
                if (colorByCategories) value.colorByCategories = preset.colorByCategories;
                if (barWidth) value.barWidth = preset.barWidth;
                if (itemSeparation) value.itemSeparation = preset.itemSeparation;
            }
        }

        [System.Serializable]
        public struct RadarChartOptions
        {
            public bool pointSize;
            public bool enableLine;
            public bool lineWidth;
            public bool enableShade;
            public bool shadeOpacity;
            public bool enablePointOutline;
            public bool pointOutlineWidth;
            public bool pointOutlineColor;
            public bool circularGrid;

            public void LoadPreset(ChartOptions.RadarChartOptions preset, ref ChartOptions.RadarChartOptions value)
            {
                if (pointSize) value.pointSize = preset.pointSize;
                if (enableLine) value.enableLine = preset.enableLine;
                if (lineWidth) value.lineWidth = preset.lineWidth;
                if (enableLine) value.enableLine = preset.enableLine;
                if (shadeOpacity) value.shadeOpacity = preset.shadeOpacity;
                if (enablePointOutline) value.enablePointOutline = preset.enablePointOutline;
                if (pointOutlineWidth) value.pointOutlineWidth = preset.pointOutlineWidth;
                if (pointOutlineColor) value.pointOutlineColor = preset.pointOutlineColor;
                if (circularGrid) value.circularGrid = preset.circularGrid;
            }
        }

        [System.Serializable]
        public struct GaugeOptions
        {
            public bool pointerLengthScale;
            public bool pointerWidth;
            public bool pointerColor;
            public bool bands;
            public bool bandWidth;

            public void LoadPreset(ChartOptions.GaugeOptions preset, ref ChartOptions.GaugeOptions value)
            {
                if (pointerLengthScale) value.pointerLengthScale = preset.pointerLengthScale;
                if (pointerWidth) value.pointerWidth = preset.pointerWidth;
                if (pointerColor) value.pointerColor = preset.pointerColor;
                if (bandWidth) value.bandWidth = preset.bandWidth;
                if (bands)
                {
                    value.bands = new ChartOptions.BandOptions[preset.bands.Length];
                    for (int i = 0; i < value.bands.Length; ++i) value.bands[i] = preset.bands[i];
                }
            }
        }

        [System.Serializable]
        public struct SolidGaugeOptions
        {
            public bool barWidth;
            public bool enableBarBackground;
            public bool barBackgroundWidth;
            public bool barBackgroundColor;

            public void LoadPreset(ChartOptions.SolidGaugeOptions preset, ref ChartOptions.SolidGaugeOptions value)
            {
                if (barWidth) value.barWidth = preset.barWidth;
                if (enableBarBackground) value.enableBarBackground = preset.enableBarBackground;
                if (barBackgroundWidth) value.barBackgroundWidth = preset.barBackgroundWidth;
                if (barBackgroundColor) value.barBackgroundColor = preset.barBackgroundColor;
            }
        }

        [System.Serializable]
        public struct PlotOptions
        {
            public bool cultureInfoName;
            public bool dataColor;
            public bool generalFont;
            public bool inverted;
            public bool mouseTracking;
            public bool frontGrid;
            public bool columnStacking;
            public bool itemHighlightColor;
            public bool enableBackground;
            public bool backgroundColor;
            public BarChartOptions barChartOption;
            public LineChartOptions lineChartOption;
            public PieChartOptions pieChartOption;
            public RoseChartOptions roseChartOption;
            public RadarChartOptions radarChartOption;
            public GaugeOptions gaugeOption;
            public SolidGaugeOptions solidGaugeOption;

            public void LoadPreset(ChartOptions.PlotOptions preset, ref ChartOptions.PlotOptions value)
            {
                if (cultureInfoName) value.cultureInfoName = preset.cultureInfoName;
                if (dataColor)
                {
                    value.dataColor = new Color[preset.dataColor.Length];
                    for (int i = 0; i < value.dataColor.Length; ++i) value.dataColor[i] = preset.dataColor[i];
                }
                if (generalFont) value.generalFont = preset.generalFont;
                if (inverted) value.inverted = preset.inverted;
                if (mouseTracking) value.mouseTracking = preset.mouseTracking;
                if (frontGrid) value.frontGrid = preset.frontGrid;
                if (columnStacking) value.columnStacking = preset.columnStacking;
                if (itemHighlightColor) value.itemHighlightColor = preset.itemHighlightColor;
                if (enableBackground) value.enableBackground = preset.enableBackground;
                if (backgroundColor) value.backgroundColor = preset.backgroundColor;
                barChartOption.LoadPreset(preset.barChartOption, ref value.barChartOption);
                lineChartOption.LoadPreset(preset.lineChartOption, ref value.lineChartOption);
                pieChartOption.LoadPreset(preset.pieChartOption, ref value.pieChartOption);
                roseChartOption.LoadPreset(preset.roseChartOption, ref value.roseChartOption);
                radarChartOption.LoadPreset(preset.radarChartOption, ref value.radarChartOption);
                gaugeOption.LoadPreset(preset.gaugeOption, ref value.gaugeOption);
                solidGaugeOption.LoadPreset(preset.solidGaugeOption, ref value.solidGaugeOption);
            }
        }

        [System.Serializable]
        public struct Title
        {
            public bool enableMainTitle;
            public bool mainTitle;
            public ChartTextOptions mainTitleOption;
            public bool enableSubTitle;
            public bool subTitle;
            public ChartTextOptions subTitleOption;

            public void LoadPreset(ChartOptions.Title preset, ref ChartOptions.Title value)
            {
                if (enableMainTitle) value.enableMainTitle = preset.enableMainTitle;
                if (mainTitle) value.mainTitle = preset.mainTitle;
                mainTitleOption.LoadPreset(preset.mainTitleOption, ref value.mainTitleOption);
                if (enableSubTitle) value.enableSubTitle = preset.enableSubTitle;
                if (subTitle) value.subTitle = preset.subTitle;
                subTitleOption.LoadPreset(preset.subTitleOption, ref value.subTitleOption);
            }
        }

        [System.Serializable]
        public struct XAxis
        {
            [Header("Grid")]
            public bool enableAxisLine;
            public bool axisLineColor;
            public bool axisLineWidth;
            public bool enableGridLine;
            public bool gridLineColor;
            public bool gridLineWidth;
            public bool enableTick;
            public bool tickColor;
            public bool tickSize;
            public bool reversed;
            [Header("Title and labels")]
            public bool enableTitle;
            public bool title;
            public ChartTextOptions titleOption;
            public bool enableLabel;
            public bool labelFormat;
            public bool labelNumericFormat;
            public ChartTextOptions labelOption;
            public bool autoRotateLabel;
            [Header("Values")]
            public bool type;
            public bool autoAxisValues;
            public bool interval;
            public bool axisDivision;
            public bool min;
            public bool max;
            public bool minPadding;
            public bool maxPadding;

            public void LoadPreset(ChartOptions.XAxis preset, ref ChartOptions.XAxis value)
            {
                if (enableAxisLine) value.enableAxisLine = preset.enableAxisLine;
                if (axisLineColor) value.axisLineColor = preset.axisLineColor;
                if (axisLineWidth) value.axisLineWidth = preset.axisLineWidth;
                if (enableGridLine) value.enableGridLine = preset.enableGridLine;
                if (gridLineColor) value.gridLineColor = preset.gridLineColor;
                if (gridLineWidth) value.gridLineWidth = preset.gridLineWidth;
                if (enableTick) value.enableTick = preset.enableTick;
                if (tickColor) value.tickColor = preset.tickColor;
                if (tickSize) value.tickSize = preset.tickSize;
                if (minPadding) value.minPadding = preset.minPadding;
                if (maxPadding) value.maxPadding = preset.maxPadding;
                if (reversed) value.reversed = preset.reversed;
                if (enableTitle) value.enableLabel = preset.enableLabel;
                if (title) value.title = preset.title;
                titleOption.LoadPreset(preset.titleOption, ref value.titleOption);
                if (enableLabel) value.enableLabel = preset.enableLabel;
                if (labelFormat) value.labelFormat = preset.labelFormat;
                if (labelNumericFormat) value.labelNumericFormat = preset.labelNumericFormat;
                labelOption.LoadPreset(preset.labelOption, ref value.labelOption);
                if (autoRotateLabel) value.autoRotateLabel = preset.autoRotateLabel;
                if (type) value.type = preset.type;
                if (autoAxisValues) value.autoAxisValues = preset.autoAxisValues;
                if (interval) value.interval = preset.interval;
                if (axisDivision) value.axisDivision = preset.axisDivision;
                if (min) value.min = preset.min;
                if (max) value.max = preset.max;
            }
        }

        [System.Serializable]
        public struct YAxis
        {
            [Header("Grid")]
            public bool enableAxisLine;
            public bool axisLineColor;
            public bool axisLineWidth;
            public bool enableGridLine;
            public bool gridLineColor;
            public bool gridLineWidth;
            public bool enableTick;
            public bool tickColor;
            public bool tickSize;
            [Header("Title and labels")]
            public bool enableTitle;
            public bool title;
            public ChartTextOptions titleOption;
            public bool enableLabel;
            public bool absoluteValue;
            public bool labelFormat;
            public bool labelNumericFormat;
            public ChartTextOptions labelOption;
            [Header("Values")]
            public bool autoAxisValues;
            public bool startFromZero;
            public bool axisDivision;
            public bool min;
            public bool max;
            public bool minPadding;
            public bool maxPadding;

            public void LoadPreset(ChartOptions.YAxis preset, ref ChartOptions.YAxis value)
            {
                if (enableAxisLine) value.enableAxisLine = preset.enableAxisLine;
                if (axisLineColor) value.axisLineColor = preset.axisLineColor;
                if (axisLineWidth) value.axisLineWidth = preset.axisLineWidth;
                if (enableGridLine) value.enableGridLine = preset.enableGridLine;
                if (gridLineColor) value.gridLineColor = preset.gridLineColor;
                if (gridLineWidth) value.gridLineWidth = preset.gridLineWidth;
                if (enableTick) value.enableTick = preset.enableTick;
                if (tickColor) value.tickColor = preset.tickColor;
                if (tickSize) value.tickSize = preset.tickSize;
                if (minPadding) value.minPadding = preset.minPadding;
                if (maxPadding) value.maxPadding = preset.maxPadding;
                if (enableTitle) value.enableLabel = preset.enableLabel;
                if (title) value.title = preset.title;
                titleOption.LoadPreset(preset.titleOption, ref value.titleOption);
                if (enableLabel) value.enableLabel = preset.enableLabel;
                if (absoluteValue) value.absoluteValue = preset.absoluteValue;
                if (labelFormat) value.labelFormat = preset.labelFormat;
                if (labelNumericFormat) value.labelNumericFormat = preset.labelNumericFormat;
                labelOption.LoadPreset(preset.labelOption, ref value.labelOption);
                if (autoAxisValues) value.autoAxisValues = preset.autoAxisValues;
                if (axisDivision) value.axisDivision = preset.axisDivision;
                if (startFromZero) value.startFromZero = preset.startFromZero;
                if (min) value.min = preset.min;
                if (max) value.max = preset.max;
            }
        }

        [System.Serializable]
        public struct Pane
        {
            public bool enableOuterBorder;
            public bool outerBorderColor;
            public bool outerBorderWidth;
            public bool enableInnerBorder;
            public bool innerBorderColor;
            public bool innerBorderWidth;
            public bool innerSize;
            public bool outerSize;
            public bool startAngle;
            public bool endAngle;
            public bool semicircle;

            public void LoadPreset(ChartOptions.Pane preset, ref ChartOptions.Pane value)
            {
                if (enableOuterBorder) value.enableOuterBorder = preset.enableOuterBorder;
                if (outerBorderColor) value.outerBorderColor = preset.outerBorderColor;
                if (outerBorderWidth) value.outerBorderWidth = preset.outerBorderWidth;
                if (enableInnerBorder) value.enableInnerBorder = preset.enableInnerBorder;
                if (innerBorderColor) value.innerBorderColor = preset.innerBorderColor;
                if (innerBorderWidth) value.innerBorderWidth = preset.innerBorderWidth;
                if (innerSize) value.innerSize = preset.innerSize;
                if (outerSize) value.outerSize = preset.outerSize;
                if (startAngle) value.startAngle = preset.startAngle;
                if (endAngle) value.endAngle = preset.endAngle;
                if (semicircle) value.semicircle = preset.semicircle;
            }
        }

        [System.Serializable]
        public struct Tooltip
        {
            public bool enable;
            public bool share;
            public bool absoluteValue;
            public bool headerFormat;
            public bool pointFormat;
            public bool pointNumericFormat;
            public ChartTextOptions textOption;
            public bool backgroundColor;

            public void LoadPreset(ChartOptions.Tooltip preset, ref ChartOptions.Tooltip value)
            {
                if (enable) value.enable = preset.enable;
                if (share) value.share = preset.share;
                if (absoluteValue) value.absoluteValue = preset.absoluteValue;
                if (headerFormat) value.headerFormat = preset.headerFormat;
                if (pointFormat) value.pointFormat = preset.pointFormat;
                if (pointNumericFormat) value.pointNumericFormat = preset.pointNumericFormat;
                textOption.LoadPreset(preset.textOption, ref value.textOption);
                if (backgroundColor) value.backgroundColor = preset.backgroundColor;
            }
        }

        [System.Serializable]
        public struct Legend
        {
            public bool enable;
            public bool alignment;
            public bool itemLayout;
            public bool horizontalRows;
            public bool format;
            public bool numericFormat;
            public ChartTextOptions textOption;
            public bool enableIcon;
            public bool iconImage;
            public bool backgroundColor;
            public bool highlightColor;
            public bool dimmedColor;

            public void LoadPreset(ChartOptions.Legend preset, ref ChartOptions.Legend value)
            {
                if (enable) value.enable = preset.enable;
                if (alignment) value.alignment = preset.alignment;
                if (itemLayout) value.itemLayout = preset.itemLayout;
                if (horizontalRows) value.horizontalRows = preset.horizontalRows;
                if (format) value.format = preset.format;
                if (numericFormat) value.numericFormat = preset.numericFormat;
                textOption.LoadPreset(preset.textOption, ref value.textOption);
                if (enableIcon) value.enableIcon = preset.enableIcon;
                if (iconImage)
                {
                    value.iconImage = new Sprite[preset.iconImage.Length];
                    for (int i = 0; i < value.iconImage.Length; ++i) value.iconImage[i] = preset.iconImage[i];
                }
                if (backgroundColor) value.backgroundColor = preset.backgroundColor;
                if (highlightColor) value.highlightColor = preset.highlightColor;
                if (dimmedColor) value.dimmedColor = preset.dimmedColor;
            }
        }

        [System.Serializable]
        public struct Label
        {
            public bool enable;
            public bool absoluteValue;
            public bool format;
            public bool numericFormat;
            public ChartTextOptions textOption;
            public bool anchoredPosition;
            public bool offset;
            public bool rotation;
            public bool bestFit;

            public void LoadPreset(ChartOptions.Label preset, ref ChartOptions.Label value)
            {
                if (enable) value.enable = preset.enable;
                if (absoluteValue) value.absoluteValue = preset.absoluteValue;
                if (format) value.format = preset.format;
                if (numericFormat) value.numericFormat = preset.numericFormat;
                textOption.LoadPreset(preset.textOption, ref value.textOption);
                if (anchoredPosition) value.anchoredPosition = preset.anchoredPosition;
                if (offset) value.offset = preset.offset;
                if (rotation) value.rotation = preset.rotation;
                if (bestFit) value.bestFit = preset.bestFit;
            }
        }

        public PlotOptions plotOptions;
        public Title title;
        public XAxis xAxis;
        public YAxis yAxis;
        public Pane pane;
        public Tooltip tooltip;
        public Legend legend;
        public Label label;

        public void LoadPreset(ChartOptionsPreset preset, ref ChartOptions value)
        {
            plotOptions.LoadPreset(preset.plotOptions, ref value.plotOptions);
            title.LoadPreset(preset.title, ref value.title);
            xAxis.LoadPreset(preset.xAxis, ref value.xAxis);
            yAxis.LoadPreset(preset.yAxis, ref value.yAxis);
            pane.LoadPreset(preset.pane, ref value.pane);
            tooltip.LoadPreset(preset.tooltip, ref value.tooltip);
            legend.LoadPreset(preset.legend, ref value.legend);
            label.LoadPreset(preset.label, ref value.label);
        }

        public void LoadPreset(ChartOptions preset, ref ChartOptions value)
        {
            plotOptions.LoadPreset(preset.plotOptions, ref value.plotOptions);
            title.LoadPreset(preset.title, ref value.title);
            xAxis.LoadPreset(preset.xAxis, ref value.xAxis);
            yAxis.LoadPreset(preset.yAxis, ref value.yAxis);
            pane.LoadPreset(preset.pane, ref value.pane);
            tooltip.LoadPreset(preset.tooltip, ref value.tooltip);
            legend.LoadPreset(preset.legend, ref value.legend);
            label.LoadPreset(preset.label, ref value.label);
        }
    }
}