using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandyPanel.ViewModel
{
    class ViewModelLocator
    {
        public IServiceProvider Services { get; }

        public MainViewModel Main { get { return Services.GetService<MainViewModel>(); } }

        public ViewModelLocator() {
            var services = new ServiceCollection();

            // ViewModels
            services.AddTransient<MainViewModel>();

            Services = services.BuildServiceProvider();
        }
    }
}
