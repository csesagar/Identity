using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class ForgotPasswordModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Passwordid { get; set; }
        [Required]
        [Display(Name = "User Name")]
        [RegularExpression("[A-Za-z0-9-._@+]*")]
        public string? Name { get; set; }
    }
}
