
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Shapes;
using GMap.NET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GMap.NET;
using GMap.NET.MapProviders;

using GMap.NET.WindowsPresentation;

namespace TaxiWPF.Services
{
    public class VirtualTrafficService
    {

        private readonly List<GMapMarker> _trafficMarkers = new List<GMapMarker>();
        private GMapControl _map;
        private bool _isRunning;

        public void Initialize(GMapControl map)
        {
            _map = map;
        }

        public void Start()
        {
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
            if (_map == null)

        private const int DriverCount = 6;
        private const int RouteZoomLevel = 15;
        private readonly Random _random = new Random();
        private readonly List<VirtualDriver> _drivers = new List<VirtualDriver>();
        private GMapControl _mainMap;
        private DispatcherTimer _timer;

        public VirtualTrafficService()
        {
        }

        public VirtualTrafficService(GMapControl mainMap)
        {
            Initialize(mainMap);
        }

        public void Initialize(GMapControl mainMap)
        {
            _mainMap = mainMap;
        }

        public void Start()
        {
            if (_mainMap == null)
            {
                throw new InvalidOperationException("VirtualTrafficService requires a map before starting.");
            }

            if (_timer != null)

            {
                return;
            }


            foreach (var marker in _trafficMarkers)
            {
                _map.Markers.Remove(marker);
            }

            _trafficMarkers.Clear();

            SpawnDrivers();

            _timer = new DispatcherTimer(DispatcherPriority.Background, _mainMap.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer == null)
            {
                return;
            }

            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer = null;

        }

        public void SimulateOrder(PointLatLng clientLocation)
        {

            if (!_isRunning || _map == null || clientLocation.IsEmpty)

            if (_drivers.Count == 0)

            {
                return;
            }


            var marker = new GMapMarker(clientLocation)
            {
                Shape = new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = Brushes.OrangeRed,
                    Stroke = Brushes.DarkRed,
                    StrokeThickness = 2
                }
            };

            _map.Markers.Add(marker);
            _trafficMarkers.Add(marker);

            VirtualDriver closestDriver = null;
            double closestDistance = double.MaxValue;

            foreach (var driver in _drivers)
            {
                var distance = GetDistanceInMeters(driver.Marker.Position, clientLocation);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDriver = driver;
                }
            }

            if (closestDriver == null)
            {
                return;
            }

            closestDriver.IsBusy = true;
            closestDriver.CarShape.Fill = Brushes.Red;
            UpdateRouteForDriver(closestDriver, closestDriver.Marker.Position, clientLocation);
        }

        private void OnTick(object sender, EventArgs e)
        {
            foreach (var driver in _drivers)
            {
                AdvanceDriver(driver);
            }
        }

        private void SpawnDrivers()
        {
            if (_drivers.Count > 0)
            {
                return;
            }

            var center = _mainMap.Position;
            for (int i = 0; i < DriverCount; i++)
            {
                var start = GetRandomNearbyPoint(center);
                var end = GetRandomNearbyPoint(center);
                var rectangle = CreateDriverShape();
                var marker = new GMapMarker(start)
                {
                    Shape = rectangle,
                    ZIndex = 100
                };

                _mainMap.Markers.Add(marker);

                var driver = new VirtualDriver
                {
                    Marker = marker,
                    CarShape = rectangle,
                    Rotation = (RotateTransform)rectangle.RenderTransform
                };

                UpdateRouteForDriver(driver, start, end);
                _drivers.Add(driver);
            }
        }

        private Rectangle CreateDriverShape()
        {
            var rotation = new RotateTransform(0);
            return new Rectangle
            {
                Width = 10,
                Height = 20,
                Fill = Brushes.Black,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = rotation
            };
        }

        private void AdvanceDriver(VirtualDriver driver)
        {
            if (driver.RoutePoints.Count == 0)
            {
                ResetDriverRoute(driver);
                return;
            }

            if (driver.RouteIndex >= driver.RoutePoints.Count - 1)
            {
                if (driver.IsBusy)
                {
                    driver.IsBusy = false;
                    driver.CarShape.Fill = Brushes.Black;
                }

                ResetDriverRoute(driver);
                return;
            }

            var currentPoint = driver.RoutePoints[driver.RouteIndex];
            var nextPoint = driver.RoutePoints[driver.RouteIndex + 1];

            driver.Marker.Position = currentPoint;
            driver.Rotation.Angle = CalculateHeading(currentPoint, nextPoint);
            driver.RouteIndex++;
        }

        private void ResetDriverRoute(VirtualDriver driver)
        {
            var start = driver.Marker.Position;
            var end = GetRandomNearbyPoint(_mainMap.Position);
            UpdateRouteForDriver(driver, start, end);
        }

        private void UpdateRouteForDriver(VirtualDriver driver, PointLatLng start, PointLatLng end)
        {
            var route = OpenStreetMapProvider.Instance.GetRoute(start, end, false, false, RouteZoomLevel);
            if (route == null || route.Points == null || route.Points.Count == 0)
            {
                driver.RoutePoints = new List<PointLatLng> { start, end };
            }
            else
            {
                driver.RoutePoints = route.Points.ToList();
            }

            driver.RouteIndex = 0;
        }

        private PointLatLng GetRandomNearbyPoint(PointLatLng center)
        {
            const double maxOffset = 0.002;
            var latOffset = (_random.NextDouble() - 0.5) * maxOffset;
            var lngOffset = (_random.NextDouble() - 0.5) * maxOffset;
            return new PointLatLng(center.Lat + latOffset, center.Lng + lngOffset);
        }

        private static double CalculateHeading(PointLatLng current, PointLatLng next)
        {
            var deltaLat = next.Lat - current.Lat;
            var deltaLng = next.Lng - current.Lng;
            return Math.Atan2(deltaLat, deltaLng) * 180 / Math.PI;
        }

        private static double GetDistanceInMeters(PointLatLng start, PointLatLng end)
        {
            const double earthRadius = 6371000;
            var lat1 = DegreesToRadians(start.Lat);
            var lat2 = DegreesToRadians(end.Lat);
            var deltaLat = DegreesToRadians(end.Lat - start.Lat);
            var deltaLng = DegreesToRadians(end.Lng - start.Lng);

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2)
                * Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private class VirtualDriver
        {
            public GMapMarker Marker { get; set; }
            public Rectangle CarShape { get; set; }
            public RotateTransform Rotation { get; set; }
            public List<PointLatLng> RoutePoints { get; set; } = new List<PointLatLng>();
            public int RouteIndex { get; set; }
            public bool IsBusy { get; set; }

        }
    }
}
