using BZKQuerySystem.DataAccess;
using System.Collections.Generic;

namespace BZKQuerySystem.Web.Models
{
    public class TablePermissionViewModel
    {
        public TableInfo TableInfo { get; set; }
        public List<UserTablePermission> UserPermissions { get; set; }
    }

    public class UserTablePermission
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string TableName { get; set; }
        public bool CanRead { get; set; }
        public bool CanExport { get; set; }
    }
} 