using System;

namespace Vostok.Configuration.Demo
{
    internal class Application
    {
        public Application(ApplicationSettings settings)
        {
            Console.WriteLine("Application initialized.");
        }

        public void UpdateSettings(ApplicationSettings settings)
        {
            Console.WriteLine("Application settings updated.");
        }
    }
}