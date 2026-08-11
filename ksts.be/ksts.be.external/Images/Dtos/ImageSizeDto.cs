namespace ksts.be.external.Images.Dtos
{
    public class ImageSizeDto
    {
        public int PixelWidth { get; set; }

        public int PixelHeight { get; set; }

        public double DpiX { get; set; }

        public double DpiY { get; set; }

        public bool HasDpiMetadata { get; set; }

        public double WidthPoints { get; set; }

        public double HeightPoints { get; set; }
    }
}
