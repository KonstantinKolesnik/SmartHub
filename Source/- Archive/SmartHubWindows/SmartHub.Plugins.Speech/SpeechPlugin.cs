﻿using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using SmartHub.Core.Plugins;
using SmartHub.Core.Plugins.Utils;
using SmartHub.Plugins.HttpListener.Api;
using SmartHub.Plugins.HttpListener.Attributes;
using SmartHub.Plugins.Scripts;
using SmartHub.Plugins.Scripts.Attributes;
using SmartHub.Plugins.Speech.Data;
using SmartHub.Plugins.WebUI.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
//using Microsoft.Speech.Recognition;
//using Microsoft.Speech.Synthesis;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;

namespace SmartHub.Plugins.Speech
{
    [AppSection("Голосовые команды", SectionType.System, "/webapp/speech/settings.js", "SmartHub.Plugins.Speech.Resources.js.settings.js", TileTypeFullName = "SmartHub.Plugins.Speech.SpeechTile")]
    [JavaScriptResource("/webapp/speech/settings-view.js", "SmartHub.Plugins.Speech.Resources.js.settings-view.js")]
    [JavaScriptResource("/webapp/speech/settings-model.js", "SmartHub.Plugins.Speech.Resources.js.settings-model.js")]
    [HttpResource("/webapp/speech/settings.html", "SmartHub.Plugins.Speech.Resources.js.settings.html")]

    [Plugin]
    public class SpeechPlugin : PluginBase, IDisposable
    {
        #region Fields
        private bool disposed = false;
        private SpeechSynthesizer speechSynthesizer;
        private SpeechRecognitionEngine recognitionEngine;

        private DateTime? readyDate;
        private const string NAME = "эй кампьютэр";
        private const string RESPONSE_READY = "слушаю!";
        private const int READY_PERIOD = 15; // seconds
        private const float CONFIDENCE_LIMIT = 0.5f;
        #endregion

        #region Plugin overrides
        public override void InitDbModel(ModelMapper mapper)
        {
            mapper.Class<VoiceCommand>(cfg => cfg.Table("Speech_VoiceCommand"));
        }
        public override void InitPlugin()
        {
            InitSpeechSynthesizer();
            InitRecognitionEngine();
        }
        public override void StopPlugin()
        {
            CloseSpeechSynthesizer();
            CloseRecognitionEngine();
        }
        #endregion

        #region Private methods
        private string[] LoadAllCommands()
        {
            using (var session = Context.OpenSession())
            {
                List<string> list = session.Query<VoiceCommand>().Select(cmd => cmd.CommandText).ToList();

                Logger.Info("Loaded commands: {0}", list.ToJson("[]"));

                list.Add(NAME);

                return list.ToArray();
            }
        }
        private VoiceCommand GetCommand(string text)
        {
            using (var session = Context.OpenSession())
            {
                var command = session.Query<VoiceCommand>().FirstOrDefault(x => x.CommandText == text);

                if (command != null)
                    Logger.Info("Loaded command: {0} (script: {1})", command.CommandText, command.UserScript.Name);

                return command;
            }
        }
        private List<VoiceCommand> GetCommands()
        {
            using (var session = Context.OpenSession())
                return session.Query<VoiceCommand>()
                    .OrderBy(cmd => cmd.CommandText)
                    .ToList();
        }
        private object BuildCommandWebModel(VoiceCommand cmd)
        {
            if (cmd == null)
                return null;

            return new
            {
                Id = cmd.Id,
                CommandText = cmd.CommandText,
                ScriptName = Context.GetPlugin<ScriptsPlugin>().GetScript(cmd.UserScript.Id).Name
            };
        }

        private void InitSpeechSynthesizer()
        {
            speechSynthesizer = new SpeechSynthesizer();
            speechSynthesizer.SetOutputToDefaultAudioDevice();

            speechSynthesizer.Rate = 1;
            speechSynthesizer.Volume = 100;

            var voiceList = speechSynthesizer.GetInstalledVoices();
            string voiceName = voiceList[0].VoiceInfo.Name;
            speechSynthesizer.SelectVoice(voiceName);
        }
        private void CloseSpeechSynthesizer()
        {
            if (speechSynthesizer != null)
            speechSynthesizer.Dispose();
        }

