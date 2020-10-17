using GitignoreIoLibrary;
using System;

namespace fun
{
    class Program
    {
        static void Main(string[] args)
        {
            // DumpTemplateNames();
            DumpTemplate(new string[] { "vscode", "jetbrains" });
        }

        static void DumpTemplateNames()
        {
            var task = GitignoreIoRepository.GetTemplateNames();//.ConfigureAwait(false).GetAwaiter();
            if (task.Wait(5000))
                foreach (var templateName in task.Result)
                    Console.WriteLine(templateName);
        }

        static void DumpTemplate(string[] names)
        {
            var task = GitignoreIoRepository.GetTemplate(names);
            if (task.Wait(5000))
                Console.WriteLine(task.Result);
        }
    }
}
