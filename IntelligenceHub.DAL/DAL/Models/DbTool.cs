using Newtonsoft.Json;
using IntelligenceHub.Common.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    [TableName("Tools")]
    public class DbTool
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Required { get; set; } = string.Empty;
        public string? ExecutionUrl { get; set; }  
        public string? ExecutionMethod { get; set; }
        public string? ExecutionBase64Key { get; set; }
    }
}
