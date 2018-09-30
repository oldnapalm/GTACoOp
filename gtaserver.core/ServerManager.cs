﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using GTAServer.PluginAPI;
using SimpleConsoleLogger;
using gtaserver.core.ServerSystem;

namespace GTAServer
{
    public class ServerManager
    {
        private static ServerConfiguration _gameServerConfiguration;
        private static GameServer _gameServer;
        private static ILogger _logger;
        private static readonly List<IPlugin> Plugins=new List<IPlugin>();
        private static readonly string Location = System.AppContext.BaseDirectory;
        private static bool _debugMode = false;
        private static int _tickEvery = 10;
        private static void CreateNeededFiles()
        {
            if (!Directory.Exists(Location + Path.DirectorySeparatorChar + "Plugins")) Directory.CreateDirectory(Location + Path.DirectorySeparatorChar + "Plugins");
            if (!Directory.Exists(Location + Path.DirectorySeparatorChar + "Configuration")) Directory.CreateDirectory(Location + Path.DirectorySeparatorChar + "Configuration");
        }

        private static void DoDebugWarning()
        {
            if (!_debugMode) return;
            _logger.LogWarning("Note - This build is a debug build. Please do not share this build and report any issues to Mitchell Monahan (@wolfmitchell)");
            _logger.LogWarning("Furthermore, debug builds will not announce themselves to the master server, regardless of the AnnounceSelf config option.");
            _logger.LogWarning("To help bring crashes to the attention of the server owner and make sure they are reported to me, error catching has been disabled in this build.");
        }
        public static void Main(string[] args)
        {
#if DEBUG
            _debugMode = true;
#endif
            CreateNeededFiles();

            // can't use logger here since the logger config depends on if debug mode is on or off
            Console.WriteLine("Reading server configuration...");
            _gameServerConfiguration = LoadServerConfiguration(Location + Path.DirectorySeparatorChar + "Configuration" + Path.DirectorySeparatorChar + "serverSettings.xml");
            if (!_debugMode) _debugMode = _gameServerConfiguration.DebugMode;

            if (_debugMode)
            {

                Util.LoggerFactory = new LoggerFactory()
                    .AddSimpleConsole();
            }
            else
            {
                Util.LoggerFactory = new LoggerFactory()
                    .AddSimpleConsole((s, l) => (int) l >= (int) LogLevel.Information);
            }
            _logger = Util.LoggerFactory.CreateLogger<ServerManager>();
            DoDebugWarning();

            if (_gameServerConfiguration.ServerVariables.Any(v => v.Key == "tickEvery"))
            {
                var tpsString = _gameServerConfiguration.ServerVariables.First(v => v.Key == "tickEvery").Value;
                if (!int.TryParse(tpsString, out _tickEvery))
                {
                    _logger.LogError(
                        "Could not set ticks per second from server variable 'tps' (value is not an integer)");
                }
                else
                {
                    _logger.LogInformation("Custom tick rate set. Will try to tick every " + _tickEvery + "ms");
                }
            }
            
            _logger.LogInformation("Server preparing to start...");

            _gameServer = new GameServer(_gameServerConfiguration.Port, _gameServerConfiguration.ServerName,
                _gameServerConfiguration.GamemodeName, _debugMode)
            {
                Password = _gameServerConfiguration.Password,
                MasterServer = _gameServerConfiguration.PrimaryMasterServer,
                BackupMasterServer = _gameServerConfiguration.BackupMasterServer,
                AnnounceSelf = _gameServerConfiguration.AnnounceSelf,
                AllowNicknames = _gameServerConfiguration.AllowNicknames,
                AllowOutdatedClients = _gameServerConfiguration.AllowOutdatedClients,
                MaxPlayers = _gameServerConfiguration.MaxClients,
                Motd = _gameServerConfiguration.Motd
            };
            _gameServer.Start();

            // Plugin Code
            _logger.LogInformation("Loading plugins");
            //Plugins = PluginLoader.LoadPlugin("TestPlugin");
            foreach (var pluginName in _gameServerConfiguration.ServerPlugins)
            {
                foreach (var loadedPlugin in PluginLoader.LoadPlugin(pluginName))
                {
                    Plugins.Add(loadedPlugin);
                }
            }

            Plugins.Add(new SystemPlugin());

            _logger.LogInformation("Plugins loaded. Enabling plugins...");
            foreach (var plugin in Plugins)
            {
                if (!plugin.OnEnable(_gameServer, false))
                {
                    _logger.LogWarning("Plugin " + plugin.Name + " returned false when enabling, marking as disabled, although it may still have hooks registered and called.");
                }
            }

            _logger.LogInformation("Starting server main loop, ready to accept connections.");
            while (true)
            {
                doServerTick(_gameServer);
                Thread.Sleep(_tickEvery);
            }
        }

        public static void doServerTick(object serverObject)
        {
            var server = (GameServer) serverObject;
            try
            {
                server.Tick();
            }
            catch (Exception e)
            {
                _logger.LogError("Exception while ticking", e.Message);
            }
        }
        private static ServerConfiguration LoadServerConfiguration(string path)
        {
            var ser = new XmlSerializer(typeof(ServerConfiguration));

            ServerConfiguration cfg = null;
            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path)) cfg = (ServerConfiguration)ser.Deserialize(stream);
                using (
                    var stream = new FileStream(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create,
                        FileAccess.ReadWrite)) ser.Serialize(stream, cfg);
            }
            else
            {
                Console.WriteLine("No configuration found, creating a new one.");
                using (var stream = File.OpenWrite(path)) ser.Serialize(stream, cfg = new ServerConfiguration());
            }
            return cfg;
        }

        public static List<IPlugin> GetPlugins()
        {
            return Plugins;
        }

        public static GameServer GameServerInstance
        {
            get {
                return _gameServer;
            }
        }
    }
}
