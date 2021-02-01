using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MCModGetter.Classes
{
    public abstract class ModConnection : INotifyPropertyChanged
    {
		#region NotifyPropertyChanged Impl
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propFull = "")
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propFull));
		#endregion

		#region Properties
		private bool _isConnected = false;

		public bool IsConnected
		{
			get => _isConnected;
			set { 
				_isConnected = value;
				OnPropertyChanged();
			}
		}

		private string _status;

		public string Status
		{
			get => _status;
			set { 
				_status = value;
				OnPropertyChanged();
			}
		}


		#endregion

		/// <summary>
		/// Will try to connnect to the target location for updates and mods
		/// </summary>
		/// <returns></returns>
		public abstract bool Connect();

		/// <summary>
		/// Will get the result after connecting to the given URL
		/// </summary>
		/// <returns></returns>
		public abstract WebResponse Result();
	}
}
