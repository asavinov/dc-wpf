using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Samm
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ObservableCollection<string> MyList { get; set; }

        public App()
        {
            MyList = new ObservableCollection<string>();
            MyList.Add("aaa");
            MyList.Add("bbb");
            MyList.Add("ccc");
        }
    }
}
