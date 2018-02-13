using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace com.concentrix.wahit.chatbot.Configuration
{
    /// <summary>
    /// Custom configuration section for Chat bot configuration. Example:
    /// <botConfig>
    ///    <XMPP server="spark.concentrix.com" username="wahitdesk" />
    /// </botConfig>
    /// </summary>
    public class BotConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("XMPP")]
        public XMPPElement XMPP
        {
            get
            {
                return (XMPPElement)base["XMPP"];
            }
        }
    }

    /// <summary>
    /// Custom configuration element XMPP. Example:
    /// </summary>
    public class XMPPElement : ConfigurationElement
    {
        [ConfigurationProperty("server", IsRequired = true)]
        public string Server
        {
            get
            {
                return (string)this["server"];
            }
        }

        /// <summary>
        /// Jabber Username for bot to connect with.
        /// </summary>
        [ConfigurationProperty("username", IsRequired = true)]
        public string Username
        {
            get
            {
                return (string)this["username"];
            }
        }

        /// <summary>
        /// Jabber Password for bot to connect with.
        /// </summary>
        [ConfigurationProperty("password", IsRequired = false)]
        public string Password
        {
            get
            {
                return (string)this["password"];
            }
        }
    }
}
