﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using GTAServer.Logging;
using Microsoft.Extensions.Logging;

namespace GTAServer.PluginAPI
{
    public class PluginLoader
    {
        private static readonly string Location = System.AppContext.BaseDirectory;

        private static ILogger _logger;
        public static List<IPlugin> LoadPlugin(string targetAssemblyName)
        {
            _logger = Util.LoggerFactory.CreateLogger<PluginLoader>();
            var assemblyName = Location + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + targetAssemblyName + ".dll";
            var pluginList = new List<IPlugin>();

            /*_logger.LogTrace(asmName.FullName);
            var pluginAssembly = Assembly.Load(asmName);*/

            var pluginAssembly =
                Assembly.LoadFrom(assemblyName);

            var types = pluginAssembly.GetExportedTypes();
            var validTypes = types.Where(t => typeof(IPlugin).IsAssignableFrom(t)).ToArray();
            if (!validTypes.Any())
            {
                _logger.LogError(LogEvent.PluginLoader, "No classes found that extend IPlugin in assembly " + assemblyName);;
                return new List<IPlugin>();
            }
            foreach (var plugin in validTypes)
            {
                var curPlugin = Activator.CreateInstance(plugin) as IPlugin;
                if (curPlugin == null)
                {
                    _logger.LogWarning(LogEvent.PluginLoader, "Could not create instance of " + plugin.Name +
                                       " (returned null after Activator.CreateInstance)");
                }
                else
                {
                    pluginList.Add(curPlugin);
                    _logger.LogInformation(LogEvent.PluginLoader, "Plugin loaded: " + curPlugin.Name + " by " + curPlugin.Author);
                }

            }
            
            return pluginList;
        }
    }
}
