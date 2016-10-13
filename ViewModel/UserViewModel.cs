using CSJSONBacklog.Model.Space;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BacklogMaintainer.ViewModel
{
    public class UserViewModel : User
    {
        public bool IsSelected { get; set; }
        public string Memo { get; set; }
    }
}
