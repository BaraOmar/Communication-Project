namespace CommunicationProject.ViewModels
{
    public class PathSearchResultViewModel
    {
        public int PathNumber { get; set; }

        public string Description { get; set; } =
            string.Empty;

        public string StartSiteId { get; set; } =
            string.Empty;

        public string EndSiteId { get; set; } =
            string.Empty;

        public string SitePath { get; set; } =
            string.Empty;

        public string LinkPath { get; set; } =
            string.Empty;

        public int LinkCount { get; set; }
    }
}