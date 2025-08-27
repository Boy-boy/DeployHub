namespace ImageUploaderApi.Models
{
    public class ConsulOptions
    {
        /// <summary>
        /// 是否开启Consul
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Acl Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 是否允许配置不存在
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// 是否监听配置变更
        /// </summary>
        public bool ReloadOnChange { get; set; }

        /// <summary>
        /// 是否忽略异常
        /// </summary>
        public bool IgnoreException { get; set; }
    }
}
