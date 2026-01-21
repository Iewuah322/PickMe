using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Shapes;
using GMap.NET;
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
            {
                return;
            }

            foreach (var marker in _trafficMarkers)
            {
                _map.Markers.Remove(marker);
            }

            _trafficMarkers.Clear();
        }

        public void SimulateOrder(PointLatLng clientLocation)
        {
            if (!_isRunning || _map == null || clientLocation.IsEmpty)
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
        }
    }
}
