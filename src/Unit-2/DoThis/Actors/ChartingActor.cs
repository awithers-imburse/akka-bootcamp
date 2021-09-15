using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;
using System.Windows.Forms;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor
    {
        /// <summary>
        /// Maximum number of points we will allow in a series
        /// </summary>
        public const int MaxPoints = 250;

        /// <summary>
        /// Incrementing counter we use to plot along the X-axis
        /// </summary>
        private int _xPosCounter;

        private readonly Chart _chart;
        private readonly Button _pauseButton;
        private Dictionary<string, Series> _seriesIndex;

        public ChartingActor(Chart chart, Button pauseButton) 
            : this(chart, pauseButton, new Dictionary<string, Series>())
        { }

        public ChartingActor(
            Chart chart,
            Button pauseButton,
            Dictionary<string, Series> seriesIndex)
        {
            _chart = chart;
            _pauseButton = pauseButton;
            _seriesIndex = seriesIndex;

            Charting();
        }

        private void Charting()
        {
            Receive<InitializeChart>(HandleInitialize);
            Receive<AddSeries>(HandleAddSeries);
            Receive<RemoveSeries>(HandleRemoveSeries);
            Receive<Metric>(HandleMetrics);

            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(true);
                BecomeStacked(Paused);
            });
        }

        private void Paused()
        {
            Receive<Metric>(metric => HandleMetricsPaused(metric));
            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(false);
                UnbecomeStacked();
            });
        }

        #region Individual Message Type Handlers

        private void HandleInitialize(InitializeChart ic)
        {
            if (ic.InitialSeries != null)
            {
                //swap the two series out
                _seriesIndex = ic.InitialSeries;
            }

            //delete any existing series
            _chart.Series.Clear();

            // set the axes up
            var area = _chart.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;

            SetChartBoundaries();

            //attempt to render the initial chart
            if (_seriesIndex.Any())
            {
                foreach (var series in _seriesIndex)
                {
                    //force both the chart and the internal index to use the same names
                    series.Value.Name = series.Key;
                    _chart.Series.Add(series.Value);
                }
            }

            SetChartBoundaries();
        }

        private void HandleAddSeries(AddSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series.Name)
                && !_seriesIndex.ContainsKey(series.Series.Name))
            {
                _seriesIndex.Add(series.Series.Name, series.Series);
                _chart.Series.Add(series.Series);
                SetChartBoundaries();
            }
        }

        private void HandleRemoveSeries(RemoveSeries series)
        {
            if (!string.IsNullOrEmpty(series.SeriesName) 
                && _seriesIndex.ContainsKey(series.SeriesName))
            {
                var seriesToRemove = _seriesIndex[series.SeriesName];
                _seriesIndex.Remove(series.SeriesName);
                _chart.Series.Remove(seriesToRemove);
                SetChartBoundaries();
            }
        }

        private void HandleMetrics(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) 
                && _seriesIndex.ContainsKey(metric.Series))
            {
                var series = _seriesIndex[metric.Series];
                series.Points.AddXY(_xPosCounter++, metric.CounterValue);
                while (series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
                SetChartBoundaries();
            }
        }

        private void HandleMetricsPaused(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) 
                && _seriesIndex.ContainsKey(metric.Series))
            {
                var series = _seriesIndex[metric.Series];
                series.Points.AddXY(_xPosCounter++, 0.0d);

                while(series.Points.Count > MaxPoints)
                    series.Points.RemoveAt(0);

                SetChartBoundaries();
            }
        }

        #endregion

        private void SetChartBoundaries()
        {
            var allPoints = _seriesIndex
                .Values
                .SelectMany(series => series.Points)
                .ToList();

            var yValues = allPoints
                .SelectMany(point => point.YValues)
                .ToList();

            double maxAxisX = _xPosCounter;
            double minAxisX = _xPosCounter - MaxPoints;

            var maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;
            var minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;

            if (allPoints.Count > 2)
            {
                var area = _chart.ChartAreas[0];
                area.AxisX.Minimum = minAxisX;
                area.AxisX.Maximum = maxAxisX;
                area.AxisY.Minimum = minAxisY;
                area.AxisY.Maximum = maxAxisY;
            }
        }

        private void SetPauseButtonText(bool paused) => 
            _pauseButton.Text = paused ? "RESUME ->" : "PAUSE ||";

        #region Messages

        public class InitializeChart
        {
            public InitializeChart(Dictionary<string, Series> initialSeries)
            {
                InitialSeries = initialSeries;
            }

            public Dictionary<string, Series> InitialSeries { get; private set; }
        }

        /// <summary>
        /// Add a new <see cref="Series"/> to the chart
        /// </summary>
        public class AddSeries
        {
            public Series Series { get; }

            public AddSeries(Series series)
            {
                Series = series;
            }
        }

        /// <summary>
        /// Remove an existing <see cref="Series"/> from the chart
        /// </summary>
        public class RemoveSeries
        {
            public string SeriesName { get; }

            public RemoveSeries(string seriesName)
            {
                SeriesName = seriesName;
            }
        }
        
        /// <summary>
        /// Toggles the pausing between charts
        /// </summary>
        public class TogglePause { }

        #endregion
    }
}
