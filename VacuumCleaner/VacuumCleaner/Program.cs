using System;

namespace VacuumCleaner
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (VacuumCleanerMain game = new VacuumCleanerMain())
            {
                game.Run();
            }
        }
    }
#endif
}

