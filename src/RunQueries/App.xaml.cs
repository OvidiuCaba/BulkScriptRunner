using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RunQueries
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            RegisterGlobalExceptionHandling();
        }

        private void RegisterGlobalExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => CurrentDomainOnUnhandledException(args);

            Dispatcher.UnhandledException +=
                (sender, args) => DispatcherOnUnhandledException(args);

            Application.Current.DispatcherUnhandledException +=
                (sender, args) => CurrentOnDispatcherUnhandledException(args);

            TaskScheduler.UnobservedTaskException +=
                (sender, args) => TaskSchedulerOnUnobservedTaskException(args);
        }

        private static void TaskSchedulerOnUnobservedTaskException(UnobservedTaskExceptionEventArgs args)
        {
            MessageBox.Show(args.Exception.Message);
            args.SetObserved();
        }

        private static void CurrentOnDispatcherUnhandledException(DispatcherUnhandledExceptionEventArgs args)
        {
            MessageBox.Show(args.Exception.Message);
            args.Handled = true;
        }

        private static void DispatcherOnUnhandledException(DispatcherUnhandledExceptionEventArgs args)
        {
            MessageBox.Show(args.Exception.Message);
            args.Handled = true;
        }

        private static void CurrentDomainOnUnhandledException(UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception;
            var terminatingMessage = args.IsTerminating ? " The application is terminating." : string.Empty;
            var exceptionMessage = exception?.Message ?? "An unmanaged exception occured.";
            var message = string.Concat(exceptionMessage, terminatingMessage);
            MessageBox.Show(message);
        }
    }
}
