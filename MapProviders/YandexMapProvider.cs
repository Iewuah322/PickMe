using System;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;
using GMap.NET.CacheProviders;

namespace TaxiWPF.MapProviders
{
    public class YandexMapProvider : GMapProvider
    {
        public static readonly YandexMapProvider Instance;

        private readonly Guid id = new Guid("A13C5A12-1B82-4C5E-8F3A-9D7E6F5A4B3C");
        public override Guid Id
        {
            get { return id; }
        }

        private readonly string name = "YandexMap";
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

        static YandexMapProvider()
        {
            Instance = new YandexMapProvider();
        }

        public YandexMapProvider()
        {
            MaxZoom = 18;
            MinZoom = 1;
        }

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            string url = MakeTileImageUrl(pos, zoom, LanguageStr);
            return GetTileImageUsingHttp(url);
        }

        string MakeTileImageUrl(GPoint pos, int zoom, string language)
        {
            // Yandex Maps tile server URL
            // Используем схему карты (map) - обычная карта
            return string.Format("https://core-renderer-tiles.maps.yandex.net/tiles?l=map&x={0}&y={1}&z={2}&scale=1&lang=ru_RU", pos.X, pos.Y, zoom);
        }
    }
}