        private void InitRecognitionEngine()
        {
            //using (SpeechRecognizer recognizer = new System.Windows.Media.SpeechRecognition.SpeechRecognizer())
            return;

            var cultureInfo = new CultureInfo("ru-RU");
            //var cultureInfo = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;

            /*
            •en-GB. English (United Kingdom)
            •en-US. English (United States)
            •de-DE. German (Germany)
            •es-ES. Spanish (Spain)
            •fr-FR. French (France)
            •ja-JP. Japanese (Japan)
            •zh-CN. Chinese (China)
            •zh-TW. Chinese (Taiwan)
            */

            var commands = LoadAllCommands();
            var choices = new Choices(commands);
            var builder = new GrammarBuilder(choices);
            builder.Culture = cultureInfo;

            recognitionEngine = new SpeechRecognitionEngine();// (cultureInfo);
            recognitionEngine.SetInputToDefaultAudioDevice();
            recognitionEngine.UnloadAllGrammars();
            recognitionEngine.LoadGrammar(new Grammar(builder));
            //recognitionEngine.LoadGrammar(new DictationGrammar()); // любой текст

            recognitionEngine.SpeechRecognized += recognitionEngine_SpeechRecognized;
            recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
        }
        private void CloseRecognitionEngine()
        {
            if (recognitionEngine != null)
                recognitionEngine.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (speechSynthesizer != null)
                        speechSynthesizer.Dispose();
                    if (recognitionEngine != null)
                        recognitionEngine.Dispose();
                }

                disposed = true;
            }
        }
        #endregion

        #region Event handlers
        private void recognitionEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            var commandText = e.Result.Text;
            Logger.Info("Command: {0} ({1:0.00})", commandText, e.Result.Confidence);

            if (e.Result.Confidence < CONFIDENCE_LIMIT)
            {
                Logger.Info("Apply confidence limit");
                return;
            }

            var now = DateTime.Now;
            var isInPeriod = readyDate.GetValueOrDefault() > now;

            if (commandText == NAME)
            {
                Logger.Info("Command is COMPUTER NAME");
                readyDate = now.AddSeconds(READY_PERIOD);
                Say(RESPONSE_READY);
            }
            else
            {
                if (isInPeriod)
                {
                    try
                    {
                        //Debugger.Launch();
                        var command = GetCommand(commandText);
                        Logger.Info("Command info loaded");

                        Context.GetPlugin<ScriptsPlugin>().ExecuteScript(command.UserScript);

                        this.RaiseScriptEvent(x => x.OnVoiceCommandReceivedForScripts, commandText);

                        readyDate = null;
                    }
                    catch (Exception ex)
                    {
                        var msg = string.Format("Voice command error: '{0}'", commandText);
                        Logger.Error(ex, msg);
                    }
                }
            }
        }
        #endregion

        #region Script events
        [ScriptEvent("speech.commandReceived")]
        public ScriptEventHandlerDelegate[] OnVoiceCommandReceivedForScripts { get; set; }
        #endregion

        #region Script commands
        [ScriptCommand("say")]
        public void Say(string text)
        {
            if (!string.IsNullOrEmpty(text))
                //speechSynthesizer.Speak(text);
                speechSynthesizer.SpeakAsync(text);
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region Web API
        [HttpCommand("/api/speech/voicecommand/list")]
        private object apiGetCommands(HttpRequestParams request)
        {
            return GetCommands()
                .Select(BuildCommandWebModel)
                .Where(x => x != null)
                .ToArray();
        }
        [HttpCommand("/api/speech/voicecommand/add")]
        private object apiAddCommand(HttpRequestParams request)
        {
            var text = request.GetRequiredString("text");
            var scriptId = request.GetRequiredGuid("scriptId");

            using (var session = Context.OpenSession())
            {
                VoiceCommand cmd = new VoiceCommand()
                {
                    Id = Guid.NewGuid(),
                    CommandText = text,
                    UserScript = Context.GetPlugin<ScriptsPlugin>().GetScript(scriptId)
                };

                session.Save(cmd);
                session.Flush();
            }

            //NotifyForSignalR(new
            //{
            //    MsgId = "SensorNameChanged",
            //    Data = new
            //    {
            //        Id = id,
            //        Name = name
            //    }
            //});

            return null;
        }
        [HttpCommand("/api/speech/voicecommand/settext")]
        private object apiSetCommandName(HttpRequestParams request)
        {
            var id = request.GetRequiredGuid("id");
            var text = request.GetRequiredString("text");

            using (var session = Context.OpenSession())
            {
                var cmd = session.Load<VoiceCommand>(id);
                cmd.CommandText = text;
                session.Flush();
            }

            CloseRecognitionEngine();
            InitRecognitionEngine();

            //NotifyForSignalR(new
            //{
            //    MsgId = "SensorNameChanged",
            //    Data = new
            //    {
            //        Id = id,
            //        Name = name
            //    }
            //});

            return null;
        }
        [HttpCommand("/api/speech/voicecommand/delete")]
        private object apiDeleteMonitor(HttpRequestParams request)
        {
            var id = request.GetRequiredGuid("Id");

            using (var session = Context.OpenSession())
            {
                var cmd = session.Load<VoiceCommand>(id);
                session.Delete(cmd);
                session.Flush();
            }

            //NotifyForSignalR(new { MsgId = "SensorDeleted", Data = new { Id = id } });

            return null;
        }
        #endregion
    }
}
