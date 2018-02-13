using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Configuration;
using System.IO;
using com.concentrix.wahit.chatbot.Configuration;
using agsXMPP;
using AIMLbot;

namespace com.concentrix.wahit.chatbot
{
    public partial class Main : Form
    {
        BotConfigSection _botConfiguration = (BotConfigSection)ConfigurationManager.GetSection("botConfig");
        XmppClientConnection _xmppConnection;
        AIMLChatBot _bot = new AIMLChatBot();
        String[] _sanitizer = { "`", "-", "_", ".", ":", ";", "(", ")", "^", "=", "[", "]", "|", "$", "+", "*", "~", "/", "&", "%", "#", "<", ">", "?", "{", "{", "\'\"" };
        String[] _ithelpdesk = { "lorenz.roxas@spark.concentrix.com", "joan.encarnacion@spark.concentrix.com", "michael.tarinay@spark.concentrix.com" };

        delegate void SetTextCallback(string text);

        private void log(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (chatBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(log);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                logtext(text);
            }
        }

        private void logtext(String message)
        {
            chatBox.Text += message + "\r\n";
            chatBox.Text += "--\r\n";

            chatBox.Refresh();
        }

        private void SetupXMPP()
        {
            // Open connection to Jabber.
            _xmppConnection = new XmppClientConnection();
            _xmppConnection.Server = _botConfiguration.XMPP.Server;
            _xmppConnection.Username = _botConfiguration.XMPP.Username;
            _xmppConnection.Password = _botConfiguration.XMPP.Password;

            
            // If a password was not present in the app.config, ask the console user for it.
            if (_xmppConnection.Password.Length == 0)
            {
                _xmppConnection.Password = CommonManager.GetPasswordFromConsole(_botConfiguration.XMPP.Username);
            }

            log("» Starting the server...");
            _xmppConnection.Open();
                       
            // Setup event handlers.
            _xmppConnection.OnLogin += new ObjectHandler(_xmppConnection_OnLogin);
            _xmppConnection.OnClose += _xmppConnection_OnClose;
            _xmppConnection.OnError += new ErrorHandler(_xmppConnection_OnError);
            _xmppConnection.OnMessage += new agsXMPP.protocol.client.MessageHandler(_xmppConnection_OnMessage);

            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        #region Event Handlers

        private void _xmppConnection_OnLogin(object sender)
        {
            log("» Ashley ChatBot is now online! Listening for connections.");
        }

        private void _xmppConnection_OnClose(object sender)
        {
            log("» Ashley is going to bed... :)");
        }

        private void _xmppConnection_OnError(object sender, Exception ex)
        {
            log(ex.Message);
        }

        public void _xmppConnection_OnMessage(object sender, agsXMPP.protocol.client.Message msg)
        {
            _bot = new AIMLChatBot();
            if (!string.IsNullOrEmpty(msg.Body))
            {

                // Get the Jabber username.
                agsXMPP.Jid JID = new Jid(msg.From.Bare);

                // Get the context User from the Jabber username (allows our bot to track conversations per user).
                User user = CommonManager.GetUser(msg.From.Bare, _bot);

                // Let our chat bot respond to the message.
                msg.Body = _sanitize(msg.Body) + "\r\n" + DateTime.Now.ToString() + "\r\n";
                string response = HandleMessage(msg.Body, user);

                // Setup a response event.
                _xmppConnection.MessageGrabber.Add(JID, new agsXMPP.Collections.BareJidComparer(), new MessageCB(ChatResponseReceived), null);

                // Create a new message.
                agsXMPP.protocol.client.Message newmsg = new agsXMPP.protocol.client.Message();
                newmsg.Type = agsXMPP.protocol.client.MessageType.chat;
                newmsg.To = JID;
                newmsg.Body = response;
     
                // Send response.
                //FullName
                if (newmsg.ToString().IndexOf("$1") > 0)
                {
                    newmsg.Body = newmsg.Body.ToString().Replace("$1", _getattribute(GetAttribute.FullName, msg.From));
                }
                //FirstName
                if (newmsg.ToString().IndexOf("$2") > 0)
                {
                    newmsg.Body = newmsg.Body.ToString().Replace("$2", _getattribute(GetAttribute.FirstName, msg.From));
                }
                //LastName
                if (newmsg.ToString().IndexOf("$3") > 0)
                {
                    newmsg.Body = newmsg.Body.ToString().Replace("$3", _getattribute(GetAttribute.LastName, msg.From));
                }
                //Email
                if (newmsg.ToString().IndexOf("$4") > 0)
                {
                    newmsg.Body = newmsg.Body.ToString().Replace("$4", _getattribute(GetAttribute.Email, msg.From));
                }
                //Username
                if (newmsg.ToString().IndexOf("$5") > 0)
                {
                    newmsg.Body = newmsg.Body.ToString().Replace("$5", _getattribute(GetAttribute.Username, msg.From));
                }
                _xmppConnection.Send(newmsg);
  
                // This is called for any message received.
                log(_getattribute(GetAttribute.FullName, msg.From) + ": " + msg.Body + "\r\n" + "WAHITDesk: " + newmsg.Body + "\r\n" + DateTime.Now.ToString());
    
             }

        }

        
        #endregion

             private void StopXMPP()
             {
            _xmppConnection.Close();
            _xmppConnection = null;
             }

            String _sanitize(String inputstring)
            {
            foreach (String _litter in _sanitizer)
            {
                inputstring = inputstring.Replace(_litter, " ");
            }

            return inputstring;
            }


        String _getattribute(GetAttribute r, String s)
        {
            /*
            String _namepart = s.Substring(0,s.IndexOf("@"));
            String _domainpart = s.Substring(s.IndexOf("@") + 7, 14);
            return _namepart +"@"+ _domainpart;
            */

            switch (r)
            {
                case GetAttribute.Email:
                    {
                        String _namepart = s.Substring(0, s.IndexOf("@"));
                        String _domainpart = s.Substring(s.IndexOf("@") + 7, 14);
                        return _namepart + "@" + _domainpart;
                    }
                case GetAttribute.FirstName:
                    {
                        String _fname = s.Substring(0, s.IndexOf("."));
                        return _fname.First().ToString().ToUpper() + _fname.Substring(1);       
                    }
                case GetAttribute.LastName:
                    {
                        String _lname = s.Substring(s.ToString().IndexOf(".") + 1, s.ToString().IndexOf("@") - s.ToString().IndexOf(".") - 1);
                        return _lname.First().ToString().ToUpper() + _lname.Substring(1);
                    }
                case GetAttribute.FullName:
                    {
                        String _fname = s.Substring(0, s.IndexOf("."));
                        String _lname = s.Substring(s.ToString().IndexOf(".") + 1, s.ToString().IndexOf("@") - s.ToString().IndexOf(".") - 1);
                        String _fcaps = _fname.First().ToString().ToUpper() + _fname.Substring(1);
                        String _lcaps = _lname.First().ToString().ToUpper() + _lname.Substring(1);
                        return _fcaps + " " + _lcaps;
                    }
                case GetAttribute.Username:
                    {
                        String _namepart = s.Substring(0, s.IndexOf("@"));
                        return _namepart;
                    }
                default:
                    return s.Substring(0, s.IndexOf("."));
            }

        }

        public void ChatResponseReceived(object sender, agsXMPP.protocol.client.Message msg, object data)
        {
            // This is called when a message is received after we've initiated a chat with someone.
            // Do nothing, we'll respond in the OnMessage event.
        }

        /// <summary>
        /// Returns a response from the message received.
        /// The response comes from our AIML chat bot or from our own custom processing.
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="user">User (for context)</param>
        /// <returns>string</returns>
        string HandleMessage(string message, User user)
        {
            string output = "";

            if (!string.IsNullOrEmpty(message))
            {
                // Provide custom commands for our chat bot, such as disk space, utility functions, typical IRC bot features, etc.
                if (message.ToUpper().IndexOf("DISK SPACE") != -1)
                {
                    DriveInfo driveInfo = new DriveInfo("C");
                    output = "Available disk space on " + driveInfo.Name + " is " + driveInfo.AvailableFreeSpace + ".";
                }
                else if (message.ToUpper().IndexOf("DISK SIZE") != -1)
                {
                    DriveInfo driveInfo = new DriveInfo("C");
                    output = "The current disk size on " + driveInfo.Name + " is " + driveInfo.TotalSize + ".";
                }
                else
                {
                    // No recognized command. Let our chat bot respond.
                    output = _bot.getOutput(message, user);
                }
            }

            return output;
        }

        public Main()
        {

        InitializeComponent();
        
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // Initialize and open the XMPP chat bot connection.
            SetupXMPP();
        }

 
        private void btnStop_Click(object sender, EventArgs e)
        {
            this.StopXMPP();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void chatBox_TextChanged(object sender, EventArgs e)
        {        
            chatBox.SelectionStart = chatBox.TextLength;
            chatBox.ScrollToCaret();

            this.Refresh();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }
    }

    internal class MessageCBType
    {
        internal static MessageCB chat;
    }

}
