using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MCModGetter.Classes
{
    public class RESTModConnection : ModConnection
    {
        #region Properties
        private string _rootURL;

        public string RootURL
        {
            get => _rootURL;
            set {
                _rootURL = value;
                OnPropertyChanged();
            }
        }


        private DateTime _lastUpdate;

        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set {
                _lastUpdate = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public RESTModConnection(
            string urlIn
            ) : base()
        {
            RootURL = urlIn;
        }

        public override bool Connect()
        {
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public WebResponse Result(string path)
        {
            if(path.Length == 0) { }
            return default;
        }

        public override WebResponse Result() => Result("mods");
    }
}
