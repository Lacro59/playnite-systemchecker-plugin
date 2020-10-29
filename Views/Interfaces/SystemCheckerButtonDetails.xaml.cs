using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SystemChecker.Clients;
using SystemChecker.Services;

namespace SystemChecker.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour SystemCheckerButtonDetails.xaml
    /// </summary>
    public partial class SystemCheckerButtonDetails : Button
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        public SystemCheckerButtonDetails()
        {
            InitializeComponent();
        }

        public void SetData(CheckSystem CheckMinimum, CheckSystem CheckRecommanded, Brush DefaultForeground)
        {
            OnlyIcon.Foreground = DefaultForeground;

            if (CheckMinimum.AllOk != null)
            {
                if (!(bool)CheckMinimum.AllOk)
                {
                    OnlyIcon.Foreground = Brushes.Red;
                }

                if ((bool)CheckMinimum.AllOk)
                {
                    OnlyIcon.Foreground = Brushes.Orange;
                    if (CheckRecommanded.AllOk == null)
                    {
                        logger.Debug("Green");
                        OnlyIcon.Foreground = Brushes.Green;
                    }
                }
            }
            if (CheckRecommanded.AllOk != null)
            {
                if ((bool)CheckRecommanded.AllOk)
                {
                    OnlyIcon.Foreground = Brushes.Green;
                }
            }
        }
    }
}
