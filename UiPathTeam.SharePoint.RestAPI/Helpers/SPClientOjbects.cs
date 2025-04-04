using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharePoint
{
    public class UserIdInfo
    {
        public string NameId { get; set; }
        public string NameIdIssuer { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Email { get; set; }
        public string LoginName { get; set; }
        public UserIdInfo UserId { get; set; }
        public PrincipalType PrincipalType { get; set; }
        public bool IsSiteAdmin { get; set; }


    }
    public enum PrincipalType
    {
        None = 0,
        User = 1,
        DistributionList = 2,
        SecurityGroup = 4,
        SharePointGroup = 8,
        All = 0xF
    }

    public class TimeZoneInfo
    {
        public string Description { get; set; }
        public int Id { get; set; }
        public TimeZoneInformation Information { get; set; }
    }

    public class TimeZoneInformation
    {
        public int Bias { get; set; }
        public int DaylightBias { get; set; }
        public int StandardBias { get; set; }
    }

    
}
