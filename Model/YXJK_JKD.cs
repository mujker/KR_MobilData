using Telerik.Windows.Controls;

namespace KR_MobilData.Model
{
    public class YXJK_JKD : ViewModelBase
    {
        private string _JKD_ID;
        private string _JKD_NAME;
        private string _JKD_VALUE;
        private string _RMI_ID;
        private string _CURR_TIME;
        private string _REDIS_SAVE;
        private string _JKD_LB;

        public string JKD_ID
        {
            get { return _JKD_ID; }

            set
            {
                _JKD_ID = value;
                OnPropertyChanged("JKD_ID");
            }
        }

        public string JKD_NAME
        {
            get { return _JKD_NAME; }

            set
            {
                _JKD_NAME = value;
                OnPropertyChanged("JKD_NAME");
            }
        }

        public string JKD_VALUE
        {
            get { return _JKD_VALUE; }

            set
            {
                _JKD_VALUE = value;
                OnPropertyChanged("JKD_VALUE");
            }
        }

        public string RMI_ID
        {
            get { return _RMI_ID; }

            set
            {
                _RMI_ID = value;
                OnPropertyChanged("RMI_ID");
            }
        }

        public string CURR_TIME
        {
            get { return _CURR_TIME; }

            set
            {
                _CURR_TIME = value;
                OnPropertyChanged("CURR_TIME");
            }
        }

        public string REDIS_SAVE
        {
            get { return _REDIS_SAVE; }

            set
            {
                _REDIS_SAVE = value;
                OnPropertyChanged("REDIS_SAVE");
            }
        }

        public string JKD_LB
        {
            get
            {
                return _JKD_LB;
            }

            set
            {
                _JKD_LB = value;
                OnPropertyChanged("JKD_LB");
            }
        }
    }
}