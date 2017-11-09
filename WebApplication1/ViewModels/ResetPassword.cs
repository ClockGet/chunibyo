using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage ="密码不能为空")]
        [StringLength(10, ErrorMessage = "{0} 必须至少包含 {2} 个字符。", MinimumLength = 4)]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "确认密码")]
        [Compare("Password", ErrorMessage = "密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; }

        public string Token1 { get; set; }
        public string Token2 { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
    }
}
