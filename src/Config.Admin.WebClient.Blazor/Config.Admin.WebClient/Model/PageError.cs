namespace pote.Config.Admin.WebClient.Model
{
    public class PageError
    {
        private Action _stateHasChanged = null!;
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        
        public void OnError(string errorMessage, Exception _)
        {
            IsError = true;
            ErrorMessage = errorMessage;
            _stateHasChanged.Invoke();
        }

        public void Reset()
        {
            IsError = false;
            ErrorMessage = string.Empty;
            _stateHasChanged.Invoke();
        }

        public void SetStateHasChangedCallback(Action stateHasChanged)
        {
            _stateHasChanged = stateHasChanged;
        }
    }
}