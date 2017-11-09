namespace WebApplication1.Services
{
    public class MailConfig
    {
        public string SmtpServer
        {
            get;
            set;
        }
        /// <summary>
        /// 默认端口25（设为-1让系统自动设置）
        /// </summary>
        public int SmtpPort
        {
            get;
            set;
        }
        /// <summary>
        /// 地址
        /// </summary>
        public string EmailAddress
        {
            get;
            set;
        }
        /// <summary>
        /// 账号
        /// </summary>
        public string EmailUserName
        {
            get;
            set;
        }
        /// <summary>
        /// 密码
        /// </summary>
        public string EmailPwd
        {
            get;
            set;
        }
        /// <summary>
        /// 是否使用SSL连接
        /// </summary>
        public bool EnableSSL
        {
            get;
            set;
        }
    }
}
