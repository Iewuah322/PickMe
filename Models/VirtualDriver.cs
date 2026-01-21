using System.Collections.Generic;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace TaxiWPF.Models
{
    public class VirtualDriver
    {
        public int Id { get; set; }
        public PointLatLng Position { get; set; }
        public PointLatLng Destination { get; set; }
        public bool IsBusy { get; set; }
        public GMapMarker Marker { get; set; }
        public IList<PointLatLng> RoutePoints { get; set; } = new List<PointLatLng>();
        public int RouteIndex { get; set; }
        public Brush BodyBrush { get; set; }

        public bool HasRoute => RoutePoints != null && RoutePoints.Count > 0 && RouteIndex < RoutePoints.Count;

        public bool Advance()
        {
            if (!HasRoute)
            {
                return false;
            }

            Position = RoutePoints[RouteIndex];
            if (Marker != null)
            {
                Marker.Position = Position;
            }

            RouteIndex++;
            return true;
        }

        public void ResetRoute(IList<PointLatLng> routePoints, PointLatLng destination)
        {
            RoutePoints = routePoints ?? new List<PointLatLng>();
            RouteIndex = 0;
            Destination = destination;
        }

        public void ClearRoute()
        {
            RoutePoints = new List<PointLatLng>();
            RouteIndex = 0;
        }
    }
}
