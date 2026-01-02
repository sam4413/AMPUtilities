using HarmonyLib;
using NLog;
using Sandbox.ModAPI;
using System;
using System.Threading;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Commands;
using Torch.Session;
using VRage.Utils;
using System.Diagnostics;
using Torch.Server;
using Torch.Mod;
using System.Threading.Tasks;
using Torch.Mod.Messages;

namespace AmpUtilities
{
    public class AmpUtilities : TorchPluginBase
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private Thread _inputThread;
        private bool _running = true;
        private static CommandManager _commandManager;
        private static IChatManagerServer _chatManagerServer;
        private readonly Harmony _harmony = new Harmony("AmpUtilities");

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            TorchSessionManager sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Log.Info("AMPUtils Patch");
            _harmony.PatchAll();
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                Log.Info("Session Loaded!");
                _commandManager = Torch.CurrentSession.Managers.GetManager<CommandManager>();
                _chatManagerServer = Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
                MyAPIGateway.Parallel.StartBackground(ReadInputLoop);
                break;

                case TorchSessionState.Unloading:
                Log.Info("Session Unloading!");
                _running = false;
                if (_inputThread != null && _inputThread.IsAlive)
                {
                    try
                    {
                        _inputThread.Interrupt();
                    }
                    catch { }
                    _inputThread = null;
                }
                break;
            }
        }

        private void ReadInputLoop()
        {
            while (_running)
            {
                try
                {
                    string line = Console.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    Log.Info($"Received Input: {line}");
                    Thread CurrentThread = Thread.CurrentThread;
                    if (CurrentThread != MyUtils.MainThread)
                    {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                        {
                            if (_commandManager != null && _chatManagerServer != null)
                            {
                                if (_commandManager.IsCommand(line))
                                {
                                    if (!_commandManager.HandleCommandFromServer(line, PrintMessage))
                                        Log.Info("Invalid Command");
                                }
                                else
                                    _chatManagerServer.SendMessageAsSelf(line);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"STDIO error: {ex.Message}");
                }
            }
        }
        private void PrintMessage(TorchChatMessage msg)
        {
            Log.Info("\n" + msg.Author + " : " + msg.Message);
        }
        [HarmonyPatch]
        [HarmonyPatch(typeof(TorchServer), "Restart")]
        public class RestartPatch
        {
            public static bool Prefix(TorchServer __instance, bool save)
            {
                if (__instance.Config.DisconnectOnRestart)
                {
                    ModCommunication.SendMessageToClients(new JoinServerMessage("0.0.0.0:25555"));
                    Log.Info("Ejected all players from server for restart.");
                }
                if (__instance.IsRunning && save)
                    __instance.Save().ContinueWith(KillProc, __instance, TaskContinuationOptions.RunContinuationsAsynchronously);

                KillProc(null, __instance);
                return false;
            }
        }
        [HarmonyPatch]
        [HarmonyPatch(typeof(Initializer), "SendAndDump")]
        public class InitializerPatch
        {
            public static void Postfix()
            {
                LogManager.Flush();
                Process.GetCurrentProcess().Kill();
            }
        }
        public static void KillProc(Task<GameSaveResult> task, object torch0)
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}