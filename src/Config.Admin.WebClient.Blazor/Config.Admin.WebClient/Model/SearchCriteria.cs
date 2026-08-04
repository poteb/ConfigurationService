namespace pote.Config.Admin.WebClient.Model
{
    public class SearchCriteria
    {
        public string SearchText { get; set; } = string.Empty;
        public string SelectedApplication { get; set; } = string.Empty;
        public string SelectedEnvironment { get; set; } = string.Empty;

        public void Reset()
        {
            SearchText = string.Empty;
            SelectedApplication = string.Empty;
            SelectedEnvironment = string.Empty;
        }
    }
}