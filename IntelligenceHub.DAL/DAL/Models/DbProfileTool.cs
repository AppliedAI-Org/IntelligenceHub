using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    [Table("ProfileTools")]
    public class DbProfileTool
    {
        public int ProfileID { get; set; }
        public DbProfile Profile { get; set; }
        public int ToolID { get; set; }
        public DbTool Tool { get; set; }
    }
}
