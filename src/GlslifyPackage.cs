using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ghgl
{
    class GlslifyPackage
    {
        static System.Net.Http.HttpClient _glslifyClient;

        static string PackageServerUrl
        {
            get
            {
                //return "http://localhost:8080";
                return "https://ghgl-glslify.herokuapp.com";
            }
        }

        public static string GlslifyCode(string code)
        {
            try
            {
                if (null == _glslifyClient)
                    _glslifyClient = new System.Net.Http.HttpClient();

                var values = new Dictionary<string, string> { { "code", code } };
                string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(values);
                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                var response = _glslifyClient.PostAsync(PackageServerUrl + "/process", content);
                string processedCode = response.Result.Content.ReadAsStringAsync().Result;
                return processedCode;
            }
            catch (Exception)
            {
            }
            return "";
        }

        static Task<List<GlslifyPackage>> _packageRetriever;
        static List<GlslifyPackage> _availablePackages;

        public static void Initialize()
        {
            if (null == _glslifyClient)
                _glslifyClient = new System.Net.Http.HttpClient();

            if (_packageRetriever == null )
            {
                _packageRetriever = Task.Run<List<GlslifyPackage>>( async() => {
                    List<GlslifyPackage> packages = new List<GlslifyPackage>();
                    try
                    {
                        var jsonSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                        var msg = await _glslifyClient.GetAsync(PackageServerUrl + "/packages");
                        {
                            string packageList = await msg.Content.ReadAsStringAsync();
                            var packageDictionaries = jsonSerializer.Deserialize<Dictionary<string, string>[]>(packageList);
                            foreach (var dict in packageDictionaries)
                            {
                                GlslifyPackage package = new GlslifyPackage(dict);
                                packages.Add(package);
                            }
                        }
                    }
                    catch (Exception) {
                        // throw away
                    }
                    return packages;
                });
            }
        }

        public static GlslifyPackage[] AvailablePackages
        {
            get
            {
                if( _availablePackages == null )
                {
                    _availablePackages = _packageRetriever.Result;
                    _packageRetriever = null;
                }
                return _availablePackages.ToArray();
            }
        }

        public string Name { get; }
        public string Author { get; }
        public string Description { get; }
        public string HomePage { get; }

        public string PragmaLine(string functionName)
        {
            if(string.IsNullOrWhiteSpace(functionName))
            {
                functionName = Name;
                if (functionName.StartsWith("glsl-"))
                    functionName = functionName.Substring("glsl-".Length);
                functionName = functionName.Replace('-', '_');
            }
            string text = $"#pragma glslify: {functionName} = require('{Name}')";
            return text;
        }

        GlslifyPackage(Dictionary<string,string> d)
        {
            Name = d["name"];
            Author = d["author"];
            Description = d["description"];
            HomePage = d["homepage"];
        }
    }
}
