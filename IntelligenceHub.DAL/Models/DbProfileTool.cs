using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    /// <summary>
    /// Join entity linking profiles and tools.
    /// </summary>
    [Table("ProfileTools")]
    public class DbProfileTool
    {
        /// <summary>
        /// Foreign key referencing the profile.
        /// </summary>
        public int ProfileID { get; set; }
        /// <summary>
        /// Navigation property to the related profile.
        /// </summary>
        public DbProfile Profile { get; set; }
        /// <summary>
        /// Foreign key referencing the tool.
        /// </summary>
        public int ToolID { get; set; }
        /// <summary>
        /// Navigation property to the related tool.
        /// </summary>
        public DbTool Tool { get; set; }
    }
}
