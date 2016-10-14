
namespace BacklogMaintainer.ViewModel
{
    using CSJSONBacklog.Model.Space;
    using System.Collections.Generic;

    public class GroupViewModel : Group
    {
        public GroupViewModel(Group group)
        {
            this.Created = group.Created;
            this.CreatedUser = group.CreatedUser;
            this.DisplayOrder = group.DisplayOrder;
            this.Id = group.Id;
            this.Members = new List<User>(group.Members);
            this.Name = group.Name;
            this.Updated = group.Updated;
            this.UpdatedUser = this.UpdatedUser;
        }

        public bool IsSelected { get; set; }
    }
}
