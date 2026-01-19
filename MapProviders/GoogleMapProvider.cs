using System;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;
using GMap.NET.CacheProviders;

namespace TaxiWPF.MapProviders
{
    public class GoogleMapProvider : GMapProvider
    {
        public static readonly GoogleMapProvider Instance;

        private readonly Guid id = new Guid("B13C5A12-1B82-4C5E-8F3A-9D7E6F5A4B3D");
        public override Guid Id
        {
            get { return id; }
        }

        private readonly string name = "GoogleMap";
        public override string Name
        {
            get { return name; }
        }

        private GMapProvider[] overlays;
        public override GMapProvider[] Overlays
        {
            get
            {
                if (overlays == null)
                {
                    overlays = new GMapProvider[] { this };
                }
                return overlays;
            }
        }

        private readonly PureProjection projection = MercatorProjection.Instance;
        public override PureProjection Projection
        {
            get { return projection; }
        }

        static GoogleMapProvider()
        {
            Instance = new GoogleMapProvider();
        }

        public GoogleMapProvider()
        {
            MaxZoom = 20;
            MinZoom = 1;
        }

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            string url = MakeTileImageUrl(pos, zoom, LanguageStr);
            return GetTileImageUsingHttp(url);
        }

        string MakeTileImageUrl(GPoint pos, int zoom, string language)
        {
            // Google Maps tile server URL (roadmap)
            // Примечание: Google Maps требует API ключ для коммерческого использования
            // Для тестирования можно использовать без ключа, но с ограничениями
            var server = (pos.X + pos.Y) % 4;
            return string.Format("https://mt{0}.google.com/vt/lyrs=m&x={1}&y={2}&z={3}", server, pos.X, pos.Y, zoom);
        }
    }
}

