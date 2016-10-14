
namespace BacklogMaintainer.ViewModel
{
    using CSJSONBacklog.Model.Space;

    public class UserViewModel : User
    {
        public bool IsSelected { get; set; }
        public string Memo { get; set; }
    }
}
