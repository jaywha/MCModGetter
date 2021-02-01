using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MCModGetter.Classes
{
    public class FTPModConnection : ModConnection
    {
        #region Properties
        private string _pass;

        public string Password
        {
            get => _pass;
            set { 
                _pass = value;
                OnPropertyChanged();
            }
        }

        private string _usr;

        public string UserName
        {
            get => _usr;
            set { 
                _usr = value;
                OnPropertyChanged();
            }
        }

        private string _ip;

        public string IP
        {
            get => _ip;
            set
            {
                _ip = value;
                OnPropertyChanged();
            }
        }

        private string _workingURL;

        public string WorkingURL
        {
            get => $"ftp://{IP}/";
        }

        #endregion
        public FTPModConnection(
            string IPIn = "",
            string usr = "yep",
            string passIn = "yeah"
            ) : base()
        {
            IP = IPIn;
            Password = passIn;
            UserName = usr;
        }

        public override bool Connect()
        {
            IsConnected = true;
            return false;
        }

        public WebResponse Result(string path, string requestMethod = "LIST")
        {
            if (!IsConnected)
                throw new InvalidOperationException("Can't get data before connecting");

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(path);
            request.Method = requestMethod;
            request.Credentials = new NetworkCredential(UserName, Password);
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Status = response.StatusDescription;

            return response;
        }

        public override WebResponse Result() => Result("/mods");
    }
}
