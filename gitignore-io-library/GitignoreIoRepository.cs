using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GitignoreIoLibrary
{
    public class GitignoreIoRepository
    {
        private static readonly string _apiUrl = "https://www.toptal.com/developers/gitignore/api/";

        public static async Task<IEnumerable<string>> GetTemplateNames()
        {
            var result = new List<string>();
            var templateNames = await InvokeApi("list").ConfigureAwait(false);
            foreach (var line in templateNames.Split('\n'))
            {
                foreach (var name in line.Split(','))
                    result.Add(name);
            }
            return result;
        }

        public static async Task<string> GetTemplate(string[] names)
        {
            return await InvokeApi(string.Join(',', names));
        }

        private static async Task<string> InvokeApi(string method)
        {
            using var client = new HttpClient();
            return await client.GetStringAsync($"{_apiUrl}{method}").ConfigureAwait(false);
        }
    }
}
